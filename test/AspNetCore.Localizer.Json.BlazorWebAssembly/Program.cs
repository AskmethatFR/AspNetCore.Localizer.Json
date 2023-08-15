using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using AspNetCore.Localizer.Json.BlazorWebAssembly.Commons;
using AspNetCore.Localizer.Json.Extensions;
using AspNetCore.Localizer.Json.JsonOptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace AspNetCore.Localizer.Json.Sample.BlazorWebAssembly
{
    public class Program
    {
        private static WebAssemblyHostBuilder _builder;
        private static WebAssemblyHost _host;

        private static HashSet<CultureInfo> _supportedCultures;
        private static RequestCulture _defaultRequestCulture;

        public static async Task Main(string[] args)
        {
            _builder = WebAssemblyHostBuilder.CreateDefault(args);
            _builder.RootComponents.Add<App>("#app");
            _builder.Services.AddScoped(sp => new HttpClient
                { BaseAddress = new Uri(_builder.HostEnvironment.BaseAddress) });

            ConfigureServices(_builder.Services);
            await ConfigureApplication(_builder.Services);

            await _host.RunAsync();
        }

        private static async Task ConfigureApplication(IServiceCollection services)
        {
            _host = _builder.Build();
            
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped(
                sp => new HttpClient { BaseAddress = new Uri(_builder.HostEnvironment.BaseAddress) });
            // Example of loading a configuration as configuration isn't available yet at this stage.
            services.AddSingleton(provider =>
            {
                var config = provider.GetService<IConfiguration>();
                return config.GetSection("App").Get<AppConfiguration>();
            });

            _defaultRequestCulture = new RequestCulture("en-US", "en-US");
            _supportedCultures = new HashSet<CultureInfo>
            {
                new CultureInfo("en-US"), new CultureInfo("fr-FR"), new CultureInfo("pt-PT")
            };

            services.AddLocalization();
            _ = services.AddJsonLocalization(options =>
            {
                options.LocalizationMode = LocalizationMode.BlazorWasm;
                options.UseBaseName = false;
                options.CacheDuration = TimeSpan.FromMinutes(1);
                options.SupportedCultureInfos = _supportedCultures;
                options.FileEncoding = new UTF8Encoding();
                options.IsAbsolutePath = true;
                options.Assembly = typeof(Program).Assembly;
                options.DefaultCulture = new CultureInfo("fr-FR");
                options.JsonFileList = new[]
                {
                    "AspNetCore.Localizer.Json.Sample.BlazorWebAssembly/Resources/localization.json",
                    "AspNetCore.Localizer.Json.Sample.BlazorWebAssembly/I18n/localization.pt.json",
                    "AspNetCore.Localizer.Json.Sample.BlazorWebAssembly/Resources/fr/localization.json"
                };
            });

            _ = services.Configure<RequestLocalizationOptions>(options =>
            {
                options.DefaultRequestCulture = _defaultRequestCulture;
                // Formatting numbers, dates, etc.
                options.SupportedCultures = _supportedCultures.ToList();
                // UI strings that we have localized.
                options.SupportedUICultures = _supportedCultures.ToList();
            });
        }
    }
}