using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Flow.Launcher.Plugin;
using OpenMeteo;
using System.Threading;
using System.Windows.Controls;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using Flow.Launcher.Plugin.SharedCommands;

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

                        return new List<Result>
                            {
                                new Result
                                {
                                    AutoCompleteText = "",
                                    Title = $"{temp}{(UseFahrenheit ? "°F" : "°C")}",
                                    SubTitle = $"{_client.WeathercodeToString((int)(weatherData?.Current?.Weathercode))} | {cityData.Name}, {cityData.Country}",
                                    IcoPath = $"Images\\{(_settings.useBlackIcons ? "b-" : "")}{GetWeatherSvgFilename((int)(weatherData?.Current?.Weathercode))}"
                                },
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

        public string GetWeatherSvgFilename(int weatherCode)
        {
            return weatherCode switch
            {
                0 => "wi-day-sunny.png",// Clear sky
                1 => "wi-day-sunny-overcast.png",// Mainly clear
                2 => "wi-day-cloudy.png",// Partly cloudy
                3 => "wi-cloudy.png",// Overcast
                45 => "wi-fog.png",// Fog
                48 => "wi-day-fog.png",// Depositing rime Fog
                51 => "wi-sprinkle.png",// Light drizzle
                53 => "wi-showers.png",// Moderate drizzle
                55 => "wi-raindrops.png",// Dense drizzle
                56 => "wi-day-rain-mix.png",// Light freezing drizzle
                57 => "wi-rain-mix.png",// Dense freezing drizzle
                61 => "wi-rain.png",// Slight rain
                63 => "wi-rain-wind.png",// Moderate rain
                65 => "wi-rain.png",// Heavy rain
                66 => "wi-rain-mix.png",// Light freezing rain
                67 => "wi-sleet.png",// Heavy freezing rain
                71 => "wi-snowflake-cold.png",// Slight snow fall
                73 => "wi-snow.png",// Moderate snow fall
                75 => "wi-snow.png",// Heavy snow fall
                77 => "wi-snow-wind.png",// Snow grains
                80 => "wi-showers.png",// Slight rain showers
                81 => "wi-storm-showers.png",// Moderate rain showers
                82 => "wi-thunderstorm.png",// Violent rain showers
                85 => "wi-snowflake-cold.png",// Slight snow showers
                86 => "wi-snow.png",// Heavy snow showers
                95 => "wi-thunderstorm.png",// Thunderstorm
                96 => "wi-thunderstorm.png",// Thunderstorm with light hail
                99 => "wi-thunderstorm.png",// Thunderstorm with heavy hail
                _ => "wi-na.png",// Invalid weather code
            };
        }

    }
}