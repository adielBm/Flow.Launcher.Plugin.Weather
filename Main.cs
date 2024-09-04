using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Controls;
using System.Net.Http;
using System.Text.Json;

using Flow.Launcher.Plugin;
using Flow.Launcher.Plugin.SharedCommands;

namespace Flow.Launcher.Plugin.Weather
{
    public class Main : IAsyncPlugin, ISettingProvider, IContextMenu
    {
        private string ClassName => GetType().Name;
        private PluginInitContext _context;
        private Settings _settings;
        private bool UseFahrenheit => _settings.useFahrenheit;

        // OLD client
        // private readonly OpenMeteoClient _client;

        // NEW client
        private readonly OpenMeteoApiClient _weather_client;

        private readonly CityLookupService cityService;

        public Main()
        {
            // OLD client
            // _client = new OpenMeteoClient();
            // NEW client
            _weather_client = new OpenMeteoApiClient();
            cityService = new CityLookupService();
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
                // OLD 
                // GeocodingOptions geocodingData = new GeocodingOptions(searchTerm);
                // GeocodingApiResponse geocodingResult = await _client.GetLocationDataAsync(geocodingData);

                // NEW
                var cityDetails = await cityService.GetCityDetailsAsync(searchTerm);

                if (cityDetails == null || cityDetails?.Latitude == null || cityDetails?.Longitude == null)
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


                token.ThrowIfCancellationRequested();

                WeatherForecast weatherData = await _weather_client.GetForecastAsync(cityDetails.Latitude, cityDetails.Longitude);

                token.ThrowIfCancellationRequested();

                if (weatherData == null)
                {
                    return new List<Result>
                        {
                            new Result
                            {
                                Title = $"Weather data not found for {cityDetails.Name}",
                                SubTitle = $"Please try again later",
                                IcoPath = "Images\\weather-icon.png",
                                AutoCompleteText = "",
                            }
                        };
                }

                // Get temperature
                double temp = weatherData.Current.Temperature2m;
                if (UseFahrenheit)
                {
                    temp = CelsiusToFahrenheit(temp);
                }

                // Set day or night
                bool isNight = weatherData?.Current?.IsDay == 0;
                string dayOrNight = isNight ? "night" : "day";

                // Set glyph (if enabled)
                GlyphInfo glyph = null;
                if (_settings.useGlyphs)
                {
                    string fontPath = Path.Combine(_context.CurrentPluginMetadata.PluginDirectory, "Resources", "easy.ttf");
                    PrivateFontCollection privateFonts = new PrivateFontCollection();
                    privateFonts.AddFontFile(fontPath);
                    glyph = new GlyphInfo(
                        FontFamily: fontPath,
                        Glyph: GetWeatherIconUnicode((int)(weatherData?.Current?.WeatherCode), isNight)
                    );
                }

                // subtitle
                var subTitle = $"{cityDetails.Name}";
                if (weatherData?.Daily?.ApparentTemperatureMax != null && weatherData?.Daily?.ApparentTemperatureMin != null)
                {
                    subTitle += $" | Min: {(UseFahrenheit ? CelsiusToFahrenheit(weatherData.Daily.ApparentTemperatureMin[0]) : weatherData.Daily.ApparentTemperatureMin[0])} °{(UseFahrenheit ? "F" : "C")}";
                    subTitle += $", Max: {(UseFahrenheit ? CelsiusToFahrenheit(weatherData.Daily.ApparentTemperatureMax[0]) : weatherData.Daily.ApparentTemperatureMax[0])} °{(UseFahrenheit ? "F" : "C")}";
                }

                // Result
                return new List<Result>
                        {
                        new Result
                            {
                                Title = $"{temp} {(UseFahrenheit ? "°F" : "°C")}",
                                SubTitle = subTitle,
                                // SubTitle = $"{_client.WeathercodeToString((int)(weatherData?.Current?.Weathercode))} ({weatherData?.Current?.Weathercode}) ({dayOrNight}) | {cityData.Name}, {cityData.Country}",
                                IcoPath = $"Images\\{GetWeatherIcon((int)(weatherData?.Current?.WeatherCode), isNight)}.png",
                                Glyph = glyph,
                            },
                        new Result
                            {
                                Title = $"{weatherData.Current.WindSpeed} {weatherData.CurrentUnits.WindSpeed}",
                                SubTitle = $"Wind speed",
                                IcoPath = "Images\\wind.png",
                            },
                        new Result
                            {
                                Title = $"{weatherData.Current.Precipitation} {weatherData.CurrentUnits.Precipitation}",
                                SubTitle = $"Precipitation",
                                IcoPath = "Images\\raindrop-measure.png",
                            },
                        new Result
                            {
                                Title = $"{weatherData.Current.SurfacePressure} {weatherData.CurrentUnits.SurfacePressure}",
                                SubTitle = $"Pressure",
                                IcoPath = "Images\\barometer.png",
                            },
                        new Result
                            {
                                Title = $"{weatherData.Current.RelativeHumidity} {weatherData.CurrentUnits.RelativeHumidity}",
                                SubTitle = $"Relative humidity",
                                IcoPath = "Images\\humidity.png"
                            },
                        new Result
                            {
                                Title = $"{weatherData.Daily.UvIndexMax[0]}",
                                SubTitle = $"UV Index",
                                IcoPath = $"Images\\uv-index-{weatherData.Daily.UvIndexMax[0]}.png"
                            }
                        };
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

        /* 
        Weather variable documentation
        WMO Weather interpretation codes (WW)
        Code	Description
        0	Clear sky
        1 Mainly clear
        2 Partly cloudy
        3 Overcast
        45, 48	Fog and depositing rime fog (resp.)
        51, 53, 55	Drizzle: Light, moderate, and dense intensity (resp.)
        56, 57	Freezing Drizzle: Light and dense intensity (resp.)
        61, 63, 65	Rain: Slight, moderate and heavy intensity (resp.)
        66, 67	Freezing Rain: Light and heavy intensity (resp.)
        71, 73, 75	Snow fall: Slight, moderate, and heavy intensity (resp.)
        77	Snow grains
        80, 81, 82	Rain showers: Slight, moderate, and violent (resp.)
        85, 86	Snow showers slight and heavy (resp.)
        95 *	Thunderstorm: Slight or moderate
        96, 99 *	Thunderstorm with slight and heavy hail (resp.)
        (*) Thunderstorm forecast with hail is only available in Central Europe
         */
        public string GetWeatherIcon(int wmoCode, bool isNight = false)
        {
            if (isNight)
            {
                return wmoCode switch
                {
                    0 => "clear-night",
                    1 or 2 => "partly-cloudy-night",
                    3 => "overcast-night",
                    45 => "fog-night",
                    48 => "haze-night",
                    51 or 53 or 55 => "partly-cloudy-night-drizzle",
                    56 or 57 => "partly-cloudy-night-sleet",
                    61 or 63 or 65 => "partly-cloudy-night-rain",
                    66 or 67 => "partly-cloudy-night-sleet",
                    71 or 73 or 75 => "partly-cloudy-night-snow",
                    77 => "snow",
                    80 or 81 => "partly-cloudy-night-rain",
                    82 => "thunderstorms-night-rain",
                    85 or 86 => "partly-cloudy-night-snow",
                    95 => "thunderstorms-night",
                    96 or 99 => "thunderstorms-night-snow",
                    31 or 32 or 33 or 34 => "dust-night",
                    _ => GetDayIcon(wmoCode),
                };
            }

            return GetDayIcon(wmoCode);
        }


        private string GetDayIcon(int wmoCode)
        {
            return wmoCode switch
            {
                0 => "clear-day",
                1 or 2 => "partly-cloudy-day",
                3 => "overcast-day",
                45 => "fog-day",
                48 => "haze-day",
                51 or 53 or 55 => "drizzle",
                56 or 57 => "partly-cloudy-day-sleet",
                61 or 63 or 65 => "rain",
                66 => "partly-cloudy-day-sleet",
                67 => "sleet",
                71 => "snowflake",
                73 or 75 => "snow",
                77 => "snow",
                80 or 81 => "partly-cloudy-day-rain",
                82 => "thunderstorms-day-rain",
                85 or 86 => "partly-cloudy-day-snow",
                95 => "thunderstorms-day",
                96 or 99 => "thunderstorms-day-snow",
                31 or 32 or 33 or 34 => "dust-day",
                _ => "not-available",
            };
        }

        public static string GetWeatherIconUnicode(int wmoCode, bool isNight = false)
        {
            return wmoCode switch
            {
                0 or 1 => isNight ? "\uE96E" : "\uE96D", // Clear sky
                2 => isNight ? "\uE96B" : "\uE96A", // Partly cloudy
                3 => "\uE95D", // Overcast
                45 => "\uE972", // Fog
                48 => "\uE973", // Depositing rime fog
                51 => "\uE974", // Drizzle: Light
                53 => "\uE975", // Drizzle: Moderate
                55 => "\uE976", // Drizzle: Dense intensity
                61 => "\uE977", // Rain: Slight
                63 => "\uE978", // Rain: Moderate
                65 => "\uE979", // Rain: Heavy
                71 => "\uE97A", // Snow fall: Slight
                73 => "\uE97B", // Snow fall: Moderate
                75 => "\uE97C", // Snow fall: Heavy
                80 => "\uE97D", // Rain showers: Slight
                81 => "\uE97E", // Rain showers: Moderate
                82 => "\uE97F", // Rain showers: Violent
                95 => "\uE980", // Thunderstorm: Slight or moderate
                96 => "\uE981", // Thunderstorm with slight hail
                99 => "\uE982", // Thunderstorm with heavy hail
                _ => "\uE9A1" // Default icon for unrecognized codes
            };
        }

    }


    public class CityDetails
    {
        public string DisplayName { get; set; }
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class CityLookupService
    {
        private static readonly HttpClient client = new HttpClient();

        public CityLookupService()
        {
            client.DefaultRequestHeaders.Add("User-Agent", "Flow.Launcher.Plugin.Weather");
        }

        public async Task<CityDetails> GetCityDetailsAsync(string cityName)
        {
            string url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(cityName)}&format=json&limit=1&accept-language=en";
            var response = await client.GetAsync(url);

            response.EnsureSuccessStatusCode();  // This will throw if the status code is not 2xx

            var responseString = await response.Content.ReadAsStringAsync();

            var json = JsonDocument.Parse(responseString);
            var root = json.RootElement;

            if (root.GetArrayLength() == 0)
            {
                return null; // No city found
            }

            var cityInfo = root[0];

            return new CityDetails
            {
                DisplayName = cityInfo.GetProperty("display_name").GetString(),
                Name = cityInfo.GetProperty("name").GetString(),
                Latitude = double.Parse(cityInfo.GetProperty("lat").GetString()),
                Longitude = double.Parse(cityInfo.GetProperty("lon").GetString())
            };
        }

    }

}