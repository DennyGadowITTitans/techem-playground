using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Techem.Cache.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to support HTTP/2 for gRPC
builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureEndpointDefaults(listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

// Add gRPC services
builder.Services.AddGrpc();

// Add Redis distributed cache
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = "TechemCache";
});

// Register application services
builder.Services.AddScoped<ICacheService, RedisCacheService>();
builder.Services.AddScoped<IConfigurationDatabaseService, DummyConfigurationDatabaseService>();

// Add logging
builder.Services.AddLogging();

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck("redis", () => HealthCheckResult.Healthy("Redis connection healthy"));

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseRouting();

// Map gRPC service
app.MapGrpcService<ConfigurationService>();

// Add health check endpoint
app.MapHealthChecks("/health");

// Add simple endpoint for service discovery
app.MapGet("/", () => "Techem.Cache gRPC Service is running");

app.Run();
