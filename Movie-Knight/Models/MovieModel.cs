namespace Movie_Knight.Models;

public struct Movie
{
    public IList<(string key, string value)> attributes;
    public string name;
    public int id;
    public int duration;
    public double? averageRating;
    public DateTime releaseDate;
    public string description;

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