using System.Globalization;
using System.Text;

namespace Backend;

/// <summary>
/// Formats the planned delivery route into a human-readable step-by-step report.
/// </summary>
public static class RoutePrinter
{
    public static string Format(
        CityMap city,
        IReadOnlyList<Package> packages,
        string start,
        List<Stop> route,
        RoutePlanner planner)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Posición inicial: {start}");
        sb.AppendLine(
            $"Paquetes: {packages.Count}   Ubicaciones: {city.Graph.VertexCount}   Calles: {city.TotalStreets} ({city.OneWayStreets} de un solo sentido)");
        sb.AppendLine();

        string currentLocation = start;
        int stepNumber = 1;
        int index = 0;
        double accumulatedMeters = 0;

        while (index < route.Count)
        {
            string targetLocation = route[index].Location;
            var deliveries = new List<int>();
            var pickups = new List<int>();

            // Group consecutive stops at the same location.
            while (index < route.Count && string.Equals(route[index].Location, targetLocation, StringComparison.Ordinal))
            {
                if (route[index].Kind == StopKind.Delivery)
                {
                    deliveries.Add(route[index].PackageId);
                }
                else
                {
                    pickups.Add(route[index].PackageId);
                }

                index++;
            }

            deliveries.Sort();
            pickups.Sort();

            string actionsText = FormatActions(deliveries, pickups);

            if (string.Equals(currentLocation, targetLocation, StringComparison.Ordinal))
            {
                sb.AppendLine($"{stepNumber}. En {targetLocation}: {actionsText}.");
            }
            else
            {
                double legMeters = planner.DistanceBetween(currentLocation, targetLocation);
                accumulatedMeters += legMeters;
                List<string> path = planner.PathBetween(currentLocation, targetLocation);

                sb.AppendLine($"{stepNumber}. Ir de {currentLocation} a {targetLocation} ({legMeters:0} m): {actionsText}.");
                sb.AppendLine($"   Camino: {string.Join(" -> ", path)}");
                currentLocation = targetLocation;
            }

            stepNumber++;
        }

        sb.AppendLine();
        sb.AppendLine("Todos los paquetes fueron recogidos y entregados.");
        sb.AppendLine(
            $"Recorrido total: {(accumulatedMeters / 1000.0).ToString("0.00", CultureInfo.InvariantCulture)} km");

        return sb.ToString();
    }

    private static string FormatActions(List<int> deliveries, List<int> pickups)
    {
        var parts = new List<string>();

        if (deliveries.Count > 0)
        {
            string label = deliveries.Count == 1 ? "entregar paquete" : "entregar paquetes";
            parts.Add($"{label} {JoinIds(deliveries)}");
        }

        if (pickups.Count > 0)
        {
            string label = pickups.Count == 1 ? "recoger paquete" : "recoger paquetes";
            parts.Add($"{label} {JoinIds(pickups)}");
        }

        return string.Join(" y ", parts);
    }

    private static string JoinIds(List<int> ids)
    {
        if (ids.Count == 1)
        {
            return ids[0].ToString(CultureInfo.InvariantCulture);
        }

        if (ids.Count == 2)
        {
            return $"{ids[0]} y {ids[1]}";
        }

        return $"{string.Join(", ", ids.Take(ids.Count - 1))} y {ids[^1]}";
    }
}