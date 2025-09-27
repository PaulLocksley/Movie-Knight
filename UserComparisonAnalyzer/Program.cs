using Microsoft.Data.Sqlite;
using System.Text.Json;

var dbPath = "/home/paull/proj/Movie-Knight/top90users.db";
var outputPath = "user_comparison_percentiles.csv";

Console.WriteLine("Loading ALL users from database into RAM...");
var users = LoadAllUsersFromDatabase(dbPath);
Console.WriteLine($"Loaded {users.Count} users with movie data");

Console.WriteLine("Generating comparison data...");
var results = new List<ComparisonResult>();

// Generate samples across different group sizes
var random = new Random(42);
var sampleSizes = new Dictionary<int, int> { 
    {2, 50000}, {3, 40000}, {4, 30000}, {5, 20000}, {6, 15000}, {7, 10000}, {8, 8000} 
};

foreach (var (groupSize, sampleCount) in sampleSizes)
{
    Console.WriteLine($"Processing group size {groupSize} with {sampleCount} samples...");
    
    for (int i = 0; i < sampleCount; i++)
    {
        var selectedUsers = SelectRandomUsers(users, groupSize, random);
        var result = CalculateUserDeltas(selectedUsers, groupSize);
        results.AddRange(result);
        
        if (i % 500 == 0) Console.WriteLine($"  Completed {i}/{sampleCount} samples");
    }
}

Console.WriteLine($"Generated {results.Count} total comparison results");
Console.WriteLine("Building percentile lookup tables...");

var percentileLookup = new PercentileLookup();
percentileLookup.BuildFromResults(results);

Console.WriteLine("Saving results to CSV...");
SaveResultsToCSV(results, outputPath);

Console.WriteLine("Testing percentile lookup function...");
TestPercentileLookup(percentileLookup);

Console.WriteLine($"Analysis complete! Results saved to {outputPath}");

static Dictionary<string, Dictionary<int, int>> LoadAllUsersFromDatabase(string dbPath)
{
    var users = new Dictionary<string, Dictionary<int, int>>();
    
    using var connection = new SqliteConnection($"Data Source={dbPath}");
    connection.Open();
    
    var command = connection.CreateCommand();
    command.CommandText = "SELECT Username, Data FROM Users";
    
    using var reader = command.ExecuteReader();
    var loadedCount = 0;
    var skippedCount = 0;
    
    while (reader.Read())
    {
        var username = reader.GetString(0);
        var jsonData = reader.GetString(1);
        
        try
        {
            var movieRatings = JsonSerializer.Deserialize<Dictionary<int, int>>(jsonData);
            if (movieRatings != null && movieRatings.Count >= 20) // Minimum 20 rated movies
            {
                users[username] = movieRatings;
                loadedCount++;
            }
            else
            {
                skippedCount++;
            }
        }
        catch (JsonException)
        {
            Console.WriteLine($"Failed to parse data for user: {username}");
            skippedCount++;
        }
    }
    
    Console.WriteLine($"  Loaded: {loadedCount}, Skipped: {skippedCount}");
    return users;
}

static List<KeyValuePair<string, Dictionary<int, int>>> SelectRandomUsers(
    Dictionary<string, Dictionary<int, int>> users, int count, Random random)
{
    var userArray = users.ToArray();
    var selected = new List<KeyValuePair<string, Dictionary<int, int>>>();
    
    var indices = new HashSet<int>();
    while (indices.Count < count && indices.Count < userArray.Length)
    {
        indices.Add(random.Next(userArray.Length));
    }
    
    foreach (var index in indices)
    {
        selected.Add(userArray[index]);
    }
    
    return selected;
}

//claude what are you doing re-writing logic???
static List<ComparisonResult> CalculateUserDeltas(
    List<KeyValuePair<string, Dictionary<int, int>>> selectedUsers, int groupSize)
{
    // Find shared movies (intersection of all users' movie lists)
    var sharedMovies = selectedUsers[0].Value.Keys.ToHashSet();
    for (int i = 1; i < selectedUsers.Count; i++)
    {
        sharedMovies.IntersectWith(selectedUsers[i].Value.Keys);
    }
    
    if (sharedMovies.Count < 5) // Need minimum shared movies
        return new List<ComparisonResult>();
    
    var results = new List<ComparisonResult>();
    
    // Calculate average ratings for shared movies
    var averageRatings = new Dictionary<int, double>();
    foreach (var movieId in sharedMovies)
    {
        var totalRating = selectedUsers.Sum(u => u.Value[movieId]);
        averageRatings[movieId] = totalRating / (double)selectedUsers.Count;
    }
    
    // Calculate delta for each user (matches UserComparisonService.cs:157-159)
    foreach (var user in selectedUsers)
    {
        var userTotal = 0;
        var meanTotal = 0.0;
        
        foreach (var movieId in sharedMovies)
        {
            userTotal += user.Value[movieId];
            meanTotal += averageRatings[movieId];
        }
        
        var delta = Math.Abs(userTotal - meanTotal) / sharedMovies.Count;
        
        results.Add(new ComparisonResult
        {
            Username = user.Key,
            GroupSize = groupSize,
            SharedMovieCount = sharedMovies.Count,
            Delta = delta
        });
    }
    
    return results;
}

