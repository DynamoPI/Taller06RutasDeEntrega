namespace Backend;

/// <summary>
/// Indicates whether a stop is a package pickup or delivery.
/// </summary>
public enum StopKind
{
    Pickup,
    Delivery
}

/// <summary>
/// Represents a single task stop in the delivery route.
/// </summary>
public readonly record struct Stop(string Location, StopKind Kind, int PackageId);