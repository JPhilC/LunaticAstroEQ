using OpenWeatherMap;
using System;
using System.Threading.Tasks;

namespace ASCOM.LunaticAstroEQ.Core.Services
{
   public sealed class WeatherService
   {
      public static async Task<double> GetCurrentTemperature(double latitude, double longitude)
      {
         OpenWeatherMapClient webClient = new OpenWeatherMapClient("e66f368a625694e6acc918fd6c845ad7");
         try
         {
            CurrentWeatherResponse response = await webClient.CurrentWeather.GetByCoordinates(new Coordinates { Latitude = latitude, Longitude = longitude },
               MetricSystem.Metric);

            return response.Temperature.Value;
         }
         catch (Exception ex)
         {
            // Internet is not available
            return double.NaN;
         }

      }
   }
}
