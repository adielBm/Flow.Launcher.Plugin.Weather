using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Flow.Launcher.Plugin;

namespace Flow.Launcher.Plugin.Weather
{
    public class Main : IPlugin
    {
        private string ClassName => GetType().Name;

        private PluginInitContext _context;
        private readonly HttpClient _httpClient;
        private const string API_URL = "https://api.open-meteo.com/v1/forecast?latitude=31.769&longitude=35.2163&daily=weather_code,apparent_temperature_max,apparent_temperature_min&wind_speed_unit=ms&timezone=Africa%2FCairo&forecast_days=1";
        public Main()
        {
            _httpClient = new HttpClient();
        }

        public void Init(PluginInitContext context)
        {
            _context = context;
        }


        private async Task<WeatherData> GetWeatherAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(API_URL);
                response.EnsureSuccessStatusCode();
                var jsonString = await response.Content.ReadAsStringAsync();
                _context.API.LogInfo(ClassName, "[[jsonString]]" + jsonString);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                WeatherData weatherData = JsonSerializer.Deserialize<WeatherData>(jsonString, options) ?? throw new Exception("Failed to deserialize WeatherInfo");
                return weatherData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetWeatherAsync: {ex}");
                throw;
            }
        }

        public List<Result> Query(Query query)
        {
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                // return empty
                return new List<Result>();
            }

            try
            {
                WeatherData weatherData = GetWeatherAsync().Result ?? throw new Exception("weatherData is null");

                return new List<Result>
                {
                    new Result
                    {
                        Title = $"Weather in Jerusalem",
                        SubTitle = $"Max: {weatherData.Daily.ApparentTemperatureMax[0]}°C, Min: {weatherData.Daily.ApparentTemperatureMin[0]}°C",
                        IcoPath = "Images\\weather-icon.png"
                    }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in Query: {ex}");
                return new List<Result>
                {
                    new Result
                    {
                        Title = "Error fetching weather",
                        SubTitle = $"Details: {ex.Message}",
                        IcoPath = "Images\\weather-icon.png"
                    }
                };
            }
        }

        public class WeatherData
        {
            [JsonPropertyName("latitude")]
            public double Latitude { get; set; }

            [JsonPropertyName("longitude")]
            public double Longitude { get; set; }

            [JsonPropertyName("generationtime_ms")]
            public double GenerationTimeMs { get; set; }

            [JsonPropertyName("utc_offset_seconds")]
            public int UtcOffsetSeconds { get; set; }

            [JsonPropertyName("timezone")]
            public string Timezone { get; set; }

            [JsonPropertyName("timezone_abbreviation")]
            public string TimezoneAbbreviation { get; set; }

            [JsonPropertyName("elevation")]
            public double Elevation { get; set; }

            [JsonPropertyName("daily_units")]
            public DailyUnits DailyUnits { get; set; }

            [JsonPropertyName("daily")]
            public Daily Daily { get; set; }
        }

        public class DailyUnits
        {
            [JsonPropertyName("time")]
            public string Time { get; set; }

            [JsonPropertyName("weather_code")]
            public string WeatherCode { get; set; }

            [JsonPropertyName("apparent_temperature_max")]
            public string ApparentTemperatureMax { get; set; }

            [JsonPropertyName("apparent_temperature_min")]
            public string ApparentTemperatureMin { get; set; }
        }

        public class Daily
        {
            [JsonPropertyName("time")]
            public List<string> Time { get; set; }

            [JsonPropertyName("weather_code")]
            public List<int> WeatherCode { get; set; }

            [JsonPropertyName("apparent_temperature_max")]
            public List<double> ApparentTemperatureMax { get; set; }

            [JsonPropertyName("apparent_temperature_min")]
            public List<double> ApparentTemperatureMin { get; set; }
        }



        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}