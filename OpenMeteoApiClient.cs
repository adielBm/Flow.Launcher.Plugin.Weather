using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.Weather
{
    public class OpenMeteoApiClient
    {
        private static readonly HttpClient client = new();
        private const string BaseUrl = "https://api.open-meteo.com/v1/forecast";
        private readonly PluginInitContext context;

        public OpenMeteoApiClient(PluginInitContext context)
        {
            this.context = context;
            client.DefaultRequestHeaders.Add("User-Agent", "Flow.Launcher.Plugin.Weather");
        }

        public async Task<WeatherForecast> GetForecastAsync(string latitude, string longitude)
        {
            string url = $"{BaseUrl}?latitude={latitude}&longitude={longitude}&current=temperature_2m,apparent_temperature,is_day,weather_code&daily=apparent_temperature_max,apparent_temperature_min&forecast_days=1";

            try
            {
                var response = await client.GetAsync(url);

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

                context.API.LogWarn(nameof(OpenMeteoApiClient), "No weather data found in response.");
                return null; // No weather data found
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                context.API.LogWarn(nameof(OpenMeteoApiClient), $"Bad Request: {ex.Message}");
                return null; // Handle specific error case
            }
            catch (Exception ex)
            {
                context.API.LogException(nameof(OpenMeteoApiClient), "An error occurred while fetching the weather forecast.", ex);
                return null; // Handle general exceptions
            }
        }


    }


    public class WeatherForecast
    {
        public string Latitude { get; set; }
        public string Longitude { get; set; }
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