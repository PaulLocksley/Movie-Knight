using System.Diagnostics;
using System.Text;
using Htmx;
using Microsoft.AspNetCore.Mvc;
using Movie_Knight.Models;
using Movie_Knight.Pages.Shared;

namespace Movie_Knight.Pages;

public class UserComparison : UserComparisonBase
{
    public async Task<IActionResult> OnGet(string? userNames, string? filterString, string? sortString)
    {
        var stopWatch = new Stopwatch();
        stopWatch.Start();

        var result = await ProcessUserComparisonRequest(userNames, filterString, sortString);
        
        Console.WriteLine($"Users: {string.Join(" ", ComparisonUsers.Select(x => x.username))})");
        Console.WriteLine($"comparison took: {stopWatch.ElapsedMilliseconds}");

        return result;
    }

    protected override IActionResult OnProcessingComplete()
    {
        if (!Request.IsHtmx())
        {
            return Page();
        }
        return Partial("_UserComparison", this);
    }
}