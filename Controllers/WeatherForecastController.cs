using Microsoft.AspNetCore.Mvc;
using WeatherApi.Services;

namespace WeatherApi.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    private readonly IWeatherService _weatherService;

    public WeatherForecastController([FromKeyedServices("hybridCache")] IWeatherService weatherService)
    {
        _weatherService = weatherService;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<WeatherForecast> Get()
    {
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
    }

    [HttpGet("/weather/{city}")]
    public async Task<IResult> Get(string city, CancellationToken cancellationToken)
    {
        var weather = await _weatherService.GetCurrentWeatherAsync(city, cancellationToken);
        return weather is null ? Results.NotFound() : Results.Ok(weather);
    }
}