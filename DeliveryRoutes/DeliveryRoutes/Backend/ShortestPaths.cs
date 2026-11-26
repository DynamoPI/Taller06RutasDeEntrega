namespace Backend;

/// <summary>
/// Holds the shortest distances and predecessor map computed by Dijkstra from a single origin.
/// </summary>
public sealed class ShortestPathTree<T> where T : notnull
{
    public T Origin { get; }
    public IReadOnlyDictionary<T, double> Distances { get; }
    public IReadOnlyDictionary<T, T> Previous { get; }

    public ShortestPathTree(
        T origin,
        IReadOnlyDictionary<T, double> distances,
        IReadOnlyDictionary<T, T> previous)
    {
        Origin = origin;
        Distances = distances;
        Previous = previous;
    }

    public double DistanceTo(T target)
    {
        if (!Distances.TryGetValue(target, out double distance))
        {
            throw new InvalidOperationException($"No path exists from '{Origin}' to '{target}'.");
        }

        return distance;
    }

    /// <summary>
    /// Reconstructs the sequence of vertices from Origin to target.
    /// </summary>
    public List<T> PathTo(T target)
    {
        if (!Distances.ContainsKey(target))
        {
            throw new InvalidOperationException($"No path exists from '{Origin}' to '{target}'.");
        }

        var path = new List<T>();
        T current = target;
        path.Add(current);

        while (Previous.TryGetValue(current, out T? prev))
        {
            current = prev;
            path.Add(current);
        }

        path.Reverse();
        return path;
    }
}

/// <summary>
/// Computes single-source shortest paths using Dijkstra's algorithm with a PriorityQueue.
/// </summary>
public static class ShortestPaths
{
    public static ShortestPathTree<T> Dijkstra<T>(
        Graph<T> graph,
        T origin,
        IEqualityComparer<T>? comparer = null) where T : notnull
    {
        if (!graph.ContainsVertex(origin))
        {
            throw new ArgumentException($"Origin '{origin}' does not exist in the graph.", nameof(origin));
        }

        var distances = new Dictionary<T, double>(comparer)
        {
            [origin] = 0
        };
        var previous = new Dictionary<T, T>(comparer);
        var queue = new PriorityQueue<T, double>();

        queue.Enqueue(origin, 0);

        while (queue.TryDequeue(out T? current, out double currentDistance))
        {
            // Skip outdated entries in the priority queue (lazy deletion).
            if (currentDistance > distances[current])
            {
                continue;
            }

            foreach (var edge in graph.GetNeighbors(current))
            {
                double candidateDistance = currentDistance + edge.Weight;

                if (!distances.TryGetValue(edge.To, out double knownDistance)
                    || candidateDistance < knownDistance)
                {
                    distances[edge.To] = candidateDistance;
                    previous[edge.To] = current;
                    queue.Enqueue(edge.To, candidateDistance);
                }
            }
        }

        return new ShortestPathTree<T>(origin, distances, previous);
    }
}