using System.Collections.Concurrent;
using Htmx;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Movie_Knight.Controllers;
using Movie_Knight.Models;
using Movie_Knight.Services;

namespace Movie_Knight.Pages;

public class UserComparison : PageModel
{
    public List<User> ComparisonUsers;
    public List<(Movie,int mean,int delta)> SharedMovies;
    public int totalAverageDelta;
    public int totalAverageRating;
    
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

        IDictionary<int, int> averageRatings = new ConcurrentDictionary<int, int>();
        IDictionary<int, int> totalDelta = new ConcurrentDictionary<int, int>();
        foreach (var movieId in sharedList)
        {
            averageRatings[movieId] = ComparisonUsers.Select(x => 
                                            x.userList[movieId]).Sum() / ComparisonUsers.Count;
            totalDelta[movieId] = ComparisonUsers.Select(x =>
                (int)Math.Abs(x.userList[movieId] - averageRatings[movieId])).Sum();
        }
        
        SharedMovies = sharedList.AsParallel().WithDegreeOfParallelism(12)
            .Select(m => (MovieCache.GetMovie(m),averageRatings[m],totalDelta[m]))
            .ToList();
        SharedMovies = SharedMovies.OrderBy(m => m.Item3).ThenByDescending(m => m.Item2).ToList();
        totalAverageDelta = SharedMovies.Select(x => x.delta).Sum() / SharedMovies.Count;
        totalAverageRating = SharedMovies.Select(x => x.mean).Sum() / SharedMovies.Count;
        
        return Partial("_UserComparison", this);
    }
}