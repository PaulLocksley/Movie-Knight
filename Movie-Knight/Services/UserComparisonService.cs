using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Movie_Knight.Controllers;
using Movie_Knight.Models;

namespace Movie_Knight.Services;

public class UserComparisonService
{
    private readonly UserService _userService;
    private readonly PercentileLookupService _percentileLookup;

    public UserComparisonService(PercentileLookupService percentileLookup)
    {
        _userService = new UserService(GetHttpClient.GetNamedHttpClient());
        _percentileLookup = percentileLookup;
    }

    public async Task<UserComparisonResult> ProcessUserComparison(string? userNames, string? filterString, string? sortString)
    {
        var result = new UserComparisonResult();
        
        Filter[]? filters = null;
        result.ComparisonUsers = new List<User>();
        var sharedList = new HashSet<int>();
        
        if (filterString is not null)
        {
            filters = JsonSerializer.Deserialize<Filter[]>(filterString);
        }

        if (sortString is not null)
        {
            result.SortOrder = Enum.Parse<SortType>(sortString);
        }

        // User validation and fetching
        if (string.IsNullOrWhiteSpace(userNames))
        {
            result.ErrorMessage = "Please provide user name/s";
            return result;
        }
        
        var users = userNames.Split([','], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (users.Length >= 8)
        {
            result.ErrorMessage = "Requested too many users";
            return result;
        }

        var invalidUserNameRegex = new Regex("[^a-zA-Z0-9_]");
        if (users.Any(x => invalidUserNameRegex.IsMatch(x)))
        {
            result.ErrorMessage = "A user contains an invalid character, if this is an error let me know somehow.";
            return result;
        }

        try
        {
            foreach (var username in users)
            {
                var userList = await _userService.FetchUser(username);
                if (sharedList.Count == 0)
                {
                    sharedList = userList.Select(x => x.Key).ToHashSet();
                }
                else
                {
                    sharedList.IntersectWith(userList.Select(x => x.Key));
                }

                result.ComparisonUsers.Add(new User(username, userList, null));
                Console.WriteLine($"UserList length {userList.Count} sharedCount = {sharedList.Count}");
                
                if(sharedList.Count == 0)
                {   
                    result.NoSharedMovies = true;
                    return result;
                }
            }

            // Delta calculations
            IDictionary<int, double> averageRatings = new ConcurrentDictionary<int, double>();
            IDictionary<int, int> totalDelta = new ConcurrentDictionary<int, int>();
            
            foreach (var movieId in sharedList)
            {
                averageRatings[movieId] = result.ComparisonUsers.Select(x =>
                    x.userList[movieId]).Sum() / (double)result.ComparisonUsers.Count;
                totalDelta[movieId] = Convert.ToInt32(result.ComparisonUsers.Select(x =>
                    Math.Abs(x.userList[movieId] - averageRatings[movieId])).Sum());
            }

            result.SharedMovies = sharedList.AsParallel().WithDegreeOfParallelism(12)
                .Select(m => (MovieCache.GetMovie(m), averageRatings[m], totalDelta[m]))
                .ToList();

            // Apply filters
            if (filters is not null)
            {
                foreach (var filter in filters)
                {
                    if (filter.type == Filter.Types.Require)
                    {
                        result.SharedMovies = result.SharedMovies.Where(m =>
                            {
                                return m.movieData.attributes.Any(x =>
                                    x.role == filter.role && x.name == filter.name);
                            })
                            .ToList();
                        continue;
                    }

                    result.SharedMovies = result.SharedMovies.Where(m =>
                        {
                            return m.movieData.attributes.All(
                                x => !(x.role == filter.role && x.name == filter.name));
                        })
                        .ToList();
                }

                if (!result.SharedMovies.Any())
                {
                    result.ErrorMessage = "No movies match the applied filters";
                    return result;
                }
            }

            // Sort movies
            result.SharedMovies = result.SortOrder switch
            {
                SortType.Popularity => result.SharedMovies.OrderBy(m => m.movieData.RatingCount).ToList(),
                SortType.PopularityDesc => result.SharedMovies.OrderByDescending(m => m.movieData.RatingCount).ToList(),
                SortType.AverageRating => result.SharedMovies.OrderBy(m => m.mean).ToList(),
                SortType.AverageRatingDesc => result.SharedMovies.OrderByDescending(m => m.mean).ToList(),
                SortType.Discord => result.SharedMovies.OrderBy(m => m.delta).ToList(),
                _ => result.SharedMovies.OrderByDescending(m => m.Item3).ThenByDescending(m => m.Item2).ToList()
            };

            // Calculate totals
            result.TotalAverageDelta = result.SharedMovies.Count > 0 ? 
                result.SharedMovies.Select(x => x.delta).Sum() / (double)result.SharedMovies.Count : 0;
            result.TotalAverageRating = result.SharedMovies.Count > 0 ? 
                Convert.ToInt32(result.SharedMovies.Select(x => x.mean).Sum()) / result.SharedMovies.Count : 0;

            // Calculate user deltas
            foreach (var user in result.ComparisonUsers)
            {
                var userTotal = 0;
                var meanTotal = 0.0;
                foreach (var m in result.SharedMovies)
                {
                    userTotal += user.userList[m.movieData.id];
                    meanTotal += m.mean;
                }
                result.UserDeltas[user.username] = result.SharedMovies.Count > 0 ? 
                    Math.Round(Math.Abs(userTotal - meanTotal) / result.SharedMovies.Count, 3) : 0;
            }

            // Calculate group percentile
            if (result.UserDeltas.Count > 0)
            {
                var averageGroupDelta = result.UserDeltas.Values.Average();
                result.GroupPercentile = Math.Round(_percentileLookup.GetPercentile(averageGroupDelta, result.ComparisonUsers.Count), 1);
            }

            // Generate recommendations
            result.MovieRecs = GenerateRecommendations(result.ComparisonUsers, result.SharedMovies);

            result.Success = true;
        }
        catch (FileNotFoundException)
        {
            result.UserNotFound = true;
        }

        return result;
    }

    private List<Movie> GenerateRecommendations(List<User> comparisonUsers, List<(Movie movieData, double mean, int delta)> sharedMovies)
    {
        var watchedMovies = comparisonUsers.Select(x => x.userList.Keys)
            .Aggregate(new HashSet<int>(), (x, y) =>
            {
                foreach (var i in y)
                {
                    x.Add(i);
                }
                return x;
            });

        var recommendationCounts = new Dictionary<int, int>();
        var rc = sharedMovies.Where(x => x.mean > 8)
            .Select(x => x.movieData.relatedFilms).Aggregate(new List<int>(), (o, t) =>
            {
                o.AddRange(t);
                return o;
            });

        foreach (var movieRec in rc.Where(x => !watchedMovies.Contains(x)))
        {
            if (recommendationCounts.ContainsKey(movieRec))
            {
                recommendationCounts[movieRec]++;
            }
            else
            {
                recommendationCounts[movieRec] = 1;
            }
        }

        var movieRecs = new List<Movie>();
        foreach (var movieRec in recommendationCounts.Where(x => x.Value > 1)
                     .OrderByDescending(x => x.Value))
        {
            movieRecs.Add(MovieCache.GetMovie(movieRec.Key));
        }

        return movieRecs;
    }
}

public class UserComparisonResult
{
    public List<User> ComparisonUsers { get; set; } = new();
    public List<(Movie movieData, double mean, int delta)> SharedMovies { get; set; } = new();
    public double TotalAverageDelta { get; set; }
    public int TotalAverageRating { get; set; }
    public List<Movie> MovieRecs { get; set; } = new();
    public SortType SortOrder { get; set; } = SortType.DiscordDesc;
    public Dictionary<string, double> UserDeltas { get; set; } = new();
    public double GroupPercentile { get; set; }
    
    // Status flags
    public bool Success { get; set; }
    public bool NoSharedMovies { get; set; }
    public bool UserNotFound { get; set; }
    public string? ErrorMessage { get; set; }
}