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
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}