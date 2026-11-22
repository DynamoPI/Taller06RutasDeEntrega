namespace Backend;

/// <summary>
/// Represents a directed, weighted edge pointing to a neighbor vertex.
/// </summary>
public readonly record struct Edge<T>(T To, double Weight) where T : notnull;