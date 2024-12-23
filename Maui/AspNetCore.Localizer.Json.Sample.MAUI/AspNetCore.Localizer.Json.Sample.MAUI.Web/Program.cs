using System.Globalization;
using System.Text;
using AspNetCore.Localizer.Json.Extensions;
using AspNetCore.Localizer.Json.JsonOptions;
using AspNetCore.Localizer.Json.Sample.MAUI.Web.Components;
using AspNetCore.Localizer.Json.Sample.MAUI.Shared.Services;
using AspNetCore.Localizer.Json.Sample.MAUI.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// Add device-specific services used by the AspNetCore.Localizer.Json.Sample.MAUI.Shared project
builder.Services.AddSingleton<IFormFactor, FormFactor>();

builder.Services.AddLocalization();
builder.Services.AddJsonLocalization(options => {
    options.CacheDuration = TimeSpan.FromMinutes(15);
    options.ResourcesPath = "Resources";
    options.LocalizationMode = LocalizationMode.I18n;
    options.FileEncoding = Encoding.GetEncoding("ISO-8859-1");
    options.SupportedCultureInfos = new HashSet<CultureInfo>()
    {
        new CultureInfo("fr-FR")
    };
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

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(
        typeof(AspNetCore.Localizer.Json.Sample.MAUI.Shared._Imports).Assembly,
        typeof(AspNetCore.Localizer.Json.Sample.MAUI.Web.Client._Imports).Assembly);

app.Run();
