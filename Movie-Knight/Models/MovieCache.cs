using System.Collections.Concurrent;
using Movie_Knight.Services;

namespace Movie_Knight.Models;

public static class MovieCache 
{
    private static IDictionary<int, Movie> _cache = new ConcurrentDictionary<int, Movie>();
    private static MovieService _movieService = new MovieService();
    /*public MovieCache()
    {
        _cache = new Dictionary<int, Movie>();
        _movieService = new MovieService();
    }*/

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
            _cache[id] =  movie.Result;
            returnMovie =  movie.Result;
        }
        return returnMovie;
    }
}