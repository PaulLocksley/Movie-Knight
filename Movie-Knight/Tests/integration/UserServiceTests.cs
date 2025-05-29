using Movie_Knight.Models;
using Movie_Knight.Services;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Movie_Knight.Tests.integration;

public class UserServiceTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public UserServiceTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task UserFetchTest()
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri("https://letterboxd.com/");
        var userService = new UserService(client);
        //Act
        var userlist= await userService.FetchUser("curtisfyee");
        //Assert
        Assert.True(userlist.Count > 340);
        Assert.Equal(7, userlist[38800]); //but I'm a cheerleader.
    }
    [Fact]
    public async Task UserFetchWatchListTest()
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri("https://letterboxd.com/");
        var userService = new UserService(client);
        //Act
        var userlist= await userService.FetchWatchList("fakerrrrrrr");
        //Assert
        Assert.True(userlist.Count == 1);
        Assert.Equal(50568, userlist[0]);
    }
 
}