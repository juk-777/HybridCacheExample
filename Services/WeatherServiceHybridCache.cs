using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using WeatherApi.Models;
using WeatherApi.Options;

namespace WeatherApi.Services;

public class WeatherServiceHybridCache(
    IHttpClientFactory httpClientFactory,
    IOptions<WeatherOptions> options,
    HybridCache hybridCache,
    ILogger<WeatherServiceHybridCache> logger)
    : WeatherService(httpClientFactory, options, logger), IWeatherService
{
    public async Task<WeatherResponse?> GetCurrentWeatherAsync(string city,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"weather:{city}";
        var cacheValue = await hybridCache.GetOrCreateAsync<WeatherResponse?>(
            cacheKey,
            async ct => await GetWeatherAsync(city, ct),
            cancellationToken: cancellationToken);

        return cacheValue;
    }
}