using Htmx;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Movie_Knight.Models;
using Movie_Knight.Services;

namespace Movie_Knight.Pages.Shared;

public class _filters : PageModel
{
    public Movie? movie;
    public string[][] displayFilterRolesWeCareAbout = { new [] {"cast"}, new [] {"studio","writer","director"} };
    
    public void OnGet(int movieId)
    {
        movie = MovieCache.GetMovie(movieId);

    }
}