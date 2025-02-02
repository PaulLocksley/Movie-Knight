using System.Collections.Concurrent;
using Movie_Knight.Models;
using Newtonsoft.Json;

namespace Movie_Knight.Services;

public static class MovieCache
{
    //todo: swap all usages of this to be done via service.
    private static readonly IDictionary<int, Movie> Cache;
    private static readonly MovieService MovieService;
    private static DateTime _updateTime;
    private static string _filePath;
    private static JsonSerializer _jt;
    static MovieCache()
    {
        _jt = new JsonSerializer();
        _filePath = $"{Environment.CurrentDirectory}MovieCache.json";
        MovieService = new MovieService();
        Cache =  new ConcurrentDictionary<int, Movie>();
        try
        {
            string json = File.ReadAllText(_filePath);

            
            IDictionary<int, Movie>? loadedCache =
                _jt.Deserialize<IDictionary<int, Movie>>(new JsonTextReader(new StringReader(json)));
            if (loadedCache is null)
            {
                return;
            }
            Console.WriteLine("Cache loaded from file successfully.");
            foreach (var movie in loadedCache)
            {
                Cache[movie.Key] = movie.Value;
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading cache from file: {ex.Message}");
        }
    }
    
    public static Movie GetMovie(int id)
    {
        if(!Cache.TryGetValue(id,out var returnMovie))
        {
            var movie = MovieService.FetchMovie("film:" + id, id);
            while(!movie.IsCompleted){ Thread.Sleep(10); }

            if (movie.IsFaulted)
            {
                throw new Exception($"Failed to get movie with id {id} \n {movie.Exception} \n {movie.Exception.Message}");
            }
            Cache[id] =  movie.Result;
            returnMovie =  movie.Result;
            UpdateFileCache();
        }
        return returnMovie;
    }

    private static async void UpdateFileCache()
    {
        var dt = DateTime.Now + TimeSpan.FromSeconds(30);
        _updateTime = dt;
        await Task.Delay(TimeSpan.FromSeconds(30));
        if (_updateTime != dt)
        {
            return;
        }
        try
        {
            var outputStringWriter = new StringWriter();
            _jt.Serialize(outputStringWriter, Cache);
            await File.WriteAllTextAsync(_filePath, outputStringWriter.ToString());
            Console.WriteLine($"Cache saved to file successfully, now at {Cache.Count} items");
        } catch (Exception ex)
        {
            Console.WriteLine($"Error saving cache to file: {ex.Message}");
        }
    }
}
