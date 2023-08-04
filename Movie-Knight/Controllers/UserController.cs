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
    private JsonSerializer jsonSerializer;
    public UserController()
    {
        this.userService = new UserService();
        this.movieService = new MovieService(); //is this bad??
        this.jsonSerializer = Newtonsoft.Json.JsonSerializer.Create(); //for some the default encoding doesn't work here.
    }

    [HttpGet("username")]
    public async Task<string> GetByUserName(string username, bool getMovies=false)
    {
        var movieList = await userService.FetchUser(username);
        var MovieDict = new ConcurrentDictionary<int, Movie>();
        if (getMovies)
        {
            var po = new ParallelOptions { MaxDegreeOfParallelism = 25 }; //todo env config
            Parallel.For(0, movieList.Count, po, async i =>
            {
                var movieTask = movieService.FetchMovie("film:" + movieList[i].movieId);
                while (!movieTask.IsCompleted)
                {
                    Thread.Sleep(33);
                } //for some reason awaiting above doesn't work but this does?????

                MovieDict[movieList[i].movieId] = movieTask.Result;
            });
        }
        var outputStringWriter = new StringWriter();
        jsonSerializer.Serialize(outputStringWriter,new User(username,movieList,MovieDict));
        return outputStringWriter.ToString();
    }
    
}