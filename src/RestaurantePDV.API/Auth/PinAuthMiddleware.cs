namespace RestaurantePDV.API.Auth;

public class PinAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _config;

    public PinAuthMiddleware(RequestDelegate next, IConfiguration config)
    {
        _next = next;
        _config = config;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var require = _config.GetValue<bool>("App:RequirePinHeader");
        var path = context.Request.Path.Value ?? string.Empty;

        var isPublic =
            path.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/health", StringComparison.OrdinalIgnoreCase);

        if (require && !isPublic)
        {
            var expected = _config["App:Pin"] ?? string.Empty;
            var sent = context.Request.Headers["X-Pin"].ToString();
            if (string.IsNullOrWhiteSpace(sent) || sent != expected)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("PIN inválido");
                return;
            }
        }

        await _next(context);
    }
}
