namespace Backend;

/// <summary>
/// Loads and validates package tasks from a flat text file, and verifies reachability.
/// </summary>
public static class PackageFile
{
    public static List<Package> Load(string path, Graph<string> city)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Packages file not found: '{path}'.", path);
        }

        // Tracks partial pickup/delivery locations per package ID while reading.
        var tasksById = new Dictionary<int, (string? Pickup, string? Delivery)>();

        string[] lines = File.ReadAllLines(path);

        for (int i = 0; i < lines.Length; i++)
        {
            int lineNumber = i + 1;
            string rawLine = lines[i].Trim();

            // Skip blank lines and comments.
            if (string.IsNullOrEmpty(rawLine) || rawLine.StartsWith('#'))
            {
                continue;
            }

            string[] parts = rawLine.Split(',');
            if (parts.Length != 3)
            {
                throw new FormatException(
                    $"[{path}:{lineNumber}] Expected 3 comma-separated fields (location,action,package), got {parts.Length}.");
            }

            string location = parts[0].Trim();
            string actionText = parts[1].Trim();
            string packageIdText = parts[2].Trim();

            if (!city.ContainsVertex(location))
            {
                throw new FormatException(
                    $"[{path}:{lineNumber}] Location '{location}' does not exist in the city map.");
            }

            bool isPickup = actionText.Equals("Pickup", StringComparison.OrdinalIgnoreCase);
            bool isDelivery = actionText.Equals("Delivery", StringComparison.OrdinalIgnoreCase);

            if (!isPickup && !isDelivery)
            {
                throw new FormatException(
                    $"[{path}:{lineNumber}] Invalid action '{actionText}'. Expected 'Pickup' or 'Delivery'.");
            }

            if (!int.TryParse(packageIdText, out int packageId) || packageId <= 0)
            {
                throw new FormatException(
                    $"[{path}:{lineNumber}] Invalid package ID '{packageIdText}'. Must be an integer greater than 0.");
            }

            tasksById.TryGetValue(packageId, out var current);

            if (isPickup)
            {
                if (current.Pickup is not null)
                {
                    throw new FormatException(
                        $"[{path}:{lineNumber}] Package {packageId} already has a Pickup task.");
                }

                tasksById[packageId] = (location, current.Delivery);
            }
            else
            {
                if (current.Delivery is not null)
                {
                    throw new FormatException(
                        $"[{path}:{lineNumber}] Package {packageId} already has a Delivery task.");
                }

                tasksById[packageId] = (current.Pickup, location);
            }
        }

        if (tasksById.Count == 0)
        {
            throw new FormatException($"[{path}] The packages file does not contain any valid tasks.");
        }

        var packages = new List<Package>(tasksById.Count);

        foreach (var (id, pair) in tasksById.OrderBy(entry => entry.Key))
        {
            if (pair.Pickup is null)
            {
                throw new FormatException($"[{path}] Package {id} is missing its Pickup task.");
            }

            if (pair.Delivery is null)
            {
                throw new FormatException($"[{path}] Package {id} is missing its Delivery task.");
            }

            packages.Add(new Package(id, pair.Pickup, pair.Delivery));
        }

        return packages;
    }

    /// <summary>
    /// Verifies that the start location exists, all task locations are reachable from start,
    /// and every task location can reach all other task locations.
    /// </summary>
    public static void ValidateReachability(
        Graph<string> city,
        IReadOnlyList<Package> packages,
        string start)
    {
        if (!city.ContainsVertex(start))
        {
            throw new ArgumentException($"Start location '{start}' does not exist in the city map.", nameof(start));
        }

        var taskLocations = new HashSet<string>(StringComparer.Ordinal);
        foreach (var pkg in packages)
        {
            taskLocations.Add(pkg.Pickup);
            taskLocations.Add(pkg.Delivery);
        }

        // Check from start and from every task location.
        var origins = new HashSet<string>(taskLocations, StringComparer.Ordinal) { start };

        foreach (string origin in origins)
        {
            HashSet<string> reachable = GetReachableVertices(city, origin);

            foreach (string target in taskLocations)
            {
                if (!reachable.Contains(target))
                {
                    throw new InvalidOperationException(
                        $"Location '{target}' is not reachable from '{origin}' following street directions.");
                }
            }
        }
    }

    private static HashSet<string> GetReachableVertices(Graph<string> city, string start)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal) { start };
        var queue = new Queue<string>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            string current = queue.Dequeue();

            foreach (var edge in city.GetNeighbors(current))
            {
                if (visited.Add(edge.To))
                {
                    queue.Enqueue(edge.To);
                }
            }
        }

        return visited;
    }
}