using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using WeatherApi.Models;
using WeatherApi.Options;

namespace WeatherApi.Services;

public class WeatherServiceConcurrentDictionaryLazy(
    IHttpClientFactory httpClientFactory,
    IOptions<WeatherOptions> options,
    ILogger<WeatherServiceHybridCache> logger)
    : WeatherService(httpClientFactory, options, logger), IWeatherService
{
    private readonly ConcurrentDictionary<string, Lazy<Task<WeatherResponse?>>> _cache = new();
    private const int MaxCacheSize = 1000;

    public async Task<WeatherResponse?> GetCurrentWeatherAsync(string city,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"weather:{city}";

        if (_cache.Count >= MaxCacheSize)
        {
            var firstKey = _cache.Keys.FirstOrDefault();
            if (firstKey != null)
                _cache.TryRemove(firstKey, out _);
        }

        var lazyTask = _cache
            .GetOrAdd(cacheKey, _ => new Lazy<Task<WeatherResponse?>>(() => GetWeatherAsync(city, cancellationToken)));

        try
        {
            return await lazyTask.Value;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error computing value for key {cacheKey}: {ex.Message}");
            _cache.TryRemove(cacheKey, out _);
            throw;
        }
    }
}