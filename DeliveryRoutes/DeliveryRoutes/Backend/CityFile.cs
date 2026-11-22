using System.Globalization;

namespace Backend;

/// <summary>
/// Holds the loaded city graph along with street statistics for reporting.
/// </summary>
public readonly record struct CityMap(Graph<string> Graph, int TotalStreets, int OneWayStreets);

/// <summary>
/// Loads and validates a city map from a flat text file.
/// </summary>
public static class CityFile
{
    public static CityMap Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"City file not found: '{path}'.", path);
        }

        // Case-sensitive vertex names (e.g., "A", "AL", "CV").
        var graph = new Graph<string>(directed: true, comparer: StringComparer.Ordinal);
        var seenStreets = new HashSet<(string First, string Second)>();

        int totalStreets = 0;
        int oneWayStreets = 0;

        string[] lines = File.ReadAllLines(path);

        for (int i = 0; i < lines.Length; i++)
        {
            int lineNumber = i + 1;
            string rawLine = lines[i].Trim();

            // Skip blank lines and comments.
            if (string.IsNullOrEmpty(rawLine) || rawLine.StartsWith('#'))
            {
                continue;
            }

            string[] parts = rawLine.Split(',');
            if (parts.Length != 4)
            {
                throw new FormatException(
                    $"[{path}:{lineNumber}] Expected 4 comma-separated fields (from,to,distance,direction), got {parts.Length}.");
            }

            string from = parts[0].Trim();
            string to = parts[1].Trim();
            string distanceText = parts[2].Trim();
            string directionText = parts[3].Trim();

            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
            {
                throw new FormatException(
                    $"[{path}:{lineNumber}] Location names cannot be empty.");
            }

            if (string.Equals(from, to, StringComparison.Ordinal))
            {
                throw new FormatException(
                    $"[{path}:{lineNumber}] Street cannot start and end at the same location ('{from}').");
            }

            if (!double.TryParse(distanceText, NumberStyles.Float, CultureInfo.InvariantCulture, out double distance)
                || distance <= 0)
            {
                throw new FormatException(
                    $"[{path}:{lineNumber}] Invalid distance '{distanceText}'. Must be a number greater than 0.");
            }

            bool isTwoWay = directionText.Equals("TwoWay", StringComparison.OrdinalIgnoreCase);
            bool isOneWay = directionText.Equals("OneWay", StringComparison.OrdinalIgnoreCase);

            if (!isTwoWay && !isOneWay)
            {
                throw new FormatException(
                    $"[{path}:{lineNumber}] Invalid direction '{directionText}'. Expected 'TwoWay' or 'OneWay'.");
            }

            // Normalize pair so (A, B) and (B, A) are treated as the same street.
            var streetKey = StringComparer.Ordinal.Compare(from, to) < 0
                ? (from, to)
                : (to, from);

            if (!seenStreets.Add(streetKey))
            {
                throw new FormatException(
                    $"[{path}:{lineNumber}] Duplicate street between '{from}' and '{to}'.");
            }

            graph.AddEdge(from, to, distance);

            if (isTwoWay)
            {
                graph.AddEdge(to, from, distance);
            }
            else
            {
                oneWayStreets++;
            }

            totalStreets++;
        }

        if (totalStreets == 0)
        {
            throw new FormatException($"[{path}] The city file does not contain any valid streets.");
        }

        return new CityMap(graph, totalStreets, oneWayStreets);
    }
}