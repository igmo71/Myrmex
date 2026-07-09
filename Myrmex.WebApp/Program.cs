using Microsoft.AspNetCore.Localization;
using MudBlazor.Services;
using MudBlazor.Translations;
using Myrmex.AspNetCore.Security;
using Myrmex.Identity.Infrastructure;
using Myrmex.ServiceDefaults;
using Myrmex.WebApp;
using Myrmex.WebApp.Components;
using Myrmex.WebApp.Identity;
using Myrmex.WebApp.Integrations.OneC;
using Myrmex.WebApp.Wms.Catalog;
using Myrmex.WebApp.Wms.Inventory;
using Myrmex.WebApp.Wms.Topology;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("ru-RU");
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("ru-RU");

CultureInfo[] supportedCultures =
[
    new("ru-RU"),
    new("en-US")
];

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
builder.AddRedisOutputCache("cache");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddMyrmexIdentity(builder.Configuration);
builder.Services.AddMyrmexIdentityDataProtection(
    builder.Configuration,
    builder.Environment);
builder.Services.AddMyrmexIdentityWebAppAuthentication();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(
        MyrmexAuthorizationPolicies.WmsOperator,
        MyrmexAuthorizationPolicies.ConfigureWmsOperator)
    .AddPolicy(
        MyrmexAuthorizationPolicies.MyrmexAdmin,
        MyrmexAuthorizationPolicies.ConfigureMyrmexAdmin);
builder.Services.AddScoped<ProtectedApiClientFactory>();

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("ru-RU");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

builder.Services.AddHttpClient<WeatherApiClient>(client =>
{
    // This URL uses "https+http://" to indicate HTTPS is preferred over HTTP.
    // Learn more about service discovery scheme resolution at https://aka.ms/dotnet/sdschemes.
    client.BaseAddress = new("https+http://apiservice");
});

builder.Services.AddHttpClient("WmsTopologyApiClient");
builder.Services.AddScoped<WmsTopologyApiClient>(services => new(
    services.GetRequiredService<ProtectedApiClientFactory>().CreateClient(
        "WmsTopologyApiClient",
        new Uri("https+http://apiservice"))));

builder.Services.AddHttpClient("WmsCatalogApiClient");
builder.Services.AddScoped<WmsCatalogApiClient>(services => new(
    services.GetRequiredService<ProtectedApiClientFactory>().CreateClient(
        "WmsCatalogApiClient",
        new Uri("https+http://apiservice"))));

builder.Services.AddHttpClient("WmsInventoryApiClient");
builder.Services.AddScoped<WmsInventoryApiClient>(services => new(
    services.GetRequiredService<ProtectedApiClientFactory>().CreateClient(
        "WmsInventoryApiClient",
        new Uri("https+http://apiservice"))));

builder.Services.AddHttpClient("IdentityApiClient");
builder.Services.AddScoped<IdentityApiClient>(services => new(
    services.GetRequiredService<ProtectedApiClientFactory>().CreateClient(
        "IdentityApiClient",
        new Uri("https+http://apiservice"))));

#pragma warning disable EXTEXP0001
builder.Services.AddHttpClient("OneCIntegrationApiClient")
.RemoveAllResilienceHandlers(); // Long-running import calls must not be cut by default Aspire HTTP resilience timeout.
builder.Services.AddScoped<OneCIntegrationApiClient>(services => new(
    services.GetRequiredService<ProtectedApiClientFactory>().CreateClient(
        "OneCIntegrationApiClient",
        new Uri("https+http://apiservice"),
        Timeout.InfiniteTimeSpan)));
#pragma warning restore EXTEXP0001

// Add MudBlazor services
builder.Services.AddMudServices();
builder.Services.AddMudTranslations();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseHttpsRedirection();

app.UseRequestLocalization();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapMyrmexIdentityWebAppAccountEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
