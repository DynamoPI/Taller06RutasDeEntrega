namespace Backend;

/// <summary>
/// Represents a package with its identifier, pickup location, and delivery location.
/// </summary>
public readonly record struct Package(int Id, string Pickup, string Delivery);