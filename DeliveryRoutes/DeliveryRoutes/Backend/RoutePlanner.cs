namespace Backend;

/// <summary>
/// Plans a short, valid pickup-and-delivery route using precomputed Dijkstra distances
/// and a multi-start insertion + local search heuristic.
/// </summary>
public class RoutePlanner
{
    private readonly Graph<string> _city;
    private readonly IReadOnlyList<Package> _packages;
    private readonly string _start;
    private readonly Dictionary<string, ShortestPathTree<string>> _trees;

    public RoutePlanner(Graph<string> city, IReadOnlyList<Package> packages, string start)
    {
        _city = city;
        _packages = packages;
        _start = start;
        _trees = PrecomputeShortestPaths();
    }

    private Dictionary<string, ShortestPathTree<string>> PrecomputeShortestPaths()
    {
        var origins = new HashSet<string>(StringComparer.Ordinal) { _start };
        foreach (var pkg in _packages)
        {
            origins.Add(pkg.Pickup);
            origins.Add(pkg.Delivery);
        }

        var trees = new Dictionary<string, ShortestPathTree<string>>(origins.Count, StringComparer.Ordinal);
        foreach (string origin in origins)
        {
            trees[origin] = ShortestPaths.Dijkstra(_city, origin, StringComparer.Ordinal);
        }

        return trees;
    }

    public double DistanceBetween(string from, string to)
    {
        if (string.Equals(from, to, StringComparison.Ordinal))
        {
            return 0;
        }

        return _trees[from].DistanceTo(to);
    }

    public List<string> PathBetween(string from, string to)
    {
        if (string.Equals(from, to, StringComparison.Ordinal))
        {
            return new List<string> { from };
        }

        return _trees[from].PathTo(to);
    }

    /// <summary>
    /// Runs multi-start cheapest insertion followed by local search to find a short route.
    /// </summary>
    public List<Stop> Plan()
    {
        const int restarts = 80;
        var rng = new Random(42); // Fixed seed for reproducible results.

        List<Stop>? bestRoute = null;
        double bestCost = double.PositiveInfinity;

        for (int attempt = 0; attempt < restarts; attempt++)
        {
            var order = _packages.ToList();
            if (attempt > 0)
            {
                Shuffle(order, rng);
            }

            List<Stop> candidate = BuildByCheapestInsertion(order);
            ImproveByLocalSearch(candidate);

            double cost = TotalDistance(candidate);
            if (cost < bestCost)
            {
                bestCost = cost;
                bestRoute = candidate;
            }
        }

        return bestRoute ?? new List<Stop>();
    }

    public double TotalDistance(List<Stop> route)
    {
        double total = 0;
        string current = _start;

        foreach (var stop in route)
        {
            total += DistanceBetween(current, stop.Location);
            current = stop.Location;
        }

        return total;
    }

    /// <summary>
    /// Verifies that every package is picked up once and delivered once after its pickup.
    /// </summary>
    public bool IsValid(List<Stop> route)
    {
        if (route.Count != _packages.Count * 2)
        {
            return false;
        }

        var expectedById = _packages.ToDictionary(p => p.Id);
        var pickedUp = new HashSet<int>();
        var delivered = new HashSet<int>();

        foreach (var stop in route)
        {
            if (!expectedById.TryGetValue(stop.PackageId, out var pkg))
            {
                return false;
            }

            if (stop.Kind == StopKind.Pickup)
            {
                if (!string.Equals(stop.Location, pkg.Pickup, StringComparison.Ordinal))
                {
                    return false;
                }

                if (!pickedUp.Add(stop.PackageId))
                {
                    return false;
                }
            }
            else
            {
                if (!string.Equals(stop.Location, pkg.Delivery, StringComparison.Ordinal))
                {
                    return false;
                }

                if (!pickedUp.Contains(stop.PackageId) || !delivered.Add(stop.PackageId))
                {
                    return false;
                }
            }
        }

        return pickedUp.Count == _packages.Count && delivered.Count == _packages.Count;
    }

    private List<Stop> BuildByCheapestInsertion(IReadOnlyList<Package> orderedPackages)
    {
        var route = new List<Stop>(orderedPackages.Count * 2);

        foreach (var pkg in orderedPackages)
        {
            InsertPackageAtBestPosition(route, pkg);
        }

        return route;
    }

    private void ImproveByLocalSearch(List<Stop> route)
    {
        bool improved = true;

        while (improved)
        {
            improved = false;

            if (RelocatePackages(route))
            {
                improved = true;
            }

            if (RelocateSingleStops(route))
            {
                improved = true;
            }
        }
    }

