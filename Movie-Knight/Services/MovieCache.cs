using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Movie_Knight.Controllers;
using Movie_Knight.Models;

namespace Movie_Knight.Services;

public static class MovieCache 
{
    private static IDictionary<int, Movie> _cache = new ConcurrentDictionary<int, Movie>();

    private static UserService userService = new UserService(GetHttpClient.GetNamedHttpClient());
    private static MovieService _movieService = new MovieService(GetHttpClient.GetNamedHttpClient());


    public static Movie GetMovie(int id)
    {
        Movie returnMovie;
        try
        {
            returnMovie = _cache[id];
            //Console.Write("Found from Cache!");

        }
        catch (Exception e)
        {
            //Console.WriteLine(e);
            var movie = _movieService.FetchMovie("film:" + id);
            while(!movie.IsCompleted){ Thread.Sleep(10); }

            if (movie.IsFaulted)
            {
                Console.WriteLine(movie.Exception);
                throw;
            }
            _cache[id] =  movie.Result;
            returnMovie =  movie.Result;
        }
        return returnMovie;
    }
    
}