static void SaveResultsToCSV(List<ComparisonResult> results, string outputPath)
{
    var output = new List<string> { "GroupSize,DeltaValue,Percentile,SharedMovies" };

    foreach (var groupSize in results.Select(r => r.GroupSize).Distinct().OrderBy(x => x))
    {
        var groupResults = results.Where(r => r.GroupSize == groupSize).OrderBy(r => r.Delta).ToList();
        Console.WriteLine($"Group size {groupSize}: {groupResults.Count} results, delta range {groupResults.First().Delta:F3} - {groupResults.Last().Delta:F3}");
        
        for (int i = 0; i < groupResults.Count; i++)
        {
            var percentile = 100.0 * (groupResults.Count - i) / groupResults.Count;
            output.Add($"{groupSize},{groupResults[i].Delta:F3},{percentile:F1},{groupResults[i].SharedMovieCount}");
        }
    }

    File.WriteAllLines(outputPath, output);
    
    // Print summary statistics
    Console.WriteLine("Summary statistics:");
    foreach (var groupSize in results.Select(r => r.GroupSize).Distinct().OrderBy(x => x))
    {
        var groupResults = results.Where(r => r.GroupSize == groupSize).ToList();
        var avgDelta = groupResults.Average(r => r.Delta);
        var medianDelta = groupResults.OrderBy(r => r.Delta).ElementAt(groupResults.Count / 2).Delta;
        var avgSharedMovies = groupResults.Average(r => r.SharedMovieCount);
        
        Console.WriteLine($"  Group {groupSize}: Avg Delta={avgDelta:F3}, Median Delta={medianDelta:F3}, Avg Shared Movies={avgSharedMovies:F0}");
    }
}

static void TestPercentileLookup(PercentileLookup lookup)
{
    Console.WriteLine("\nTesting percentile lookup function:");
    var testCases = new[] { 0.0, 0.5, 1.0, 1.5, 2.0, 3.0, 5.0 };
    
    foreach (var testDelta in testCases)
    {
        Console.WriteLine($"Delta {testDelta:F1}:");
        for (int groupSize = 2; groupSize <= 8; groupSize++)
        {
            try
            {
                var percentile = lookup.GetPercentile(testDelta, groupSize);
                Console.WriteLine($"  Group {groupSize}: {percentile:F1}th percentile");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"  Group {groupSize}: {ex.Message}");
            }
        }
        Console.WriteLine();
    }
}

public class ComparisonResult
{
    public string Username { get; set; } = "";
    public int GroupSize { get; set; }
    public int SharedMovieCount { get; set; }
    public double Delta { get; set; }
}

public class PercentileLookup
{
    private readonly Dictionary<int, List<(double delta, double percentile)>> _lookupTables;
    
    public PercentileLookup()
    {
        _lookupTables = new Dictionary<int, List<(double delta, double percentile)>>();
    }
    
    public void BuildFromResults(List<ComparisonResult> results)
    {
        foreach (var groupSize in results.Select(r => r.GroupSize).Distinct().OrderBy(x => x))
        {
            var groupResults = results.Where(r => r.GroupSize == groupSize)
                                   .OrderBy(r => r.Delta)
                                   .ToList();
            
            var lookupTable = new List<(double delta, double percentile)>();
            
            for (int i = 0; i < groupResults.Count; i++)
            {
                var percentile = 100.0 * (groupResults.Count - i) / groupResults.Count;
                lookupTable.Add((groupResults[i].Delta, percentile));
            }
            
            _lookupTables[groupSize] = lookupTable;
            Console.WriteLine($"  Built lookup table for group size {groupSize}: {lookupTable.Count} entries");
        }
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
            throw new ArgumentException($"No lookup table for group size {groupSize}");
        
        var table = _lookupTables[groupSize];
        
        // Handle edge cases
        if (delta <= table[0].delta) return 100.0;
        if (delta >= table[^1].delta) return table[^1].percentile;
        
        // Binary search for efficiency
        int left = 0, right = table.Count - 1;
        
        while (left <= right)
        {
            int mid = (left + right) / 2;
            
            if (table[mid].delta == delta)
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
            var percentileRange = upperPoint.percentile - lowerPoint.percentile;
            var deltaOffset = delta - lowerPoint.delta;
            
            return lowerPoint.percentile + (deltaOffset / deltaRange) * percentileRange;
        }
        
        return 0.0;
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
}
