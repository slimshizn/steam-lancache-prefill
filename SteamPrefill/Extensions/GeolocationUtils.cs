﻿namespace SteamPrefill.Extensions
{
    public static class GeolocationUtils
    {
        public static async Task PrintGeolocationInfoAsync(this EndPoint endpoint)
        {
            var ipAddress = ((IPEndPoint)endpoint).Address;

            using var httpClient = new HttpClient();
            var response = await httpClient.GetStringAsync(new Uri($"http://ip-api.com/json/{ipAddress}"));
            var geolocationInfo = JsonSerializer.Deserialize(response, SerializationContext.Default.GeolocationDetails);

            AnsiConsole.Console.LogMarkupVerbose($"Using CM {LightYellow(ipAddress)} - {geolocationInfo.Country} - {geolocationInfo.City}");
        }
    }

    public sealed class GeolocationDetails
    {
        [JsonPropertyName("city")]
        public string City { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }
    }
}
