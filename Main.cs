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

namespace Flow.Launcher.Plugin.Weather
{
    public class Main : IAsyncPlugin // , ISettingProvider
    {
        private string ClassName => GetType().Name;
        private PluginInitContext _context;
        // private Settings _settings;

        // public Control CreateSettingPanel()
        // {
        //     return new WeatherSettings(_settings);
        // }


        private readonly OpenMeteoClient _client;
        public Main()
        {
            _client = new OpenMeteoClient();
        }

        public Task InitAsync(PluginInitContext context)
        {
            _context = context;
            return Task.CompletedTask;
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
                    Temperature_Unit = TemperatureUnitType.celsius
                    // Temperature_Unit = _settings?.TemperatureUnit == "Celsius" ? TemperatureUnitType.celsius : TemperatureUnitType.fahrenheit,
                };

                if (cityData?.Name != null)
                {
                    WeatherForecast weatherData = await _client.QueryAsync(cityData?.Name/* , options */);

                    // _context.API.ShowMsgError(ClassName, $"WEATHER: c:{cityData.Latitude}:{cityData.Longitude}; w:{weatherData.Latitude}:{weatherData.Longitude}; {weatherData?.Daily?.Apparent_temperature_max[0]}, {weatherData?.Current?.Apparent_temperature}");

                    token.ThrowIfCancellationRequested();

                    if (weatherData != null)
                    {
                        return new List<Result>
                            {
                                new Result
                                {
                                    Title = $"{weatherData?.Current?.Apparent_temperature}°C",
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
    }

    /*  public partial class WeatherSettings : UserControl
     {
         private readonly Settings _settings;
         private bool _contentLoaded;

         public WeatherSettings(Settings settings)
         {
             _settings = settings;
             InitializeComponent();
             this.DataContext = settings;
         }

         private void InitializeComponent()
         {
             if (!_contentLoaded)
             {
                 _contentLoaded = true;
                 Uri resourceLocator = new Uri("/Flow.Launcher.Plugin.Weather;component/WeatherSettings.xaml", UriKind.Relative);
                 System.Windows.Application.LoadComponent(this, resourceLocator);
             }
         }
     } */


    /*  public class Settings
     {
         public string TemperatureUnit { get; set; } = "Celsius";

         [JsonIgnore]
         public List<string> TemperatureUnits { get; } = new List<string> { "Celsius", "Fahrenheit" };
     }

     public static class Temperature
     {
         public static double CelsiusToFahrenheit(double celsius)
         {
             return (celsius * 9 / 5) + 32;
         }
     } */

}