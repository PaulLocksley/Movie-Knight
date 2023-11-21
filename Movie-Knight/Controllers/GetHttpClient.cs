
namespace Movie_Knight.Controllers;

public class GetHttpClient
{
    
        public static HttpClient GetNamedHttpClient()
        {
            var c = new HttpClient();
            c.BaseAddress = new Uri("https://letterboxd.com/");
            c.Timeout = TimeSpan.FromSeconds(90);
            return c;
        }//todo: work out how to use the registered factory.
}