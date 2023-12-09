using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Movie_Knight.Models;

public class Filter
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Types
    {
        Remove,
        Require
    }

    [JsonPropertyName("type")] 
    public Types type { get; set; }

    [JsonPropertyName("role")]
    public string role { get; set; }
    [JsonPropertyName("name")]
    public string name { get; set; }

    public Filter(Types type, string role, string name)
    {
        this.type = type;
        this.role = role;
        this.name = name;
    }
}