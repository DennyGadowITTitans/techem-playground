using System.Reflection;
using Microsoft.OpenApi.Models;
using Techem.Api.Security;
using Techem.Api.Services;
using Techem.Api.Services.Cache;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
    
    // Add API Key authentication
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        Name = "X-API-Key",
        In = ParameterLocation.Header,
        Description = "API Key needed to access the endpoints"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            []
        }
    });
});



// Add MVC controllers
builder.Services.AddControllers();

// Add Redis cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
});

// Add cache services - using Azure Table Storage as primary cache
builder.Services.AddScoped<ICacheService, AzureTableStorageCacheService>();
builder.Services.AddScoped<IConfigurationDatabaseService, DummyConfigurationDatabaseService>();

// Add configuration service that implements cache-aside pattern
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();

// Add load testing service
builder.Services.AddScoped<ILoadTestService, LoadTestService>();

// Business logic service (dummy for now)
builder.Services.AddScoped<IGdprCheckService, DummyGdprCheckService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseMiddleware<ApiKeyMiddleware>();

app.MapControllers();

app.Run();