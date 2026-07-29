using IAppraise;
using Integrations;
using Microsoft.ApplicationInsights.Extensibility;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Application Insights auto-captures inbound HTTP requests, outbound HttpClient
// dependencies, and unhandled exceptions. It reads its connection string from either
// APPLICATIONINSIGHTS_CONNECTION_STRING (env var / App Setting) or the
// ApplicationInsights:ConnectionString section in appsettings. If neither is set the
// telemetry pipeline is a no-op — safe for local dev.
builder.Services.AddApplicationInsightsTelemetry();

// Serilog for our own request/response body logs. Also mirrors Serilog events into
// Application Insights (as trace telemetry) when the AI connection string is present.
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services);

    var aiConnectionString =
        context.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]
        ?? context.Configuration["ApplicationInsights:ConnectionString"];

    if (!string.IsNullOrWhiteSpace(aiConnectionString))
    {
        configuration.WriteTo.ApplicationInsights(
            services.GetRequiredService<TelemetryConfiguration>(),
            TelemetryConverter.Traces);
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
