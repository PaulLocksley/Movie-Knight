namespace Movie_Knight.Services;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;

public class UserService
{
     private HttpClient client;

    public UserService()
    {
        this.client = new HttpClient();
    }

    public async  Task<IList<(int movieId, int rating)>> FetchUser(string username, int pageNumber=0)
    {
        var userUrl = ("https://letterboxd.com/" + username +"/films/");
        if (pageNumber != 0)
        {
            userUrl += "page/" + pageNumber;
        } 

        var response = await client.GetAsync(userUrl);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Letterboxd returned for user " + username+ " invalid code " + response.StatusCode + " : " + response.Content);
        }
        var content = await response.Content.ReadAsStringAsync();
        var movieList = new List<(int movieID, int rating)>();
        
        Regex filmsRx = new Regex(@"film-poster-(\d+).+rated-(\d+)");
        var filmsMatch = filmsRx.Matches(content);
        foreach (Match film in filmsMatch)
        {
            movieList.Add((movieID: Int32.Parse(film.Groups[1].Value), rating: Int32.Parse(film.Groups[2].Value)));
        }
        if(pageNumber==0)
        {
            Regex pageMatch = new Regex(@"films\/page\/(\d+)\/.>\d+<\/a><\/li> <\/ul> <\/div> <\/div>");
            var pageCount = int.Parse(pageMatch.Match(content).Groups[1].Value);
            ConcurrentBag<IList<(int movieID, int rating)>> movieBag = new ConcurrentBag<IList<(int movieID, int rating)>> ();
            var po = new ParallelOptions { MaxDegreeOfParallelism = 15 };
            Parallel.For(2, pageCount + 1,po, async i =>
            {
                var movieListTask = FetchUser(username, i);
                while (!movieListTask.IsCompleted) { Thread.Sleep(100);
                } //for some reason awaiting above doesn't work but this does?????
                movieBag.Add(movieListTask.Result);
            });
            
            foreach (var tmpMovieList in movieBag)
            {
                movieList.AddRange(tmpMovieList);
            }

        }
        return movieList;
    }
}