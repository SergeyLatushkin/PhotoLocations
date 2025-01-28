using LocationsFromPhotos.Clients.Models;
using System.Net;
using System.Text.Json;

namespace LocationsFromPhotos.Clients;

public class NominatimClient
{
    public async Task<string> GetCountry(double? latitude, double? longitude)
    {
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

        var requestUrl = $"https://api.opencagedata.com/geocode/v1/json?q={latitude}+{longitude}&key=37a206344dbf4b5696fbfdae0bc0bbae";
        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
        HttpResponseMessage response = await client.GetAsync(requestUrl);
        Stream content = await response.Content.ReadAsStreamAsync();
          
        var deserializedContent = await JsonSerializer.DeserializeAsync<Content>(content);

        return deserializedContent?.Results.FirstOrDefault()?.Components.Country ?? "undefined";
    }
}