namespace Movie_Knight.Services;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;

public class UserService
{
     private readonly HttpClient _httpClient;

    public UserService(HttpClient client)
    {
        _httpClient = client;
    }

    public async Task<IDictionary<int, int>> FetchUser(string username)
    {
        var p = UserCache.TryGetUser(username);
        if (p is not null) return p;
        
        p = await _fetchUser(username);
        UserCache.InsertUser(username,p);
        return p;
    }

    private async Task<IDictionary<int, int>> _fetchUser(string username, int pageNumber=0)
    {
        Console.WriteLine("REAL FETCH!");
        var userUrl = ( username +"/films/");
        if (pageNumber != 0)
        {
            userUrl += "page/" + pageNumber;
        }

        var response = await _httpClient.GetAsync(userUrl);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Letterboxd returned for user " + username+ " invalid code " + response.StatusCode + " : " + response.Content);
        }
        var content = await response.Content.ReadAsStringAsync();
        var movieList = new ConcurrentDictionary<int, int>();
        
        Regex filmsRx = new Regex(@"film-poster-(\d+).+rated-(\d+)");
        var filmsMatch = filmsRx.Matches(content);
        foreach (Match film in filmsMatch)
        {
            movieList[Int32.Parse(film.Groups[1].Value)] =  Int32.Parse(film.Groups[2].Value);
        }
        if(pageNumber==0)
        {
            Regex pageMatch = new Regex(@"films\/page\/(\d+)\/.>\d+<\/a><\/li> <\/ul> <\/div> <\/div>");
            var pageCount = int.Parse(pageMatch.Match(content).Groups[1].Value);
            var po = new ParallelOptions { MaxDegreeOfParallelism = 15 };
            Parallel.For(2, pageCount + 1,po, async i =>
            {
                var movieListTask = _fetchUser(username, i);
                while (!movieListTask.IsCompleted) { Thread.Sleep(100);
                } //for some reason awaiting above doesn't work but this does?????

                foreach (var m in movieListTask.Result)
                {
                    movieList[m.Key] = m.Value;
                }; //(movieListTask.Result);
            });

        }
        return movieList;
    }
}