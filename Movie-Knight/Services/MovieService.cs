using System.Diagnostics;
using System.Net;
using Movie_Knight.Controllers;

namespace Movie_Knight.Services;
using Models;
using System.Text.RegularExpressions;

public class MovieService
{
    public MovieService()
    {
        Console.WriteLine("Starting new Movie Service!");
    }
    
    public async Task<Movie> FetchMovie(string url, int id, int attempts = 0)
    {
        var movieUrl = ("film/" + url);
        var httpClient = GetHttpClient.GetNamedHttpClient();
        var response = await httpClient.GetAsync(movieUrl);
        if (!response.IsSuccessStatusCode)
        {
            var newException = response.StatusCode switch
            {
                HttpStatusCode.TooManyRequests => new Exception("Too Many Requests"),
                HttpStatusCode.NotFound =>  new FileNotFoundException(
                    $"Letterboxd could not locate movie {url} : {response.StatusCode}"),
                _ => new Exception($"Unknown Error: {url} : {response.StatusCode}"),
            };

            if (response.StatusCode != HttpStatusCode.TooManyRequests || attempts >= 10) throw newException;

            await Task.Delay(10_000);
            return await FetchMovie(url, id, attempts + 1 );
        }
        var content = await response.Content.ReadAsStringAsync();


        IList<(string role, string name)> attributes = new List<(string role, string name)>();
        string name;
        int duration = 0;
        double? averageRating;
        int ratingCount;
        DateTime? releaseDate;
        string description;

        //var filmData = { id: 251943, name: "Spider-Man: Into the Spider-Verse", gwiId: 301363, releaseYear: "2018", posterURL: "/film/spider-man-into-the-spider-verse/image-150/", path: "/film/spider-man-into-the-spider-verse/", runTime: 117 };
        Regex nameDataRx = new Regex(@"\/film\/([^\/]+)\/rating-histogram");
        name = nameDataRx.Match(content).Groups[1].Value;
        //id
        //duration
        Regex durationDataRx = new Regex("""(\d+)&nbsp;mins""");
        int.TryParse(durationDataRx.Match(content).Groups[1].Value, out duration);
        //release date section
        Regex releaseYearRx = new Regex(@"\/films\/year\/(\d+)");
        var releaseYear = releaseYearRx.Match(content).Groups[1].Value;
        if (int.TryParse(releaseYear, out var releaseYearInt) && releaseYearInt > 1880 && releaseYearInt < 2200)
        {
            releaseDate = new DateTime(releaseYearInt, 1, 1, 12, 0, 0);
        }
        else
        {
            Debug.WriteLine($"Failed to parse date for movie {movieUrl}");
            releaseDate = null;
        }

        //description
        var descriptionRx = new Regex(@"<div class=.review body-text -prose -hero prettify.>.|\n.+<p>(.+)</p>");
        description = descriptionRx.Match(content).Groups[1].Value;
        //attrs:
        Regex attrsRx = new Regex(@".+ratingCount.+");
        var attrsMatch = attrsRx.Match(content).Value;
        //averageRating
        var ratingRx = new Regex(@"ratingValue.:([\d|\.]+)");
        try
        {
            averageRating = double.Round((double.Parse(ratingRx.Match(attrsMatch).Groups[1].Value) * 2), 1);
        }
        catch (Exception e)
        {
            Debug.WriteLine($"Failed to parse average raiting, {e}");
            averageRating = null;
        }

        Regex ratingsCountRx = new Regex(@"ratingCount.:(\d+),");
        try
        {
            ratingCount = Int32.Parse(ratingsCountRx.Match(attrsMatch).Groups[1].ToString());
        }
        catch (Exception)
        {
            try
            {
                Debug.WriteLine("Failed to find movie rating count attempting backup");
                Regex backupRatingsCountsRx = new Regex("""title="(\d+)""");
                var ratingHistogram = await httpClient.GetAsync($"csi/film/{url}/rating-histogram/");
                if (!ratingHistogram.IsSuccessStatusCode)
                {
                    throw new Exception(ratingHistogram.ToString());
                }
                var ratingContent = await ratingHistogram.Content.ReadAsStringAsync();
                ratingCount = backupRatingsCountsRx.Matches(ratingContent).Select(x => 
                    Int32.Parse(x.Groups[1].ToString()))
                    .Aggregate((x,y) => (x + y));
                Debug.WriteLine($"Found rating number: {ratingCount}");
            }
            catch (Exception e)
            {
                Debug.WriteLine("Something really went wrong when attempting backup parsing");
                Debug.WriteLine(e);
                ratingCount = 0;
            }

        }
        //attrs - Studio
        Regex studiosRx = new Regex(@"\/studio\/([^\/]+)");
        var studios = studiosRx.Matches(attrsMatch);
        foreach (Match studio in studios)
        {
            attributes.Add((role:"studio",name:studio.Groups[1].Value));
        }
        //attrs - cast
        Regex castRx = new Regex(@"\/actor\/([^\/]+)");
        var castMembers = castRx.Matches(attrsMatch);
        foreach (Match actor in castMembers)
        {
            attributes.Add((role:"cast",name:actor.Groups[1].Value));
        }
        //attrs - genre
        Regex genreRx = new Regex(@"genre.:\[(.*?)\]");
        var genresMatch = genreRx.Match(attrsMatch).Groups[1].Value;
        var genres = genresMatch.Replace("\"", "").Split(",");
        foreach (var genre in genres)
        {   
            attributes.Add((role:"genre",name:genre));
        }
        //attrs - ranking :)
        attributes.Add((role:"rating",name:averageRating?.ToString() ?? "Null"));
        //attrs - writer
        Regex writersRx = new Regex(@"writer\/([^\/]+)");
        var writers = writersRx.Matches(content);
        foreach (Match writer in writers)
        {
            attributes.Add((role:"writer",name:writer.Groups[1].Value));

        }

        //attrs - Director
        Regex directorsRx = new Regex(@"director/([^/]*)");
        var directors = directorsRx.Matches(attrsMatch);
        foreach (Match match in directors)
        {
            attributes.Add((role:"director",name:match.Groups[1].Value));

        }
        Regex relatedRx = new Regex("""
                                    linked-film-poster.+data-film-id="(\d+)"
                                    """);
        var relateds = relatedRx.Matches(content);
        var relatedFilms = (relateds.Select(x => Int32.Parse(x.Groups[1].Value)).ToArray());
        
        return new Movie(attributes, name, id, duration, averageRating, ratingCount, releaseDate, description,relatedFilms);
    }

}