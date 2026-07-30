namespace AspireApp1.Web;

// WeatherApiClient.cs
public class WeatherApiClient(HttpClient httpClient)
{
    // Метод теперь асинхронно возвращает готовый массив прогнозов
    public async Task<WeatherForecast[]> GetWeatherAsync(int maxItems = 10, CancellationToken cancellationToken = default)
    {
        var forecasts = new List<WeatherForecast>();

        await foreach (var forecast in httpClient.GetFromJsonAsAsyncEnumerable<WeatherForecast>("/weatherforecast", cancellationToken))
        {
            if (forecasts.Count >= maxItems) break;
            if (forecast is not null)
            {
                forecasts.Add(forecast);
            }
        }

        return forecasts.ToArray();
    }
}

public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

//namespace AspireApp1.Web;

//public class WeatherApiClient(HttpClient httpClient)
//{
//    public async Task<WeatherForecast[]> GetWeatherAsync(int maxItems = 10, CancellationToken cancellationToken = default)
//    {
//        List<WeatherForecast>? forecasts = null;

//        await foreach (var forecast in httpClient.GetFromJsonAsAsyncEnumerable<WeatherForecast>("/weatherforecast", cancellationToken))
//        {
//            if (forecasts?.Count >= maxItems)
//            {
//                break;
//            }
//            if (forecast is not null)
//            {
//                forecasts ??= [];
//                forecasts.Add(forecast);
//            }
//        }

//        return forecasts?.ToArray() ?? [];
//    }
//}

//public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
//{
//    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
//}
