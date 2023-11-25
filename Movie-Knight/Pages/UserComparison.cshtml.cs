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
    public List<Movie> SharedMovies;

    
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
            Console.WriteLine("Found all users.");
            SharedMovies = sharedList.AsParallel().WithDegreeOfParallelism(12)
                .Select(MovieCache.GetMovie)
                .ToList();
            ComparisonUsers.Add(
                new User(username,
                    userList,
                    null));
            Console.WriteLine($"UserList length {userList.Count} sharedCount = {sharedList.Count}");
        }


        return Partial("_UserComparison", this);
    }
}