using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using WeatherApi.Models;
using WeatherApi.Options;

namespace WeatherApi.Services;

public class WeatherServiceConcurrentDictionarySemaphore(
    IHttpClientFactory httpClientFactory,
    IOptions<WeatherOptions> options,
    ILogger<WeatherServiceHybridCache> logger)
    : WeatherService(httpClientFactory, options, logger), IWeatherService
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new();
    private readonly ConcurrentDictionary<string, Task<WeatherResponse?>> _cache = new();
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

        try
        {
            var semaphore = _semaphores.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));

            await semaphore.WaitAsync(cancellationToken);

            try
            {
                if (!_cache.TryGetValue(cacheKey, out var cachedTask))
                {
                    var task = GetWeatherAsync(city, cancellationToken);
                    _cache[cacheKey] = task;

                    _ = task.ContinueWith(_ => _semaphores.TryRemove(cacheKey, out var _), TaskScheduler.Default);

                    return await task;
                }

                return await cachedTask;
            }
            finally
            {
                semaphore.Release();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error computing value for key {cacheKey}: {ex.Message}");
            throw;
        }
    }
}