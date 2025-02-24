using Microsoft.Extensions.Caching.StackExchangeRedis;
using Serilog;
using WeatherApi.Options;
using WeatherApi.Services;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Is(Serilog.Events.LogEventLevel.Information)
    .WriteTo.Console()
    .CreateLogger();
builder.Services.AddLogging(configure => configure.AddSerilog());

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddHttpClient();
builder.Services.Configure<WeatherOptions>(builder.Configuration.GetSection("WeatherOptions"));

builder.Services.AddKeyedSingleton<IWeatherService, WeatherServiceHybridCache>("hybridCache");
builder.Services.AddKeyedSingleton<IWeatherService, WeatherServiceConcurrentDictionary>("concurrentDict");
builder.Services.AddKeyedSingleton<IWeatherService, WeatherServiceConcurrentDictionarySemaphore>("concurrentDict_semaphore");
builder.Services.AddKeyedSingleton<IWeatherService, WeatherServiceConcurrentDictionaryLazy>("concurrentDict_lazy");

builder.Services.AddMemoryCache();
builder.Services.AddFusionCache()
    .WithDefaultEntryOptions(options => options.Duration = TimeSpan.FromMinutes(1))
    .WithSerializer(new FusionCacheSystemTextJsonSerializer())
    .WithRegisteredMemoryCache()
    .WithDistributedCache(new RedisCache(new RedisCacheOptions { Configuration = "localhost:6379" }))
    .AsHybridCache();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
