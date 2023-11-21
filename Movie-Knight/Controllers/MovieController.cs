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

        private readonly IHttpClientFactory _httpClientFactory;
        public MovieController(IHttpClientFactory httpClientFactory){

                _httpClientFactory = httpClientFactory;
                
        }

        [HttpPost("movieList")]
        public IList<Movie> PostMovieList(int[] movieIds)
        {
                return movieIds.AsParallel().WithDegreeOfParallelism(12).Select(MovieCache.GetMovie).ToList();
        }

}