using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Htmx;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Movie_Knight.Controllers;
using Movie_Knight.Models;
using Movie_Knight.Services;

namespace Movie_Knight.Pages;

public class UserComparison : PageModel
{
    public required List<User> ComparisonUsers;
    public required List<(Movie movieData,double mean,int delta)> SharedMovies;
    public double TotalAverageDelta;
    public int TotalAverageRating;
    public StringBuilder BarGraphData = new();
    public StringBuilder ScatterPlotData = new();
    public StringBuilder RadarPlotData = new();
    public List<Movie> MovieRecs = new();
    public string[] RolesWeCareAbout = {"cast","studio","writer","director"};
    public string[][] DisplayFilterRolesWeCareAbout = { new [] {"cast"}, new [] {"studio","writer","director"} };
    public SortType SortOrder = SortType.DiscordDesc;
    public Dictionary<string, double> UserDeltas = new();
    public async Task<IActionResult> OnGet(string? userNames, string? filterString, string? sortString)
    {
        var stopWatch = new Stopwatch();
        stopWatch.Start();
        Filter[]? filters = null;
        ComparisonUsers = new List<User>();
        var userService = new UserService(GetHttpClient.GetNamedHttpClient());

        var sharedList = new HashSet<int>();
        
        if (filterString is not null)
        {
            filters = JsonSerializer.Deserialize<Filter[]>(filterString);
        }

        if (sortString is not null)
        {
            SortOrder = Enum.Parse<SortType>(sortString);
            Console.WriteLine(SortOrder);
        }

        #region User Comparison

        try
        {
            if (string.IsNullOrWhiteSpace(userNames))
            {
                return BadRequest("Please provide user name/s");
            }
            var users = userNames.Split([','], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            if (users.Length >= 8)
            {
                return BadRequest("Requested too many users");
            }

            var invalidUserNameRegex = new Regex("[^a-zA-Z0-9_]");
            if (users.Any(x => invalidUserNameRegex.IsMatch(x)))
            {
                return BadRequest("A user contains an invalid character, if this is an error let me know somehow.");
            }

            foreach (var username in users)
            {
                var userList = await userService.FetchUser(username);
                if (sharedList.Count == 0)
                {
                    sharedList = userList.Select(x => x.Key).ToHashSet();
                }
                else
                {
                    sharedList.IntersectWith(userList.Select(x => x.Key));
                }


                ComparisonUsers.Add(
                    new User(username,
                        userList,
                        null));
                Console.WriteLine($"UserList length {userList.Count} sharedCount = {sharedList.Count}");
                if(sharedList.Count == 0)
                {
                    return Partial("_NoSharedMovies");
                }
            }

            //Delta calculations.
            IDictionary<int, double> averageRatings = new ConcurrentDictionary<int, double>();
            IDictionary<int, int> totalDelta = new ConcurrentDictionary<int, int>();
            foreach (var movieId in sharedList)
            {
                averageRatings[movieId] = ComparisonUsers.Select(x =>
                    x.userList[movieId]).Sum() / (double)ComparisonUsers.Count;
                totalDelta[movieId] = Convert.ToInt32(ComparisonUsers.Select(x =>
                    Math.Abs(x.userList[movieId] - averageRatings[movieId])).Sum());
            }

            SharedMovies = sharedList.AsParallel().WithDegreeOfParallelism(12)
                .Select(m => (MovieCache.GetMovie(m), averageRatings[m], totalDelta[m]))
                .ToList();
            //Filters
            if (filters is not null)
            {
                //todo: I think performance here will be bad. Look into a more elegant solution.
                foreach (var filter in filters)
                {
                    if (filter.type == Filter.Types.Require)
                    {
                        SharedMovies = SharedMovies.Where(m =>
                            {
                                return m.movieData.attributes.Any(x =>
                                    x.role == filter.role && x.name == filter.name);
                            })
                            .ToList();
                        continue;
                    }

                    SharedMovies = SharedMovies.Where(m =>
                        {
                            return m.movieData.attributes.All(
                                x => !(x.role == filter.role && x.name == filter.name));
                        })
                        .ToList();

                }

                if (!SharedMovies.Any())
                {
                    return BadRequest();
                }
            }
        }
        catch (FileNotFoundException)
        {
            return Partial("_UserNotFound");
        }
        

        //Final parsing.
        SharedMovies = SortOrder switch

        {
            SortType.Popularity => SharedMovies.OrderBy(m => m.movieData.RatingCount).ToList(),
            SortType.PopularityDesc => SharedMovies.OrderByDescending(m => m.movieData.RatingCount).ToList(),
            SortType.AverageRating =>  SharedMovies.OrderBy(m => m.mean).ToList(),
            SortType.AverageRatingDesc => SharedMovies.OrderByDescending(m => m.mean).ToList(),
            SortType.Discord => SharedMovies.OrderBy(m => m.delta).ToList(),
            _ => SharedMovies.OrderByDescending(m => m.Item3).ThenByDescending(m => m.Item2).ToList()
        };
        TotalAverageDelta = SharedMovies.Select(x => x.delta).Sum() / (double)SharedMovies.Count;
        TotalAverageRating = Convert.ToInt32(SharedMovies.Select(x => x.mean).Sum()) / SharedMovies.Count;

        foreach (var user in ComparisonUsers)
        {
            var userTotal = 0;
            var meanTotal = 0.0;
            foreach (var m in SharedMovies)
            {
                userTotal += user.userList[m.movieData.id];
                meanTotal += m.mean;
            }
            UserDeltas[user.username] = Math.Round(Math.Abs(userTotal - meanTotal) / SharedMovies.Count,3);

        }
        
        #endregion
        
        
        
        Console.WriteLine($"comparison took: {stopWatch.ElapsedMilliseconds}");
        stopWatch.Reset();
        stopWatch.Start();
        
        
        stopWatch.Reset();
        stopWatch.Start();
        #region recomendations
        var watchedMovies = ComparisonUsers.Select(x => x.userList.Keys)
            .Aggregate(new HashSet<int>(), (x, y) =>
            {
                foreach (var i in y)
                {
                    x.Add(i);
                }

                return x;
            });
        var recomendationCounts = new Dictionary<int, int>();
        var rc = SharedMovies.Where(x => x.mean > 8)
            .Select(x => x.movieData.relatedFilms).Aggregate(new List<int>(), (o, t) =>
            {
                o.AddRange(t);
                return o;
            });
        foreach (var movieRec in rc.Where(x => !watchedMovies.Contains(x)))
        {
            if (recomendationCounts.ContainsKey(movieRec)) {
                recomendationCounts[movieRec]++;
            } else {
                recomendationCounts[movieRec] = 1;
            }
        }
        
        foreach (var movieRec in recomendationCounts.Where(x => x.Value > 1)
                     .OrderByDescending(x => x.Value))
        {
            MovieRecs.Add(MovieCache.GetMovie(movieRec.Key));
        }
        #endregion
        Console.WriteLine($"Recs took: {stopWatch.ElapsedMilliseconds}");
        if (!Request.IsHtmx())
        {
            return Page();
        }
        return Partial("_UserComparison", this);
    }
}