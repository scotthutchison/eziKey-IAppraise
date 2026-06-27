namespace IAppraise.Middleware
{
    public class ApiKeyOptions
    {
        public const string SectionName = "KioskApi";
        public const string HeaderName = "X-Api-Key";

        public string? ApiKey { get; set; }
    }

    public class ApiKeyMiddleware
    {
        private static readonly string[] BypassPrefixes =
        {
            "/swagger",
            "/health"
        };

        private readonly RequestDelegate _next;
        private readonly ApiKeyOptions _options;
        private readonly ILogger<ApiKeyMiddleware> _logger;

        public ApiKeyMiddleware(RequestDelegate next, Microsoft.Extensions.Options.IOptions<ApiKeyOptions> options, ILogger<ApiKeyMiddleware> logger)
        {
            _next = next;
            _options = options.Value;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            if (BypassPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context);
                return;
            }

            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                _logger.LogError("KioskApi:ApiKey is not configured. Refusing all requests until set.");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsync("API key is not configured on the server.");
                return;
            }

            if (!context.Request.Headers.TryGetValue(ApiKeyOptions.HeaderName, out var provided)
                || !string.Equals(provided.ToString(), _options.ApiKey, StringComparison.Ordinal))
            {
                _logger.LogWarning("Rejected request to {Path} — missing or invalid API key.", path);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Missing or invalid API key.");
                return;
            }

            await _next(context);
        }
    }
}
