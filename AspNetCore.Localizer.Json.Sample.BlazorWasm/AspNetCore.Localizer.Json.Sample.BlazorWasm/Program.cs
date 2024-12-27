using System.Globalization;
using System.Reflection;
using System.Text;
using AspNetCore.Localizer.Json.Commons;
using AspNetCore.Localizer.Json.Extensions;
using AspNetCore.Localizer.Json.JsonOptions;
using AspNetCore.Localizer.Json.Sample.BlazorWasm.Client.Pages;
using AspNetCore.Localizer.Json.Sample.BlazorWasm.Components;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

var defaultRequestCulture = new RequestCulture("en-US", "en-US");
var supportedCultures = new HashSet<CultureInfo>
{
    new CultureInfo("en-US"), new CultureInfo("fr-FR"), new CultureInfo("pt-PT")
};

builder.Services.AddLocalization();
_ = builder.Services.AddJsonLocalization(options =>
{
    options.LocalizationMode = LocalizationMode.I18n;
    options.CacheDuration = TimeSpan.FromMinutes(1);
    options.SupportedCultureInfos = supportedCultures;
    options.FileEncoding = new UTF8Encoding();
    options.DefaultCulture = new CultureInfo("fr-FR");
    options.AssemblyHelper = new AssemblyHelper(Assembly.GetExecutingAssembly());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(AspNetCore.Localizer.Json.Sample.BlazorWasm.Client._Imports).Assembly);

app.Run();