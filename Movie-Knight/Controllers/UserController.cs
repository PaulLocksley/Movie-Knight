using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Movie_Knight.Models;
using Movie_Knight.Services;
//using System.Text.Json;
using Newtonsoft.Json;
namespace Movie_Knight.Controllers;

[ApiController]
[Route("[controller]")]
//[ResponseCache(Duration = 7200)]
public class UserController
{
    private UserService userService;
    private MovieService movieService;
    private JsonSerializer jsonSerializer;
    private HttpClient? sharedHttpCleint;
    public UserController()
    {
        this.sharedHttpCleint = new HttpClient();
        this.userService = new UserService(sharedHttpCleint);
        this.movieService = new MovieService(sharedHttpCleint);
        this.jsonSerializer = Newtonsoft.Json.JsonSerializer.Create(); //for some the default encoding doesn't work here.
    }

    [HttpGet("username")]
    public async Task<string> GetByUserName(string username, bool getMovies=false)
    {
        //Console.WriteLine($"MC Count: {MovieCache22.Count}");
        var movieList = await userService.FetchUser(username);
        var s = Stopwatch.StartNew();

        var MovieDict = new ConcurrentDictionary<int, Movie>();
        if (getMovies)
        {
            var po = new ParallelOptions { MaxDegreeOfParallelism = 25 }; //todo env config
            int iLength = movieList.Count();
                Parallel.For(0, iLength ,po, async  i =>
                {
                    MovieDict[movieList[i].movieId] = MovieCache.GetMovie(movieList[i].movieId);
                });
        }
        Console.WriteLine(s.Elapsed);
        var outputStringWriter = new StringWriter();
        jsonSerializer.Serialize(outputStringWriter,new User(username,movieList,MovieDict));
        return outputStringWriter.ToString();
    }
    
}