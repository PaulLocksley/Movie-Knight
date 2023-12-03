using System.Text.Json;
    
namespace Movie_Knight.Models;

[Serializable]
public class Movie
{
    public  IList<(string key, string value)> attributes;
    public  string name;
    public  int id;
    public  int duration;
    public  double? averageRating;
    public  DateTime releaseDate;
    public  string description;
    public int[] relatedFilms;

    public Movie(IList<(string key, string value)> attributes, string name, int id, int duration, double? averageRating,
        DateTime releaseDate, string description, int[] relatedFilms)
    {
        if (id == 0)
        {
            throw new InvalidDataException();
        }
        this.attributes = attributes;
        this.name = name;
        this.id = id;
        this.duration = duration;
        this.averageRating = averageRating;
        this.releaseDate = releaseDate;
        this.description = description;
        this.relatedFilms = relatedFilms;
    }
}   