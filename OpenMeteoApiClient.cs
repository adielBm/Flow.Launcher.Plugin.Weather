using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.Weather
{
    public class OpenMeteoApiClient
    {
        private static readonly HttpClient client = new HttpClient();

        public OpenMeteoApiClient()
        {
            client.DefaultRequestHeaders.Add("User-Agent", "Flow.Launcher.Plugin.Weather");
        }

        public async Task<WeatherForecast> GetForecastAsync(double latitude, double longitude)
        {
            string url = $"{BaseUrl}?latitude={latitude}&longitude={longitude}&current=temperature_2m,apparent_temperature,is_day,weather_code&daily=apparent_temperature_max,apparent_temperature_min&forecast_days=1";
            var response = await client.GetAsync(url);

            response.EnsureSuccessStatusCode();  // Throws if status code is not 2xx

            var responseString = await response.Content.ReadAsStringAsync();

            var json = JsonDocument.Parse(responseString);
            var root = json.RootElement;

            // Check if there's any data
            if (root.TryGetProperty("current", out JsonElement currentWeather) &&
                root.TryGetProperty("daily", out JsonElement dailyWeather))
            {
                return new WeatherForecast
                {
                    Latitude = latitude,
                    Longitude = longitude,
                    Current = new CurrentWeather
                    {
                        Time = currentWeather.GetProperty("time").GetDateTime(),
                        Temperature2m = Math.Round(currentWeather.GetProperty("temperature_2m").GetDouble()),
                        ApparentTemperature = Math.Round(currentWeather.GetProperty("apparent_temperature").GetDouble()),
                        IsDay = currentWeather.GetProperty("is_day").GetInt32(),
                        WeatherCode = currentWeather.GetProperty("weather_code").GetInt32()
                    },
                    Daily = new DailyWeather
                    {
                        Time = dailyWeather.GetProperty("time").EnumerateArray().Select(d => d.GetDateTime()).ToArray(),
                        ApparentTemperatureMax = dailyWeather.GetProperty("apparent_temperature_max").EnumerateArray().Select(d => Math.Round(d.GetDouble())).ToArray(),
                        ApparentTemperatureMin = dailyWeather.GetProperty("apparent_temperature_min").EnumerateArray().Select(d => Math.Round(d.GetDouble())).ToArray()
                    }
                };
            }

            return null; // No weather data found
        }

        private const string BaseUrl = "https://api.open-meteo.com/v1/forecast";
    }

    public class WeatherForecast
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public CurrentWeather Current { get; set; }
        public DailyWeather Daily { get; set; }
    }

    public class CurrentWeather
    {
        public DateTime Time { get; set; }
        public double Temperature2m { get; set; }
        public double ApparentTemperature { get; set; }
        public int IsDay { get; set; }
        public int WeatherCode { get; set; }
    }

    public class DailyWeather
    {
        public DateTime[] Time { get; set; }
        public double[] ApparentTemperatureMax { get; set; }
        public double[] ApparentTemperatureMin { get; set; }
    }
}
