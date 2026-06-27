using IAppraise.Middleware;
using IAppraise.Services;
using Integrations;
using Integrations.Auth;
using Integrations.Configuration;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<IAppraiseOptions>(
    builder.Configuration.GetSection(IAppraiseOptions.SectionName));
builder.Services.Configure<ApiKeyOptions>(
    builder.Configuration.GetSection(ApiKeyOptions.SectionName));

builder.Services.AddHttpClient(IAppraiseAuthenticator.HttpClientName, (sp, http) =>
{
    var opts = sp.GetRequiredService<IOptions<IAppraiseOptions>>().Value;
    http.BaseAddress = new Uri(opts.BaseUrl);
    http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Services.AddHttpClient(IAppraiseApi.HttpClientName, (sp, http) =>
{
    var opts = sp.GetRequiredService<IOptions<IAppraiseOptions>>().Value;
    http.BaseAddress = new Uri(opts.BaseUrl);
    http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Services.AddSingleton<IIAppraiseAuthenticator, IAppraiseAuthenticator>();
builder.Services.AddScoped<IIAppraiseApi, IAppraiseApi>();
builder.Services.AddScoped<IBookingService, BookingService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "eziKey iAppraise Facade v1");
    options.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.UseMiddleware<ApiKeyMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
