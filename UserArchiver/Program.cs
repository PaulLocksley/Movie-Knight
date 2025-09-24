// See https://aka.ms/new-console-template for more information
using Movie_Knight.Services;
using Microsoft.Data.Sqlite;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dapper;

// User service setup
var c = new HttpClient();
c.BaseAddress = new Uri("https://letterboxd.com/");
c.Timeout = TimeSpan.FromSeconds(90);
var userService = new UserService(c);

//db setup local sqlite.
//create if doesn't exists.
var connectionString = "Data Source=users.db";
using var connection = new SqliteConnection(connectionString);
connection.Open();

await connection.ExecuteAsync(@"
    CREATE TABLE IF NOT EXISTS Users (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        Username TEXT UNIQUE NOT NULL,
        Data TEXT NOT NULL,
        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
    )");

var userPattern = new Regex(@"href=""\/([^\/]+)\/films");
var processedUsers = 0;
for (int i = 2; i < 100; i++)
{
    var userPage = await c.GetAsync($"members/popular/this/week/page/{i}");
    if (!userPage.IsSuccessStatusCode)
    {
        Console.WriteLine($"page {i} failed with code {userPage.StatusCode},");
        continue;
    }

    var userPageContent = await userPage.Content.ReadAsStringAsync();
    var userNames = userPattern.Matches(userPageContent).Select(x => x.Groups[1].Value).ToHashSet().ToList();

    var totalUsers = userNames.Count * 98;

    foreach (var username in userNames)
    {
        processedUsers++;

        //check if user results in db. Conitnue if found.
        var userExists = await connection.QuerySingleAsync<int>(
            "SELECT COUNT(*) FROM Users WHERE Username = @username",
            new { username });

        if (userExists > 0)
        {
            Console.WriteLine($"User {username} already exists in database, skipping.");
            continue;
        }

        //try the rest with logging failures
        try
        {
            var results = await userService.FetchUser(username);
            //write results to db.
            await connection.ExecuteAsync(@"
            INSERT INTO Users (Username, Data) 
            VALUES (@username, @data)",
                new { username, data = JsonSerializer.Serialize(results) });

            Console.WriteLine($"Successfully archived user: {username}");
            await Task.Delay(2000);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to fetch user {username}: {ex.Message}");
        }

        // Progress marker every 10 users
        if (processedUsers % 10 == 0)
        {
            var percentage = (processedUsers * 100) / totalUsers;
            Console.WriteLine($"Progress: {processedUsers}/{totalUsers} ({percentage}%)");
        }
    }

}