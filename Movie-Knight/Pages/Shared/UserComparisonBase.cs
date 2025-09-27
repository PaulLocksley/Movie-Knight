using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Movie_Knight.Models;
using Movie_Knight.Services;

namespace Movie_Knight.Pages.Shared;

public abstract class UserComparisonBase : PageModel
{
    protected readonly UserComparisonService _userComparisonService;

    public List<User> ComparisonUsers { get; set; } = new();
    public List<(Movie movieData, double mean, int delta)> SharedMovies { get; set; } = new();
    public double TotalAverageDelta { get; set; }
    public int TotalAverageRating { get; set; }
    public StringBuilder BarGraphData { get; set; } = new();
    public StringBuilder ScatterPlotData { get; set; } = new();
    public StringBuilder RadarPlotData { get; set; } = new();
    public List<Movie> MovieRecs { get; set; } = new();
    public string[] RolesWeCareAbout { get; set; } = {"cast","studio","writer","director"};
    public string[][] DisplayFilterRolesWeCareAbout { get; set; } = { new [] {"cast"}, new [] {"studio","writer","director"} };
    public SortType SortOrder { get; set; } = SortType.DiscordDesc;
    public Dictionary<string, double> UserDeltas { get; set; } = new();
    public double GroupPercentile { get; set; }

    protected UserComparisonBase()
    {
        _userComparisonService = new UserComparisonService();
    }

    protected async Task<IActionResult> ProcessUserComparisonRequest(string? userNames, string? filterString, string? sortString)
    {
        var result = await _userComparisonService.ProcessUserComparison(userNames, filterString, sortString);

        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            return BadRequest(result.ErrorMessage);
        }

        if (result.NoSharedMovies)
        {
            return Partial("_NoSharedMovies");
        }

        if (result.UserNotFound)
        {
            return Partial("_UserNotFound");
        }

        if (!result.Success)
        {
            return BadRequest("An error occurred processing the request");
        }

        // Map result to page properties
        ComparisonUsers = result.ComparisonUsers;
        SharedMovies = result.SharedMovies;
        TotalAverageDelta = result.TotalAverageDelta;
        TotalAverageRating = result.TotalAverageRating;
        MovieRecs = result.MovieRecs;
        SortOrder = result.SortOrder;
        UserDeltas = result.UserDeltas;
        GroupPercentile = result.GroupPercentile;

        return OnProcessingComplete();
    }

    protected abstract IActionResult OnProcessingComplete();

    // Helper methods for common movie operations
    public static string FormatMovieName(string movieName)
    {
        return movieName.Split("-")
            .Select(x => x[..1].ToUpper() + x[1..])
            .Aggregate("", (x, y) => (x + " " + y));
    }

    public static string GetMovieGenres(Movie movie)
    {
        return movie.attributes
            .Where(x => x.role == "genre")
            .Aggregate("", (_, y) => "" + y.name);
    }
}