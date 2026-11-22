using Backend;

string cityPath = args.Length > 0 ? args[0] : "city.txt";

try
{
    CityMap city = CityFile.Load(cityPath);
    Console.WriteLine($"Locations: {city.Graph.VertexCount}");
    Console.WriteLine($"Streets: {city.TotalStreets} ({city.OneWayStreets} one-way)");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}