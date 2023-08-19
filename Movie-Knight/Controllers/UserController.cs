using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Movie_Knight.Models;
using Movie_Knight.Services;
//using System.Text.Json;
using Newtonsoft.Json;
namespace Movie_Knight.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController
{
    private UserService userService;
    private MovieService movieService;
    private ConcurrentDictionary<int, Movie> MovieCache;
    private JsonSerializer jsonSerializer;
    public UserController()
    {
        this.userService = new UserService();
        this.movieService = new MovieService(); //is this bad??
        this.jsonSerializer = Newtonsoft.Json.JsonSerializer.Create(); //for some the default encoding doesn't work here.
        this.MovieCache = new ConcurrentDictionary<int, Movie>();
    }

    [HttpGet("username")]
    public async Task<string> GetByUserName(string username, bool getMovies=false)
    {
        var s = Stopwatch.StartNew();

        var movieList = await userService.FetchUser(username);
        var MovieDict = new ConcurrentDictionary<int, Movie>();
        if (getMovies)
        {
            var po = new ParallelOptions { MaxDegreeOfParallelism = 25 }; //todo env config
            int iLength = movieList.Count();
                Parallel.For(0, iLength ,po, async  i =>
                {
                    MovieDict[movieList[i].movieId] =
                        MovieCache.GetOrAdd(movieList[i].movieId, delegate(int i1)
                        {
                            var movie =  movieService.FetchMovie("film:" + movieList[i].movieId);
                            while(!movie.IsCompleted){ Thread.Sleep(33); }
                            return movie.Result;
                        });
                });
        }
        Console.WriteLine(s.Elapsed);
        var outputStringWriter = new StringWriter();
        jsonSerializer.Serialize(outputStringWriter,new User(username,movieList,MovieDict));
        return outputStringWriter.ToString();
    }
    
}