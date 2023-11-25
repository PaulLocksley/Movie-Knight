using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Movie_Knight.Controllers;
using Movie_Knight.Models;
using Newtonsoft.Json;

namespace Movie_Knight.Services;

public static class MovieCache
{
    private static readonly IDictionary<int, Movie> _cache;
    private static readonly MovieService _movieService;
    private static DateTime _updateTime;
    private static string _filePath;
    private static JsonSerializer jt;
    static MovieCache()
    {
        jt = new JsonSerializer();
        _filePath = $"{Environment.CurrentDirectory}MovieCache.420";
        _movieService = new MovieService(GetHttpClient.GetNamedHttpClient());
        _cache =  new ConcurrentDictionary<int, Movie>();
        try
        {
            string json = File.ReadAllText(_filePath);

            
            IDictionary<int, Movie>? loadedCache =
                jt.Deserialize<IDictionary<int, Movie>>(new JsonTextReader(new StringReader(json)));
            Console.WriteLine("Cache loaded from file successfully.");
            foreach (var movie in loadedCache)
            {
                _cache[movie.Key] = movie.Value;
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading cache from file: {ex.Message}");
        }
    }
    
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

            if (movie.IsFaulted)
            {
                Console.WriteLine(movie.Exception);
                throw;
            }
            _cache[id] =  movie.Result;
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
            jt.Serialize(outputStringWriter, _cache);
            await File.WriteAllTextAsync(_filePath, outputStringWriter.ToString());
            Console.WriteLine("Cache saved to file successfully.");
        } catch (Exception ex)
        {
            Console.WriteLine($"Error saving cache to file: {ex.Message}");
        }
    }
}
