using System.Globalization;
using System.Reflection;
using AspNetCore.Localizer.Json.Commons;
using AspNetCore.Localizer.Json.Extensions;
using AspNetCore.Localizer.Json.JsonOptions;
using AspNetCore.Localizer.Json.Sample.I18NTestVersion10.Components;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

var jsonLocalizationOptions = builder.Configuration.GetSection(nameof(JsonLocalizationOptions))
    .Get<JsonLocalizationOptions>() ?? new JsonLocalizationOptions();

var defaultRequestCulture = new RequestCulture(
    jsonLocalizationOptions.DefaultCulture,
    jsonLocalizationOptions.DefaultUICulture);
var supportedCultures = jsonLocalizationOptions.SupportedCultureInfos.ToList();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddLocalization();
builder.Services.AddJsonLocalization(options =>
{
    options.ResourcesPath = jsonLocalizationOptions.ResourcesPath;
    options.AdditionalResourcesPaths = jsonLocalizationOptions.AdditionalResourcesPaths;
    options.CacheDuration = jsonLocalizationOptions.CacheDuration;
    options.SupportedCultureInfos = jsonLocalizationOptions.SupportedCultureInfos;
    options.FileEncoding = jsonLocalizationOptions.FileEncoding;
    options.LocalizationMode = LocalizationMode.I18n;
    options.LocalizerDiagnosticMode = jsonLocalizationOptions.LocalizerDiagnosticMode;
    options.MaxMissingTranslations = jsonLocalizationOptions.MaxMissingTranslations;
    options.MissingTranslationRetention = jsonLocalizationOptions.MissingTranslationRetention;
    options.MissingTranslationLogBehavior = MissingTranslationLogBehavior.CollectToJSON;
    options.DefaultCulture = jsonLocalizationOptions.DefaultCulture;
    options.DefaultUICulture = jsonLocalizationOptions.DefaultUICulture;
    options.UseEmbeddedResources = false;
});

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
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = defaultRequestCulture,
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

app.UseAntiforgery();

app.MapStaticAssets();
app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
