using Microsoft.Extensions.Options;
using WeatherApi.Models;
using WeatherApi.Options;

namespace WeatherApi.Services;

public abstract class WeatherService(
    IHttpClientFactory httpClientFactory,
    IOptions<WeatherOptions> options,
    ILogger<WeatherServiceHybridCache> logger)
{
    private readonly WeatherOptions _options = options.Value;

    protected async Task<WeatherResponse?> GetWeatherAsync(string city, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Getting weather for {City}", city);

        await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        var url = $"https://api.openweathermap.org/data/2.5/weather?q={city}&appid={_options.AppId}";
        var httpClient = httpClientFactory.CreateClient();
        var response = await httpClient.GetAsync(url, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<WeatherResponse>(cancellationToken);
    }
}