    /// <summary>
    /// Removes both stops of a package and reinserts them at the cheapest valid positions.
    /// </summary>
    private bool RelocatePackages(List<Stop> route)
    {
        bool improved = false;

        foreach (var pkg in _packages)
        {
            double beforeCost = TotalDistance(route);

            int pickupIdx = route.FindIndex(s => s.PackageId == pkg.Id && s.Kind == StopKind.Pickup);
            int deliveryIdx = route.FindIndex(s => s.PackageId == pkg.Id && s.Kind == StopKind.Delivery);

            // Remove the later index first so the earlier index remains valid.
            route.RemoveAt(deliveryIdx);
            route.RemoveAt(pickupIdx);

            InsertPackageAtBestPosition(route, pkg);

            if (TotalDistance(route) < beforeCost - 1e-6)
            {
                improved = true;
            }
        }

        return improved;
    }

    /// <summary>
    /// Tries moving each individual stop to a better valid position without violating precedence.
    /// </summary>
    private bool RelocateSingleStops(List<Stop> route)
    {
        bool improved = false;

        for (int i = 0; i < route.Count; i++)
        {
            Stop stop = route[i];
            int minPos;
            int maxPos;

            if (stop.Kind == StopKind.Pickup)
            {
                int deliveryIdx = route.FindIndex(s => s.PackageId == stop.PackageId && s.Kind == StopKind.Delivery);
                minPos = 0;
                maxPos = deliveryIdx - 1; // After removing i, delivery shifts left by 1.
            }
            else
            {
                int pickupIdx = route.FindIndex(s => s.PackageId == stop.PackageId && s.Kind == StopKind.Pickup);
                minPos = pickupIdx + 1;
                maxPos = route.Count - 1;
            }

            double beforeCost = TotalDistance(route);
            route.RemoveAt(i);

            int bestPos = i;
            double bestDelta = double.PositiveInfinity;

            for (int pos = minPos; pos <= maxPos; pos++)
            {
                double delta = SingleInsertionDelta(route, stop.Location, pos);
                if (delta < bestDelta)
                {
                    bestDelta = delta;
                    bestPos = pos;
                }
            }

            route.Insert(bestPos, stop);

            if (TotalDistance(route) < beforeCost - 1e-6)
            {
                improved = true;
            }
        }

        return improved;
    }

    private void InsertPackageAtBestPosition(List<Stop> route, Package pkg)
    {
        int n = route.Count;
        int bestPickupPos = 0;
        int bestDeliveryPos = 1;
        double bestDelta = double.PositiveInfinity;

        for (int i = 0; i <= n; i++)
        {
            string prevI = i == 0 ? _start : route[i - 1].Location;

            // Case 1: Delivery immediately after Pickup (j = i + 1).
            double oldEdgeI = i < n ? DistanceBetween(prevI, route[i].Location) : 0;
            double adjacentNew = DistanceBetween(prevI, pkg.Pickup)
                               + DistanceBetween(pkg.Pickup, pkg.Delivery)
                               + (i < n ? DistanceBetween(pkg.Delivery, route[i].Location) : 0);
            double adjacentDelta = adjacentNew - oldEdgeI;

            if (adjacentDelta < bestDelta)
            {
                bestDelta = adjacentDelta;
                bestPickupPos = i;
                bestDeliveryPos = i + 1;
            }

            // Case 2: Delivery placed later (j > i + 1), which requires i < n.
            if (i < n)
            {
                double deltaPickup = DistanceBetween(prevI, pkg.Pickup)
                                   + DistanceBetween(pkg.Pickup, route[i].Location)
                                   - oldEdgeI;

                for (int k = i + 1; k <= n; k++)
                {
                    string prevK = route[k - 1].Location;
                    double oldEdgeK = k < n ? DistanceBetween(prevK, route[k].Location) : 0;
                    double newEdgeK = DistanceBetween(prevK, pkg.Delivery)
                                    + (k < n ? DistanceBetween(pkg.Delivery, route[k].Location) : 0);
                    double totalDelta = deltaPickup + (newEdgeK - oldEdgeK);

                    if (totalDelta < bestDelta)
                    {
                        bestDelta = totalDelta;
                        bestPickupPos = i;
                        bestDeliveryPos = k + 1; // Index after inserting pickup at i.
                    }
                }
            }
        }

        route.Insert(bestPickupPos, new Stop(pkg.Pickup, StopKind.Pickup, pkg.Id));
        route.Insert(bestDeliveryPos, new Stop(pkg.Delivery, StopKind.Delivery, pkg.Id));
    }

    private double SingleInsertionDelta(List<Stop> route, string location, int pos)
    {
        string prev = pos == 0 ? _start : route[pos - 1].Location;
        double oldEdge = pos < route.Count ? DistanceBetween(prev, route[pos].Location) : 0;
        double newEdges = DistanceBetween(prev, location)
                        + (pos < route.Count ? DistanceBetween(location, route[pos].Location) : 0);
        return newEdges - oldEdge;
    }

    private static void Shuffle<T>(List<T> list, Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}