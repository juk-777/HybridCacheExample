using Microsoft.Extensions.Caching.StackExchangeRedis;
using Serilog;
using WeatherApi.Options;
using WeatherApi.Services;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;
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

builder.Services.AddKeyedSingleton<IWeatherService, WeatherServiceConcurrentDictionary>("concurrentDict");
builder.Services.AddKeyedSingleton<IWeatherService, WeatherServiceConcurrentDictionarySemaphore>(
    "concurrentDict_semaphore");
builder.Services.AddKeyedSingleton<IWeatherService, WeatherServiceConcurrentDictionaryLazy>("concurrentDict_lazy");
builder.Services.AddKeyedSingleton<IWeatherService, WeatherServiceHybridCache>("hybridCache");

builder.Services.AddMemoryCache();
//AddHybridCache(builder.Services);
AddHybridFusionCache(builder.Services);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
return;

void AddHybridFusionCache(IServiceCollection serviceCollection)
{
    serviceCollection.AddFusionCache()
        .WithOptions(options =>
        {
            // DISTRIBUTED CACHE CIRCUIT-BREAKER
            options.DistributedCacheCircuitBreakerDuration = TimeSpan.FromSeconds(2);
        })
        .WithDefaultEntryOptions(options =>
        {
            options.Duration = TimeSpan.FromMinutes(1);

            // FAIL-SAFE OPTIONS
            options.IsFailSafeEnabled = true;
            options.FailSafeMaxDuration = TimeSpan.FromHours(2);
            options.FailSafeThrottleDuration = TimeSpan.FromSeconds(30);

            // FACTORY TIMEOUTS
            options.FactorySoftTimeout = TimeSpan.FromMilliseconds(100);
            options.FactoryHardTimeout = TimeSpan.FromMilliseconds(1500);

            // DISTRIBUTED CACHE OPTIONS
            options.DistributedCacheSoftTimeout = TimeSpan.FromSeconds(1);
            options.DistributedCacheHardTimeout = TimeSpan.FromSeconds(2);
            options.AllowBackgroundDistributedCacheOperations = true;

            // JITTERING
            options.JitterMaxDuration = TimeSpan.FromSeconds(2);
        })
        .WithSerializer(new FusionCacheSystemTextJsonSerializer())
        .WithRegisteredMemoryCache()
        .WithDistributedCache(new RedisCache(new RedisCacheOptions { Configuration = "localhost:6379" }))
        .WithBackplane(
            new RedisBackplane(new RedisBackplaneOptions { Configuration = "localhost:6379" })
        )
        .AsHybridCache();
}

void AddHybridCache(IServiceCollection serviceCollection)
{
#pragma warning disable EXTEXP0018
    serviceCollection.AddHybridCache();
#pragma warning restore EXTEXP0018
    
    serviceCollection.AddMemoryCache().AddStackExchangeRedisCache(options =>
    {
        options.Configuration = "localhost:6379";
    });
}