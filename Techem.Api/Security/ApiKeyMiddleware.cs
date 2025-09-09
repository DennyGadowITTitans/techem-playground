using System.Security.Claims;

namespace Techem.Api.Security;

public class ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
{
    private const string HeaderName = "X-Api-Key"; // per OpenAPI spec
    private readonly string? _expectedKey = configuration["ApiKey:Value"];

    public async Task InvokeAsync(HttpContext context)
    {
        // Only protect the DigitalTwin endpoint path(s)
        if (context.Request.Path.StartsWithSegments("/device", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(_expectedKey))
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsync("API key is not configured");
                return;
            }

            if (!context.Request.Headers.TryGetValue(HeaderName, out var provided) || string.IsNullOrWhiteSpace(provided))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Missing API key");
                return;
            }

            if (!string.Equals(provided.ToString(), _expectedKey, StringComparison.Ordinal))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Invalid API key");
                return;
            }

            // Optionally set a simple identity for downstream
            var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "ApiKeyUser")], "ApiKey");
            context.User = new ClaimsPrincipal(identity);
        }

        await next(context);
    }
}
