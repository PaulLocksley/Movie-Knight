using System.Net;

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
        if (p is not null) 
        {
            Console.WriteLine($"[UserService] Cached user '{username}': {p.Count} movies");
            return p;
        }
        
        p = await _fetchUser(username);
        Console.WriteLine($"[UserService] Fetched user '{username}': {p.Count} movies");
        
        UserCache.InsertUser(username,p);
        return p;
    }

    public async Task<IList<int>> FetchWatchList(string username, int pageNumber=1)
    {
        var tmp = UserCache.TryGetUserWatchListCache(username);
        if(tmp is not null) return tmp;
        
        var movieList = new ConcurrentBag<int>();
        var userUrl = $"{username}/watchlist/page/{pageNumber}";
        var response = await _httpClient.GetAsync(userUrl);
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new UnauthorizedAccessException();
        }
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to fetch watch list for user {username}");
        }
        var content = await response.Content.ReadAsStringAsync();
        Regex filmsRx = new Regex(@"data-film-id=""(\d+)");
        var filmsMatch = filmsRx.Matches(content);
        
        foreach (Match filmMatch in filmsMatch)
        {
            movieList.Add(int.Parse(filmMatch.Groups[1].Value));
        }
        
        if(pageNumber==0)
        {
            Regex pageMatch = new Regex(@"watchlist\/page\/(\d+)\/.>\d+<\/a><\/li> <\/ul> <\/div> <\/div>");
            var foundPage = int.TryParse(pageMatch.Match(content).Groups[1].Value,out var pageCount);
            if (foundPage)
            {
                var po = new ParallelOptions { MaxDegreeOfParallelism = 15 };
                await Parallel.ForEachAsync(
                    Enumerable.Range(2, pageCount - 1),
                    po,
                    async (i, ct) =>
                    {
                        var movies = await FetchWatchList(username, i);
                        foreach (var movie in movies)
                        {
                            movieList.Add(movie);
                        }
                    });
            }
        }
        UserCache.InsertWatchListUser(username,movieList.ToList());
        return movieList.ToList();
    }
    
    
    private async Task<IDictionary<int, int>> _fetchUser(string username, int pageNumber=0)
    {
        var userUrl = ( username +"/films/");
        if (pageNumber != 0)
        {
            userUrl += "page/" + pageNumber;
        }

        var response = await _httpClient.GetAsync(userUrl);
        
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"[UserService] ERROR: Failed to fetch user '{username}' - Status: {response.StatusCode}");
            throw new FileNotFoundException("Letterboxd returned for user " + username+ " invalid code " + response.StatusCode + " : " + response.Content);
        }
        var content = await response.Content.ReadAsStringAsync();
        var movieList = new ConcurrentDictionary<int, int>();
        
        Regex filmsRx = new Regex(@"film:(\d+).*rated-(\d+)");
        var filmsMatch = filmsRx.Matches(content);
        
        foreach (Match film in filmsMatch)
        {
            movieList[Int32.Parse(film.Groups[1].Value)] =  Int32.Parse(film.Groups[2].Value);
        }
        
        if(pageNumber==0)
        {
            Regex pageMatch = new Regex(@"films\/page\/(\d+)\/.>\d+<\/a><\/li> <\/ul> <\/div> <\/div>");
            var foundPage = int.TryParse(pageMatch.Match(content).Groups[1].Value,out var pageCount);
            
            if (foundPage)
            {
                var po = new ParallelOptions { MaxDegreeOfParallelism = 15 };
                await Parallel.ForEachAsync(
                    Enumerable.Range(2, pageCount - 1),
                    po,
                    async (i, ct) =>
                    {
                        var movies = await _fetchUser(username, i);
                        foreach (var m in movies)
                        {
                            movieList[m.Key] = m.Value;
                        }
                    });
            }
        }
        
        return movieList;
    }
}