using WeatherApi.Models;

namespace WeatherApi.Services;

public interface IWeatherService
{
    public Task<WeatherResponse?> GetCurrentWeatherAsync(string city, CancellationToken cancellationToken = default);
}