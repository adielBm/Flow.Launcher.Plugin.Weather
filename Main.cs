using System;
using System.Collections.Generic;
// using System.Text.Json.Serialization;
// using System.Net.Http;
// using System.Text.Json;
using System.Threading.Tasks;
using Flow.Launcher.Plugin;
using OpenMeteo;
using System.Threading;
using System.Windows.Controls;
// using System.IO;
// using System.Text.RegularExpressions;
// using System.Windows.Controls;
using Flow.Launcher.Plugin.SharedCommands;
// using System.Drawing.Text;
// using System.Runtime.InteropServices;
// using System.Windows;

namespace Flow.Launcher.Plugin.Weather
{
    public class Main : IAsyncPlugin, ISettingProvider, IContextMenu
    {
        private string ClassName => GetType().Name;
        private PluginInitContext _context;
        private Settings _settings;
        private bool UseFahrenheit => _settings.useFahrenheit;


        private readonly OpenMeteoClient _client;
        public Main()
        {
            _client = new OpenMeteoClient();
        }

        // Initialise query url
        public async Task InitAsync(PluginInitContext context)
        {
            _context = context;
            _settings = _context.API.LoadSettingJsonStorage<Settings>();


        }

        public async Task<List<Result>> QueryAsync(Query query, CancellationToken token)
        {
            var searchTerm = query.Search;

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                if (!string.IsNullOrWhiteSpace(_settings.defaultLocation))
                {
                    searchTerm = _settings.defaultLocation;
                }
                else
                {
                    return new List<Result>
                        {
                            new Result
                            {
                                Title = "Please enter a city name",
                                IcoPath = "Images\\weather-icon.png"
                            }
                        };
                }
            }
            try
            {
                token.ThrowIfCancellationRequested();

                // Get city data
                GeocodingOptions geocodingData = new GeocodingOptions(searchTerm);
                var geocodingResult = await _client.GetLocationDataAsync(geocodingData);

                if (geocodingResult?.Locations == null)
                {
                    return new List<Result>
                        {
                            new Result
                            {
                                Title = "City not found",
                                SubTitle = "Please check the city name and try again",
                                IcoPath = "Images\\weather-icon.png",
                                AutoCompleteText = "",
                            }
                        };
                }

                var cityData = geocodingResult.Locations[0];

                token.ThrowIfCancellationRequested();

                // Set custom options
                WeatherForecastOptions options = new WeatherForecastOptions
                {
                    Temperature_Unit = TemperatureUnitType.celsius,
                };

                if (cityData?.Name != null)
                {
                    WeatherForecast weatherData = await _client.QueryAsync(cityData?.Name);

                    token.ThrowIfCancellationRequested();

                    if (weatherData != null && weatherData.Current?.Apparent_temperature != null)
                    {

                        float temp = (float)weatherData.Current.Apparent_temperature;
                        if (UseFahrenheit)
                        {
                            temp = (float)CelsiusToFahrenheit(temp);
                        }

                        bool isNight = weatherData?.Current?.Is_day == 0;

                        return new List<Result>
                        {
                        new Result
                            {
                                AutoCompleteText = "",
                                Title = $"{temp}{(UseFahrenheit ? "°F" : "°C")}",
                                SubTitle = $"{_client.WeathercodeToString((int)(weatherData?.Current?.Weathercode))} | {cityData.Name}, {cityData.Country}",
                                IcoPath = $"Images\\{GetWeatherIcon((int)(weatherData?.Current?.Weathercode), isNight)}",
                            }
                        };
                    }

                }

            }
            catch (OperationCanceledException)
            {
                // return empty
                return new List<Result>();
            }
            catch (Exception ex)
            {
                return new List<Result>
                {
                    new() {
                        Title = $"err: {ex.Message}, {ex.Source}",
                        SubTitle = $"{ex.InnerException}, {ex.StackTrace}",
                        IcoPath = "Images\\weather-icon.png",
                        AutoCompleteText = "",
                    }
                };
            }
            return new List<Result>();
        }

        public void Dispose()
        {
            // _httpClient.Dispose();
        }
        public Control CreateSettingPanel() => new SettingsControl(_settings);

        public List<Result> LoadContextMenus(Result selectedResult)
        {
            var results = new List<Result>
            {

            };

            return results;
        }

        // celsius to fahrenheit
        private double CelsiusToFahrenheit(double celsius)
        {
            return celsius * 9 / 5 + 32;
        }

        public string GetWeatherIcon(int code, bool isNight = false)
        {
            if (isNight)
            {
                return code switch
                {
                    0 => "clear-night.png",
                    1 or 2 => "partly-cloudy-night.png",
                    3 => "overcast-night.png",
                    45 => "fog-night.png",
                    48 => "haze-night.png",
                    51 or 53 or 55 => "partly-cloudy-night-drizzle.png",
                    56 or 57 => "partly-cloudy-night-sleet.png",
                    61 or 63 or 65 => "partly-cloudy-night-rain.png",
                    66 or 67 => "partly-cloudy-night-sleet.png",
                    71 or 73 or 75 => "partly-cloudy-night-snow.png",
                    77 => "snow.png",
                    80 or 81 => "partly-cloudy-night-rain.png",
                    82 => "thunderstorms-night-rain.png",
                    85 or 86 => "partly-cloudy-night-snow.png",
                    95 => "thunderstorms-night.png",
                    96 or 99 => "thunderstorms-night-snow.png",
                    31 or 32 or 33 or 34 => "dust-night.png",
                    _ => GetDayIcon(code),
                };
            }

            return GetDayIcon(code);
        }


        private string GetDayIcon(int code)
        {
            return code switch
            {
                0 => "clear-day.png",
                1 or 2 => "partly-cloudy-day.png",
                3 => "overcast-day.png",
                45 => "fog-day.png",
                48 => "haze-day.png",
                51 or 53 or 55 => "drizzle.png",
                56 or 57 => "partly-cloudy-day-sleet.png",
                61 or 63 or 65 => "rain.png",
                66 => "partly-cloudy-day-sleet.png",
                67 => "sleet.png",
                71 => "snowflake.png",
                73 or 75 => "snow.png",
                77 => "snow.png",
                80 or 81 => "partly-cloudy-day-rain.png",
                82 => "thunderstorms-day-rain.png",
                85 or 86 => "partly-cloudy-day-snow.png",
                95 => "thunderstorms-day.png",
                96 or 99 => "thunderstorms-day-snow.png",
                31 or 32 or 33 or 34 => "dust-day.png",
                _ => "not-available.png",
            };
        }

    }
}