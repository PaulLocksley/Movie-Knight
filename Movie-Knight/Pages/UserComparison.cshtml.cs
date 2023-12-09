using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Htmx;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Primitives;
using Movie_Knight.Controllers;
using Movie_Knight.Models;
using Movie_Knight.Services;
using HostingEnvironmentExtensions = Microsoft.AspNetCore.Hosting.HostingEnvironmentExtensions;

namespace Movie_Knight.Pages;

public class UserComparison : PageModel
{
    public List<User> ComparisonUsers;
    public List<(Movie movieData,double mean,int delta)> SharedMovies;
    public double totalAverageDelta;
    public int totalAverageRating;
    public StringBuilder barGraphData = new();
    public StringBuilder scatterPlotData = new();
    public StringBuilder radarPlotData = new();
    public List<Movie> movieRecs = new();
    public string[] rolesWeCareAbout = {"cast","studio","writer","director"};

    public Dictionary<string, double> userDeltas = new();
    public async Task<IActionResult> OnGet(string userNames, string? filterString)
    {
        var stopWatch = new Stopwatch();
        stopWatch.Start();
        Filter[]? filters = null;
        if (filterString is not null)
        {
            filters = JsonSerializer.Deserialize<Filter[]>(filterString);
        }

        #region User Comparison

        var users = userNames.Split(",")
            .Select(x => x.Trim())
            .ToArray();

        if (users.Length >= 8)
        {
            return BadRequest("Requested too many users");
        }

        var invalidUserNameRegex = new Regex("[^a-zA-Z0-9_]");
        if (users.Any(x => invalidUserNameRegex.IsMatch(x)))
        {
            return BadRequest("A user contains an invalid character, if this is an error let me know somehow.");
        }

        ComparisonUsers = new List<User>();
        var userService = new UserService(GetHttpClient.GetNamedHttpClient());

        var sharedList = new HashSet<int>();
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
        if (filters is not null){
            //todo: I think performance here will be bad. Look into a more elegant solution.
            foreach (var filter in filters)
            {
                if(filter.type == Filter.Types.Require)
                {
                    SharedMovies = SharedMovies.Where(m =>
                        {
                            return m.movieData.attributes.Any(x => x.role == filter.role && x.name == filter.name);
                        })
                        .ToList();
                    continue;
                }
                SharedMovies = SharedMovies.Where(m =>
                    {
                        return m.movieData.attributes.Any(x => x.role != filter.role && x.name != filter.name);
                    })
                    .ToList();
                
            }

            if (!SharedMovies.Any())
            {
                return BadRequest();
            }
        }
        
        
        //Final parsing.
        SharedMovies = SharedMovies.OrderBy(m => m.Item3).ThenByDescending(m => m.Item2).ToList();
        totalAverageDelta = SharedMovies.Select(x => x.delta).Sum() / (double)SharedMovies.Count;
        totalAverageRating = Convert.ToInt32(SharedMovies.Select(x => x.mean).Sum()) / SharedMovies.Count;

        foreach (var user in ComparisonUsers)
        {
            var userTotal = 0;
            var meanTotal = 0.0;
            foreach (var m in SharedMovies)
            {
                userTotal += user.userList[m.movieData.id];
                meanTotal += m.mean;
            }
            userDeltas[user.username] = Math.Round(Math.Abs(userTotal - meanTotal) / SharedMovies.Count,3);

        }
        
        #endregion
        
        
        
        Console.WriteLine($"comparison took: {stopWatch.ElapsedMilliseconds}");
        stopWatch.Reset();
        stopWatch.Start();
        
        #region graphing section
        barGraphData.Append($$"""
            labels: [{{SharedMovies.Chunk(30).Last()
                .Select(x => $""" "{x.movieData.name}" """).Aggregate((x,y) => 
                x +"," + y)}}],
            datasets: [
            """);
        foreach (var user in ComparisonUsers)
        {
            barGraphData.Append($$"""
                                   {
                                   label: '{{user.username}}',
                                   data: [{{
                                       SharedMovies.Chunk(30).Last().Select(x => user.userList[x.movieData.id])
                                           .Aggregate("",(x,y) => $"{x},{y}")[1..]
                                       
                                   }}]
                                   },
                                   """);
        }
        barGraphData.Append($$"""
                               {
                               label: 'Group average',
                               data: [{{
                                   SharedMovies.Chunk(30).Last().Select(x => x.mean)
                                       .Aggregate("",(x,y) => $"{x},{y}")[1..]
                                   }}]
                               },{
                               label: 'All User Average',
                               data: [{{
                                   SharedMovies.Chunk(30).Last().Select(x => (x.movieData.averageRating))
                                       .Aggregate("",(x,y) => $"{x},{y}")[1..]

                                   }}]
                               }
                               """);

        barGraphData.Append("]");

        var personData = new Dictionary<(string role, string name), (int frequency, double score)>();
        foreach (var movie in SharedMovies)
        {
            foreach ((string role, string name)person in movie.movieData.attributes
                         .Where(x => rolesWeCareAbout.Contains(x.role)))
            {
                if (personData.ContainsKey(person))
                {
                    personData[person] = (personData[person].frequency+1,
                        personData[person].score + movie.mean);
                }
                else
                {
                    personData[person] = (1,
                        movie.mean);
                }
            }
        }
        foreach (var person in personData.Keys)
        {
            personData[person] = (personData[person].frequency,
                personData[person].score / personData[person].frequency);
        }
        //put above data into table
        scatterPlotData.Append($$"""
                                 {
                                 labels: [{{rolesWeCareAbout.Select(x=>$"'{x}'").Aggregate((x,y)=>x+","+y)}}],
                                 datasets:[
                                 
                                 """);
        foreach (var role in rolesWeCareAbout)
        {
            var tempItems = personData
                .Where(p => p.Key.role == role && p.Value.frequency > 1);
                
            scatterPlotData.Append($$"""
                                                          {
                                                          label: '{{role}}',
                                                          data: [
                                     """);
            if (tempItems.Any())
                scatterPlotData.Append(tempItems.Select(x => $$"""
                                                               { x: {{x.Value.frequency}}, y: {{x.Value.score}},name:'{{x.Key.name}}' }
                                                               """).Aggregate((x, y) => (x + "," + y)));
            scatterPlotData.Append("]},");
        }

        scatterPlotData.Remove(scatterPlotData.Length - 1, 1);
        scatterPlotData.Append("]}");
        var dictscores = new Dictionary<string, (int count, double sum)>();
        foreach (var m in SharedMovies.Where(m => m.movieData.averageRating.HasValue))
        {
            foreach (var genre in m.movieData.attributes.Where(x => x.role == "genre"))
            {
                if (dictscores.ContainsKey(genre.name))
                {
                    dictscores[genre.name] = (dictscores[genre.name].count + 1,
                        dictscores[genre.name].sum + m.movieData.averageRating!.Value);
                }
                else
                {
                    dictscores[genre.name] = (1, m.movieData.averageRating!.Value);
                }
            }
        }
        radarPlotData.Append($$"""
                               {
                               labels: [{{dictscores.Select(x=>$"'{x.Key}'").Aggregate((x,y)=>x+","+y)}}],
                               datasets:[{
                               label: 'Shared genrescoress',
                               data: [{{dictscores.Select(x => ""+x.Value.sum/x.Value.count)
                                   .Aggregate((x,y) => x+","+y)}}
                                   ]}]
                                   }
                               """);
        
        
        #endregion
        Console.WriteLine($"Graphing took: {stopWatch.ElapsedMilliseconds}");
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
            movieRecs.Add(MovieCache.GetMovie(movieRec.Key));
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