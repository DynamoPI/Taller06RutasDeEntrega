using Backend;

string cityPath = args.Length > 0 ? args[0] : "city.txt";
string packagesPath = args.Length > 1 ? args[1] : "packages.txt";
string start = args.Length > 2 ? args[2] : "D";

try
{
    CityMap city = CityFile.Load(cityPath);
    List<Package> packages = PackageFile.Load(packagesPath, city.Graph);
    PackageFile.ValidateReachability(city.Graph, packages, start);

    Console.WriteLine($"Locations: {city.Graph.VertexCount}");
    Console.WriteLine($"Streets: {city.TotalStreets} ({city.OneWayStreets} one-way)");
    Console.WriteLine($"Packages: {packages.Count}");
    Console.WriteLine($"Reachability from '{start}': OK");

    var fromAQ = ShortestPaths.Dijkstra(city.Graph, "AQ", StringComparer.Ordinal);
    var fromBS = ShortestPaths.Dijkstra(city.Graph, "BS", StringComparer.Ordinal);

    Console.WriteLine($"AQ -> BS: {fromAQ.DistanceTo("BS")} m | Path: {string.Join(" -> ", fromAQ.PathTo("BS"))}");
    Console.WriteLine($"BS -> AQ: {fromBS.DistanceTo("AQ")} m | Path: {string.Join(" -> ", fromBS.PathTo("AQ"))}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}