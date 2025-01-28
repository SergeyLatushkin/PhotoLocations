using System.Text.Json.Serialization;

namespace LocationsFromPhotos.Clients.Models;

public class Content
{
    [JsonPropertyName("results")]
    public required IEnumerable<Result> Results { get; set; }
}

public class Result
{
    [JsonPropertyName("components")]
    public required Component Components { get; set; }
}

public class Component
{
    [JsonPropertyName("country")]
    public required string Country { get; set; }
}