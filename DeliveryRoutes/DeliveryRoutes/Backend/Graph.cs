namespace Backend;

/// <summary>
/// Weighted graph implemented with an adjacency list.
/// </summary>
public class Graph<T> where T : notnull
{
    private readonly Dictionary<T, List<Edge<T>>> _adjacency;

    public bool IsDirected { get; }

    public Graph(bool directed = true, IEqualityComparer<T>? comparer = null)
    {
        IsDirected = directed;
        _adjacency = new Dictionary<T, List<Edge<T>>>(comparer);
    }

    public int VertexCount => _adjacency.Count;

    public IReadOnlyCollection<T> Vertices => _adjacency.Keys;

    public bool ContainsVertex(T vertex) => _adjacency.ContainsKey(vertex);

    public void AddVertex(T vertex)
    {
        if (!_adjacency.ContainsKey(vertex))
        {
            _adjacency[vertex] = new List<Edge<T>>();
        }
    }

    public void AddEdge(T from, T to, double weight)
    {
        if (weight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(weight), "Edge weight must be greater than zero.");
        }

        AddVertex(from);
        AddVertex(to);

        _adjacency[from].Add(new Edge<T>(to, weight));

        if (!IsDirected)
        {
            _adjacency[to].Add(new Edge<T>(from, weight));
        }
    }

    public IReadOnlyList<Edge<T>> GetNeighbors(T vertex)
    {
        if (!_adjacency.TryGetValue(vertex, out var neighbors))
        {
            throw new KeyNotFoundException($"Vertex '{vertex}' does not exist in the graph.");
        }

        return neighbors;
    }
}