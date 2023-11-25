using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using Htmx;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Primitives;
using Movie_Knight.Controllers;
using Movie_Knight.Models;
using Movie_Knight.Services;

namespace Movie_Knight.Pages;

public class UserComparison : PageModel
{
    public List<User> ComparisonUsers;
    public List<(Movie movieData,double mean,int delta)> SharedMovies;
    public double totalAverageDelta;
    public int totalAverageRating;
    public StringBuilder lineGraphData = new StringBuilder();
    public async Task<IActionResult> OnGet(string userNames)
    {
        if (!Request.IsHtmx())
        {
            return Page();
        }
        
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

        IDictionary<int, double> averageRatings = new ConcurrentDictionary<int, double>();
        IDictionary<int, int> totalDelta = new ConcurrentDictionary<int, int>();
        foreach (var movieId in sharedList)
        {
            averageRatings[movieId] = ComparisonUsers.Select(x => 
                                            x.userList[movieId]).Sum() /(double) ComparisonUsers.Count;
            totalDelta[movieId] = Convert.ToInt32(ComparisonUsers.Select(x =>
                Math.Abs(x.userList[movieId] - averageRatings[movieId])).Sum());
        }
        
        SharedMovies = sharedList.AsParallel().WithDegreeOfParallelism(12)
            .Select(m => (MovieCache.GetMovie(m),averageRatings[m],totalDelta[m]))
            .ToList();
        SharedMovies = SharedMovies.OrderBy(m => m.Item3).ThenByDescending(m => m.Item2).ToList();
        totalAverageDelta = SharedMovies.Select(x => x.delta).Sum() / (double)SharedMovies.Count;
        totalAverageRating = Convert.ToInt32(SharedMovies.Select(x => x.mean).Sum()) / SharedMovies.Count;


        //Section: Graph Data
        lineGraphData.Append($$"""
            labels: [{{SharedMovies.Select(x => $""" "{x.movieData.name}" """).Aggregate((x,y) => 
                x +"," + y)}}],
            datasets: [
            """);
        foreach (var user in ComparisonUsers)
        {
            lineGraphData.Append($$"""
                                   {
                                   label: '{{user.username}}',
                                   data: [{{
                                       SharedMovies.Select(x => user.userList[x.movieData.id])
                                           .Aggregate("",(x,y) => $"{x},{y}")[1..]
                                       
                                   }}]
                                   },
                                   """);
        }
        lineGraphData.Append($$"""
                               {
                               label: 'Group average',
                               data: [{{
                                   SharedMovies.Select(x => x.mean)
                                       .Aggregate("",(x,y) => $"{x},{y}")[1..]
                                   }}]
                               },{
                               label: 'All User Average',
                               data: [{{
                                   SharedMovies.Select(x => (x.movieData.averageRating))
                                       .Aggregate("",(x,y) => $"{x},{y}")[1..]

                                   }}]
                               }
                               """);
        /*
        lineGraphData.Remove(lineGraphData.Length - 1, 1);
        */
        lineGraphData.Append("]");
        return Partial("_UserComparison", this);
    }
}