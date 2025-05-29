using Movie_Knight.Models;
using Movie_Knight.Services;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Movie_Knight.Tests.integration;

public class MovieServiceTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public MovieServiceTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task MovieServiceParsesMovie()
    {
        var movieService = new MovieService();
        var movieJson = """
                        {"attributes":[{"Item1":"studio","Item2":"regency-enterprises"},{"Item1":"studio","Item2":"indian-paintbrush"},{"Item1":"studio","Item2":"american-empirical-pictures"},{"Item1":"cast","Item2":"george-clooney"},{"Item1":"cast","Item2":"meryl-streep"},{"Item1":"cast","Item2":"jason-schwartzman"},{"Item1":"cast","Item2":"bill-murray"},{"Item1":"cast","Item2":"willem-dafoe"},{"Item1":"cast","Item2":"owen-wilson"},{"Item1":"cast","Item2":"wallace-wolodarsky"},{"Item1":"cast","Item2":"eric-chase-anderson"},{"Item1":"cast","Item2":"michael-gambon"},{"Item1":"cast","Item2":"jarvis-cocker"},{"Item1":"cast","Item2":"wes-anderson"},{"Item1":"cast","Item2":"robin-hurlstone"},{"Item1":"cast","Item2":"hugo-guinness"},{"Item1":"cast","Item2":"helen-mccrory"},{"Item1":"cast","Item2":"juman-malouf"},{"Item1":"cast","Item2":"karen-duffy"},{"Item1":"cast","Item2":"roman-coppola"},{"Item1":"cast","Item2":"jeremy-dawson"},{"Item1":"cast","Item2":"garth-jennings"},{"Item1":"cast","Item2":"brian-cox-2"},{"Item1":"cast","Item2":"tristan-oliver"},{"Item1":"cast","Item2":"james-hamilton"},{"Item1":"cast","Item2":"steven-m-rales"},{"Item1":"cast","Item2":"rob-hersov"},{"Item1":"cast","Item2":"jennifer-furches"},{"Item1":"cast","Item2":"allison-abbate-1"},{"Item1":"cast","Item2":"molly-cooper"},{"Item1":"cast","Item2":"adrien-brody"},{"Item1":"cast","Item2":"mario-batali"},{"Item1":"cast","Item2":"martin-ballard"},{"Item1":"genre","Item2":"Family"},{"Item1":"genre","Item2":"Adventure"},{"Item1":"genre","Item2":"Comedy"},{"Item1":"genre","Item2":"Animation"},{"Item1":"rating","Item2":"8.5"},{"Item1":"writer","Item2":"wes-anderson"},{"Item1":"writer","Item2":"noah-baumbach"},{"Item1":"writer","Item2":"roald-dahl"},{"Item1":"director","Item2":"wes-anderson"}],"name":"fantastic-mr-fox","id":46344,"duration":87,"averageRating":8.5,"RatingCount":1016732,"releaseDate":"2009-01-01T12:00:00","description":"The Fantastic Mr. Fox, bored with his current life, plans a heist against the three local farmers. The farmers, tired of sharing their chickens with the sly fox, seek revenge against him and his family.","relatedFilms":[316801,47994,48033,186518,16093,51670]}
                        """;
        var jt = new JsonSerializer();
        
        var movie = jt.Deserialize<Movie>(new JsonTextReader(new StringReader(movieJson)))!;
        var ogattributes = movie.attributes.Where(x => x.role != "rating").ToDictionary(x => $"{x.role}{x.name}", x => x.name);
        //Act
        var parsedMovie = await movieService.FetchMovie("film:46344",46344);
        var parsedAttributes = parsedMovie.attributes.Where(x => x.role != "rating").ToDictionary(x => $"{x.role}{x.name}", x => x.name);
        //Assert
        Assert.Equal(movie.id, parsedMovie.id);
        Assert.Equal(ogattributes, parsedAttributes);
        Assert.Equal(movie.name, parsedMovie.name);
        Assert.Equal(movie.duration, parsedMovie.duration);
        Assert.True(float.Parse(parsedMovie.attributes.First(x => x.role == "rating").name) > 8.0);
    }    
}