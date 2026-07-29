using IAppraise;
using Integrations;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog: pull config from appsettings (Serilog section). Enrich every log line with the
// request's TraceIdentifier so an inbound touchscreen call and the outbound TDL calls it
// triggers can be correlated in the log by searching for the same trace id.
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
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
