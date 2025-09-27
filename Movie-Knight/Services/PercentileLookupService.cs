using System.Globalization;

namespace Movie_Knight.Services;

public class PercentileLookupService
{
    private readonly Dictionary<int, List<(double delta, double percentile)>> _lookupTables;
    
    public PercentileLookupService()
    {
        _lookupTables = LoadPercentileData();
    }
    
    /// <summary>
    /// Gets the percentile for a given delta and group size
    /// </summary>
    /// <param name="delta">The calculated delta value</param>
    /// <param name="groupSize">Number of users in comparison (2-8)</param>
    /// <returns>Percentile (0-100) where 100 = perfect agreement</returns>
    public double GetPercentile(double delta, int groupSize)
    {
        if (!_lookupTables.ContainsKey(groupSize))
        {
            Console.WriteLine($"Warning: No lookup table for group size {groupSize}, returning 50th percentile");
            return 50.0; // Default fallback
        }
        
        var table = _lookupTables[groupSize];
        
        // Handle edge cases
        if (delta <= table[0].delta) return 100.0;
        if (delta >= table[^1].delta) return table[^1].percentile;
        
        // Binary search for efficiency
        int left = 0, right = table.Count - 1;
        
        while (left <= right)
        {
            int mid = (left + right) / 2;
            
            if (Math.Abs(table[mid].delta - delta) < 0.0001) // Close enough for doubles
                return table[mid].percentile;
            
            if (table[mid].delta < delta)
                left = mid + 1;
            else
                right = mid - 1;
        }
        
        // Interpolate between right and left
        if (right >= 0 && left < table.Count)
        {
            var lowerPoint = table[right];
            var upperPoint = table[left];
            
            var deltaRange = upperPoint.delta - lowerPoint.delta;
            if (Math.Abs(deltaRange) < 0.0001) return lowerPoint.percentile; // Avoid division by zero
            
            var percentileRange = upperPoint.percentile - lowerPoint.percentile;
            var deltaOffset = delta - lowerPoint.delta;
            
            return lowerPoint.percentile + (deltaOffset / deltaRange) * percentileRange;
        }
        
        return 50.0; // Fallback
    }
    
    /// <summary>
    /// Gets statistics about the lookup table for a given group size
    /// </summary>
    public (double minDelta, double maxDelta, int entryCount) GetStats(int groupSize)
    {
        if (!_lookupTables.ContainsKey(groupSize))
            return (0, 0, 0);
        
        var table = _lookupTables[groupSize];
        return (table.First().delta, table.Last().delta, table.Count);
    }
    
    private Dictionary<int, List<(double delta, double percentile)>> LoadPercentileData()
    {
        var data = new Dictionary<int, List<(double delta, double percentile)>>();
        var csvPath = Path.Combine("Data", "user_comparison_percentiles.csv");
        
        try
        {
            if (!File.Exists(csvPath))
            {
                Console.WriteLine($"Warning: Percentile data file not found at {csvPath}");
                return data;
            }
            
            var lines = File.ReadAllLines(csvPath).Skip(1); // Skip header
            
            foreach (var line in lines)
            {
                var parts = line.Split(',');
                if (parts.Length >= 3)
                {
                    if (int.TryParse(parts[0], out var groupSize) &&
                        double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var delta) &&
                        double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var percentile))
                    {
                        if (!data.ContainsKey(groupSize))
                            data[groupSize] = new List<(double, double)>();
                        
                        data[groupSize].Add((delta, percentile));
                    }
                }
            }
            
            // Sort by delta for binary search
            foreach (var groupSize in data.Keys)
            {
                data[groupSize] = data[groupSize].OrderBy(x => x.delta).ToList();
            }
            
            Console.WriteLine($"Loaded percentile lookup tables for group sizes: {string.Join(", ", data.Keys.OrderBy(x => x))}");
            foreach (var groupSize in data.Keys.OrderBy(x => x))
            {
                Console.WriteLine($"  Group {groupSize}: {data[groupSize].Count} entries, delta range {data[groupSize].First().delta:F3} - {data[groupSize].Last().delta:F3}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading percentile data: {ex.Message}");
        }
        
        return data;
    }
}