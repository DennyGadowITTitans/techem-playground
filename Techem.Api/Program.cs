using System.Reflection;
using Microsoft.OpenApi.Models;
using Techem.Api.Security;
using Techem.Api.Services;
using Techem.Cache.Protos;

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

// Add gRPC client for Techem.Cache
builder.Services.AddGrpcClient<ConfigurationService.ConfigurationServiceClient>(options =>
{
    options.Address = new Uri(builder.Configuration.GetValue<string>("TechemCache:GrpcAddress") ?? "https://localhost:7159");
});

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