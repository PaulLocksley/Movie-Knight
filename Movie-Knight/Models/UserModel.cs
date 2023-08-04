using Movie_Knight.Controllers;
using Movie_Knight.Services;

namespace Movie_Knight.Models;

public struct User
{
    public IList<(int movieId, int rating)> userList;
    public string username;
    public IDictionary<int, Movie> Movies;

    public User(string username,IList<(int movieId, int rating)> userList,  IDictionary<int, Movie> movies)
    {
        this.userList = userList;
        this.username = username;
        Movies = movies;
    }
    
}