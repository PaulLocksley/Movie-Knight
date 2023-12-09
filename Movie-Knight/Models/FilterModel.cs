namespace Movie_Knight.Models;

public class Filter
{
    public enum Types
    {
        Remove,
        Require
    }

    public Types type;
    public string role;
    public string name;

    public Filter(Types type, string role, string name)
    {
        this.type = type;
        this.role = role;
        this.name = name;
    }
}