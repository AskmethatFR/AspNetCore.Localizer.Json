using System.Globalization;
using System.Reflection;
using System.Text;
using AspNetCore.Localizer.Json.Commons;
using AspNetCore.Localizer.Json.Extensions;
using AspNetCore.Localizer.Json.JsonOptions;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using AspNetCore.Localizer.Json.StandaloneWasm;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

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


builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAdB2C", options.ProviderOptions.Authentication);
});

await builder.Build().RunAsync();
