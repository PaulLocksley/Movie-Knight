namespace Movie_Knight.Models;

public class Movie
{
    public readonly IList<(string key, string value)> attributes;
    public readonly string name;
    public readonly int id;
    public readonly int duration;
    public readonly double? averageRating;
    public readonly DateTime releaseDate;
    public readonly string description;

    public Movie(IList<(string key, string value)> attributes, string name, int id, int duration, double? averageRating, DateTime releaseDate, string description)
    {
        this.attributes = attributes;
        this.name = name;
        this.id = id;
        this.duration = duration;
        this.averageRating = averageRating;
        this.releaseDate = releaseDate;
        this.description = description;
    }
}   