using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Movie_Knight.Controllers;
using Movie_Knight.Models;
using Movie_Knight.Services;

namespace Movie_Knight.Pages.Shared;

public class _WatchList : PageModel
{
    public List<Movie> Movies = [];

    public async Task<IActionResult> OnGet(string userNames)
    {
        var userService = new UserService(GetHttpClient.GetNamedHttpClient());
        var users = userNames.Split(",")
            .Select(x => x.Trim())
            .ToArray();
        
        if (users.Length is >= 8 or 0)
        {
            return BadRequest("Requested too many users");
        }

        var invalidUserNameRegex = new Regex("[^a-zA-Z0-9_]");
        if (users.Any(x => invalidUserNameRegex.IsMatch(x)))
        {
            return BadRequest("A user contains an invalid character, if this is an error let me know somehow.");
        }

        var tmpMovieList = await userService.FetchWatchList(users[0]);
        foreach (var username in users)
        {
           tmpMovieList = tmpMovieList.Intersect(await userService.FetchWatchList(username)).ToList();
        }
        Movies = tmpMovieList.Select(MovieCache.GetMovie).ToList();
        
        return Page();
    }
}