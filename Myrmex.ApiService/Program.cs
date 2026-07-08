using Myrmex.AppDispatching;
using Myrmex.AspNetCore.Security;
using Myrmex.Core.Application.Security;
using Myrmex.Identity.Infrastructure;
using Myrmex.Integrations.OneC;
using Myrmex.Integrations.OneC.Endpoints;
using Myrmex.Modules.Wms;
using Myrmex.ServiceDefaults;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IActorContext, HttpContextActorContext>();

builder.Services.AddMyrmexIdentity(builder.Configuration);
builder.Services.AddMyrmexIdentityDataProtection(
    builder.Configuration,
    builder.Environment);
builder.Services.AddMyrmexIdentityApiAuthentication(
    builder.Configuration,
    builder.Environment);

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(
        MyrmexAuthorizationPolicies.WmsOperator,
        MyrmexAuthorizationPolicies.ConfigureWmsOperator);

builder.Services.AddWmsModule(builder.Configuration);
builder.Services.AddOneCIntegration(builder.Configuration);

builder.Services.AddMyrmexAppDispatching(typeof(WmsModule).Assembly);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

LogEnvironmentAndActor(app);

app.UseAuthentication();
app.UseAuthorization();

//if (app.Environment.IsDevelopment())
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

app.MapWmsModule();
app.MapOneCIntegration();

app.Run();

static void LogEnvironmentAndActor(WebApplication app)
{
    if ((app.Environment.IsDevelopment() || app.Environment.IsStaging()) &&
        app.Configuration.GetValue<bool>(
            $"{DevelopmentActorAuthenticationHandler.ConfigurationSectionName}:Enabled"))
    {
        string actorId = app.Configuration[
            $"{DevelopmentActorAuthenticationHandler.ConfigurationSectionName}:ActorId"]
            ?? "(not configured)";

        app.Logger.LogWarning(
            "DevelopmentActor authentication is enabled. Environment={Environment}; ActorId={ActorId}",
            app.Environment.EnvironmentName,
            actorId);
    }
}

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
