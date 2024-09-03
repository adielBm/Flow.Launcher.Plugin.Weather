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
            string url = $"{BaseUrl}?latitude={latitude}&longitude={longitude}&forecast_days=1";

            // current
            url += "&current=temperature_2m,apparent_temperature,is_day,weather_code,relative_humidity_2m,surface_pressure,wind_speed_10m,precipitation";
            // daily
            url += "&daily=apparent_temperature_max,apparent_temperature_min,uv_index_max";

            var response = await client.GetAsync(url);

            response.EnsureSuccessStatusCode();  // Throws if status code is not 2xx

            var responseString = await response.Content.ReadAsStringAsync();

            var json = JsonDocument.Parse(responseString);
            var root = json.RootElement;

            // Check if there's any data
            if (root.TryGetProperty("current", out JsonElement currentWeather) &&
                root.TryGetProperty("current_units", out JsonElement currentUnits) &&
                root.TryGetProperty("daily", out JsonElement dailyWeather) &&
                root.TryGetProperty("daily_units", out JsonElement dailyUnits)
                )
            {
                var forecast = new WeatherForecast
                {
                    Latitude = latitude,
                    Longitude = longitude,
                    Current = new CurrentWeather
                    {
                        Time = currentWeather.GetProperty("time").GetDateTime(),
                        Temperature2m = Math.Round(currentWeather.GetProperty("temperature_2m").GetDouble()),
                        ApparentTemperature = Math.Round(currentWeather.GetProperty("apparent_temperature").GetDouble()),
                        IsDay = currentWeather.GetProperty("is_day").GetInt32(),
                        WeatherCode = currentWeather.GetProperty("weather_code").GetInt32(),
                        RelativeHumidity = currentWeather.GetProperty("relative_humidity_2m").GetDouble(),
                        SurfacePressure = currentWeather.GetProperty("surface_pressure").GetDouble(),
                        WindSpeed = currentWeather.GetProperty("wind_speed_10m").GetDouble(),
                        Precipitation = currentWeather.GetProperty("precipitation").GetDouble()
                    },
                    CurrentUnits = new CurrentUnits(),
                    Daily = new DailyWeather
                    {
                        Time = dailyWeather.GetProperty("time").EnumerateArray().Select(d => d.GetDateTime()).ToArray(),
                        ApparentTemperatureMax = dailyWeather.GetProperty("apparent_temperature_max").EnumerateArray().Select(d => Math.Round(d.GetDouble())).ToArray(),
                        ApparentTemperatureMin = dailyWeather.GetProperty("apparent_temperature_min").EnumerateArray().Select(d => Math.Round(d.GetDouble())).ToArray(),
                        UvIndexMax = dailyWeather.GetProperty("uv_index_max").EnumerateArray().Select(d => Math.Round(d.GetDouble())).ToArray(),
                    },
                    DailyUnits = new DailyUnits()
                };

                // Safely get current units
                if (currentUnits.TryGetProperty("temperature_2m", out JsonElement tempUnit))
                    forecast.CurrentUnits.Temperature = tempUnit.GetString();
                if (currentUnits.TryGetProperty("wind_speed_10m", out JsonElement windUnit))
                    forecast.CurrentUnits.WindSpeed = windUnit.GetString();
                if (currentUnits.TryGetProperty("surface_pressure", out JsonElement pressureUnit))
                    forecast.CurrentUnits.SurfacePressure = pressureUnit.GetString();
                if (currentUnits.TryGetProperty("precipitation", out JsonElement precipUnit))
                    forecast.CurrentUnits.Precipitation = precipUnit.GetString();
                if (currentUnits.TryGetProperty("relative_humidity_2m", out JsonElement humidityUnit))
                    forecast.CurrentUnits.RelativeHumidity = humidityUnit.GetString();

                // Safely get daily units
                if (currentUnits.TryGetProperty("uv_index_max", out JsonElement uvUnit))
                    forecast.DailyUnits.Temperature = uvUnit.GetString();

                return forecast;
            }

            return null; // No weather data found
        }

        private const string BaseUrl = "https://api.open-meteo.com/v1/forecast";
    }

    internal class DailyUnits
    {
        public string Temperature { get; set; }

    }

    internal class CurrentUnits
    {
        public string Temperature { get; set; }
        public string WindSpeed { get; set; }
        public string SurfacePressure { get; set; }
        public string Precipitation { get; set; }
        public string RelativeHumidity { get; set; }
    }


    public class WeatherForecast
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public CurrentWeather Current { get; set; }
        public DailyWeather Daily { get; set; }
        internal CurrentUnits CurrentUnits { get; set; }
        internal DailyUnits DailyUnits { get; set; }
    }

    public class CurrentWeather
    {
        public DateTime Time { get; set; }
        public double Temperature2m { get; set; }
        public double ApparentTemperature { get; set; }
        public int IsDay { get; set; }
        public int WeatherCode { get; set; }
        public double RelativeHumidity { get; set; }
        public double SurfacePressure { get; set; }
        public double WindSpeed { get; set; }
        public double Precipitation { get; set; }

    }

    public class DailyWeather
    {
        public DateTime[] Time { get; set; }
        public double[] ApparentTemperatureMax { get; set; }
        public double[] ApparentTemperatureMin { get; set; }
        public double[] UvIndexMax { get; set; }
    }
}
