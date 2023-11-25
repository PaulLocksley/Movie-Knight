using Movie_Knight.Controllers;
using Movie_Knight.Services;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Movie_Knight.Models;

public struct User
{
    public IDictionary<int,int> userList;
    public string username;
    public IDictionary<int, Movie>? Movies;

    public User(string username,IDictionary<int,int> userList,  IDictionary<int, Movie>? movies)
    {
        this.userList = userList;
        this.username = username;
        Movies = movies;
    }
    
}