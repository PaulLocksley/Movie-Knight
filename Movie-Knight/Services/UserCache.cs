using System.Collections.Concurrent;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Movie_Knight.Services;

public static class UserCache
{
    private static IDictionary<string, IDictionary<int, int>> _userCache = new ConcurrentDictionary<string, IDictionary<int,int>>();
    public static IDictionary<int, int>? TryGetUser(string username)
    {
        if (_userCache.TryGetValue(username, out var userCache))
        {
            return userCache;
        }
        return null;
    }

    public static void InsertUser(string username, IDictionary<int, int> userList)
    {
        _userCache[username] = userList;
        _removeUser(username,TimeSpan.FromHours(1));
    }

    private static async void _removeUser(string username, TimeSpan time)
    {
        await Task.Delay(time);
        _userCache.Remove(username);
    }
}