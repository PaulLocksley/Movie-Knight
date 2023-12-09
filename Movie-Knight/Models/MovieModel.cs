using System.Text.Json;
    
namespace Movie_Knight.Models;

[Serializable]
public class Movie
{
    public  IList<(string role, string name)> attributes { get; set; }
    public  string name { get; set; }
    public  int id { get; set; }
    public  int duration { get; set; }
    public  double? averageRating { get; set; }
    public  DateTime? releaseDate { get; set; }
    public  string description { get; set; }
    public int[] relatedFilms { get; set; }

    public Movie(IList<(string role, string name)> attributes, string name, int id, int duration, double? averageRating,
        DateTime? releaseDate, string description, int[] relatedFilms)
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