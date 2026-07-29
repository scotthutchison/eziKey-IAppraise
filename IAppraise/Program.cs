using IAppraise;
using Integrations;
using Microsoft.ApplicationInsights.Extensibility;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Application Insights auto-captures inbound HTTP requests, outbound HttpClient
// dependencies, and unhandled exceptions when a connection string is set (either
// APPLICATIONINSIGHTS_CONNECTION_STRING env var / App Setting, or the
// ApplicationInsights:ConnectionString appsettings key). It is intentionally NOT wired
// up when unset — current versions of AddApplicationInsightsTelemetry crash the host at
// startup if no connection string is present, so we guard on it here to keep local dev
// and any deployment that doesn't want AI running cleanly.
var appInsightsConnectionString =
    builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]
    ?? builder.Configuration["ApplicationInsights:ConnectionString"];

if (!string.IsNullOrWhiteSpace(appInsightsConnectionString))
{
    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = appInsightsConnectionString;
    });
}

// Serilog for our own request/response body logs. Also mirrors Serilog events into
// Application Insights (as trace telemetry) when the AI connection string is present.
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services);

    // Mirror Serilog events into AI as trace telemetry, but only if AI is actually
    // registered above — otherwise TelemetryConfiguration isn't in the container.
    if (services.GetService<TelemetryConfiguration>() is { } tc)
    {
        configuration.WriteTo.ApplicationInsights(tc, TelemetryConverter.Traces);
    }
});

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IIAppraiseApi, IAppraiseApi>();

var app = builder.Build();

app.UseSwagger();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Web API V1");
    options.RoutePrefix = "swagger";
});

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Body-level request/response logging — must run before auth so we log 401s too.
app.UseMiddleware<RequestResponseLoggingMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
