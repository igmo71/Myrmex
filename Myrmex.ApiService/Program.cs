using System.Security.Claims;
using Myrmex.AppDispatching;
using Myrmex.Integrations.OneC;
using Myrmex.Integrations.OneC.Endpoints;
using Myrmex.Modules.Wms;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddWmsModule(builder.Configuration);
builder.Services.AddOneCIntegration(builder.Configuration);

builder.Services.AddMyrmexAppDispatching(typeof(WmsModule).Assembly);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app.MapGet("/", () => "API service is running. Navigate to /weatherforecast to see sample data.");

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment()
    && app.Configuration.GetValue<bool>("Myrmex:DevelopmentActor:Enabled"))
{
    var actorId = app.Configuration["Myrmex:DevelopmentActor:ActorId"];
    if (string.IsNullOrWhiteSpace(actorId))
    {
        actorId = "dev-smoke-operator";
    }

    app.Use(async (context, next) =>
    {
        Claim[] claims =
        [
            new("sub", actorId),
            new(ClaimTypes.NameIdentifier, actorId),
            new(ClaimTypes.Name, actorId)
        ];

        context.User = new ClaimsPrincipal(
            new ClaimsIdentity(claims, authenticationType: "DevelopmentActor"));

        await next(context);
    });
}

app.MapWmsModule();
app.MapOneCIntegration();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
