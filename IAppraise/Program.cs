
using Integrations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IIAppraiseApi, IAppraiseApi>();

var app = builder.Build();

app.UseSwagger();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Web API V1");
    // if (app.Environment.IsDevelopment())
    options.RoutePrefix = "swagger";
    //else
    //  options.RoutePrefix = string.Empty;
}
);
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
