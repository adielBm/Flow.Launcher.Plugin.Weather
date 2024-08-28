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
            if (string.IsNullOrWhiteSpace(query.Search))
            {
                // return empty
                return new List<Result>
                {
                    new Result
                    {
                        Title = "Please enter a city name",
                        IcoPath = "Images\\weather-icon.png"
                    }
                };
            }
            try
            {
                token.ThrowIfCancellationRequested();

                // Get city data
                GeocodingOptions geocodingData = new GeocodingOptions(query.Search);
                var geocodingResult = await _client.GetLocationDataAsync(geocodingData);

                if (geocodingResult?.Locations == null)
                {
                    return new List<Result>
                        {
                            new Result
                            {
                                Title = "City not found",
                                SubTitle = "Please check the city name and try again",
                                IcoPath = "Images\\weather-icon.png"
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
                                    Title = $"{temp}{(UseFahrenheit ? "°F" : "°C")}",
                                    SubTitle = $"{cityData.Name}, {cityData.Country}",
                                    IcoPath = "Images\\weather-icon.png"
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
                        IcoPath = "Images\\weather-icon.png"
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
    }
}