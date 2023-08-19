using System.Collections.Concurrent;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing.Matching;
using Movie_Knight.Models;
using Movie_Knight.Services;

namespace Movie_Knight.Controllers;

[ApiController]
[Route("[controller]")]
public class MovieController : ControllerBase
{
        private MovieService movieService;
        private IDictionary<int, Movie> MovieCache;
        public MovieController()
        {
                this.MovieCache = new ConcurrentDictionary<int, Movie>();
                this.movieService = new MovieService();
                
        }

        [HttpPost("movieList")]
        public IList<Movie> PostMovieList(int[] movieIds)
        {
                var movieList = new List<Movie>();
                var po = new ParallelOptions { MaxDegreeOfParallelism = 25 }; //todo env config
                Parallel.For(0, movieIds.Length, po,  i =>
                {
                        if (MovieCache.ContainsKey(movieIds[i]))
                        {
                                movieList.Add(MovieCache[movieIds[i]]);
                        }
                        else
                        {
                                var movie =  movieService.FetchMovie("film:" + movieIds[i]);
                                while(!movie.IsCompleted){ Thread.Sleep(33); }
                                movieList.Add(movie.Result);
                                MovieCache[movieIds[i]] = movie.Result;
                        }
                });
                        
                return movieList;
        }

}