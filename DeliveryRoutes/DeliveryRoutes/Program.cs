using Backend;

string cityPath;
string packagesPath;
string start;

if (args.Length >= 3)
{
    cityPath = args[0];
    packagesPath = args[1];
    start = args[2];
}
else
{
    Console.Write("Ruta del archivo de ciudad (ej. city.txt): ");
    cityPath = (Console.ReadLine() ?? string.Empty).Trim();

    Console.Write("Ruta del archivo de paquetes (ej. packages.txt): ");
    packagesPath = (Console.ReadLine() ?? string.Empty).Trim();

    Console.Write("Posición inicial (ej. D): ");
    start = (Console.ReadLine() ?? string.Empty).Trim();
}

try
{
    CityMap city = CityFile.Load(cityPath);
    List<Package> packages = PackageFile.Load(packagesPath, city.Graph);
    PackageFile.ValidateReachability(city.Graph, packages, start);

    var planner = new RoutePlanner(city.Graph, packages, start);
    List<Stop> route = planner.Plan();

    if (!planner.IsValid(route))
    {
        throw new InvalidOperationException("The generated route failed validation.");
    }

    string report = RoutePrinter.Format(city, packages, start, route, planner);
    Console.WriteLine();
    Console.Write(report);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.ExitCode = 1;
}