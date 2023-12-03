using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using Movie_Knight.Controllers;

namespace Movie_Knight.Services;
using Movie_Knight.Models;
using System.Text.RegularExpressions;

public class MovieService
{
    public MovieService(HttpClient client)
    {
        Console.WriteLine("Starting new Movie Service!");
    }
    
    public async Task<Movie> FetchMovie(string url)
    {
        var movieUrl = ("film/" + url);

        var _client = GetHttpClient.GetNamedHttpClient();
        var response = await _client.GetAsync(movieUrl);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Letterboxd returned invalid code for movie " +url+" : "+ response.StatusCode + " : " + response.Content);
        }
        var content = await response.Content.ReadAsStringAsync();


        IList<(string key, string value)> attributes = new List<(string key, string value)>();
        string name;
        int id;
        int duration;
        double? averageRating;
        DateTime? releaseDate;
        string description;

        //var filmData = { id: 251943, name: "Spider-Man: Into the Spider-Verse", gwiId: 301363, releaseYear: "2018", posterURL: "/film/spider-man-into-the-spider-verse/image-150/", path: "/film/spider-man-into-the-spider-verse/", runTime: 117 };
        Regex movieDataRX = new Regex(@"var filmData = {([^\}]+)");
        var movieDataMatch = movieDataRX.Match(content).Groups[1].Value;
        //name
        Regex nameDataRX = new Regex(@"path: .\/film\/([^\/]+)");
        name = nameDataRX.Match(content).Groups[1].Value;
        //id
        Regex idDataRX = new Regex(@"id: (\d+)");
        id = int.Parse(idDataRX.Match(content).Groups[1].Value);
        //duration
        Regex durationDataRX = new Regex("""runTime: (\d+)""");
        duration = int.Parse(durationDataRX.Match(content).Groups[1].Value);
        //release date section
        Regex releaseYearRx = new Regex("""releaseYear: "(\d+)""");
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
        Regex attrsRX = new Regex(@".+ratingCount.+");
        var attrsMatch = attrsRX.Match(content).Value;
        //averageRating
        var ratingRX = new Regex(@"ratingValue.:([\d|\.]+)");
        try
        {
            averageRating = double.Round((double.Parse(ratingRX.Match(attrsMatch).Groups[1].Value) * 2), 1);
        }
        catch (Exception e)
        {
            averageRating = null;
        }

        //attrs - Studio
        Regex studiosRx = new Regex(@"\/studio\/([^\/]+)");
        var studios = studiosRx.Matches(attrsMatch);
        foreach (Match studio in studios)
        {
            attributes.Add((key:"studio",value:studio.Groups[1].Value));
        }
        //attrs - cast
        Regex castRX = new Regex(@"\/actor\/([^\/]+)");
        var castMembers = castRX.Matches(attrsMatch);
        foreach (Match actor in castMembers)
        {
            attributes.Add((key:"cast",value:actor.Groups[1].Value));
        }
        //attrs - genre
        Regex genreRX = new Regex(@"genre.:\[(.*?)\]");
        var genresMatch = genreRX.Match(attrsMatch).Groups[1].Value;
        var genres = genresMatch.Replace("\"", "").Split(",");
        foreach (var genre in genres)
        {   
            attributes.Add((key:"genre",value:genre));
        }
        //attrs - ranking :)
        attributes.Add((key:"rating",value:averageRating.ToString()));
        //attrs - writer
        Regex writersRx = new Regex(@"writer\/([^\/]+)");
        var writers = writersRx.Matches(content);
        foreach (Match writer in writers)
        {
            attributes.Add((key:"writer",value:writer.Groups[1].Value));

        }

        //attrs - Director
        Regex directorsRX = new Regex(@"director/([^/]*)");
        var directors = directorsRX.Matches(attrsMatch);
        foreach (Match match in directors)
        {
            attributes.Add((key:"director",value:match.Groups[1].Value));

        }
        Regex relatedRX = new Regex("""
                                    linked-film-poster.+data-film-id="(\d+)"
                                    """);
        var relateds = relatedRX.Matches(content);
        var relatedFilms = (relateds.Select(x => Int32.Parse(x.Groups[1].Value)).ToArray());
        
        return new Movie(attributes, name, id, duration, averageRating, releaseDate, description,relatedFilms);
    }

}