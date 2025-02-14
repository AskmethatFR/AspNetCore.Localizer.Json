using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AspNetCore.Localizer.Json.Commons;
using AspNetCore.Localizer.Json.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AspNetCore.Localizer.Json.Sample.I18nTest.Data;
using AspNetCore.Localizer.Json.JsonOptions;
using Microsoft.AspNetCore.Localization;

namespace AspNetCore.Localizer.Json.Sample.I18nTest
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        JsonLocalizationOptions _jsonLocalizationOptions;
        List<CultureInfo> _supportedCultures;
        RequestCulture _defaultRequestCulture;
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages()
                .AddDataAnnotationsLocalization()
                .AddViewLocalization();
            services.AddControllers();
            services.AddServerSideBlazor();
            services.AddScoped<WeatherForecastService>();
            
            var jsonLocalizationOptions = Configuration.GetSection(nameof(JsonLocalizationOptions));

            _jsonLocalizationOptions = jsonLocalizationOptions.Get<JsonLocalizationOptions>();
            _defaultRequestCulture = new RequestCulture(_jsonLocalizationOptions.DefaultCulture,
                _jsonLocalizationOptions.DefaultUICulture);
            _supportedCultures = _jsonLocalizationOptions.SupportedCultureInfos.ToList();

            services.AddJsonLocalization(options =>
            {
                options.ResourcesPath = _jsonLocalizationOptions.ResourcesPath;
                options.AdditionalResourcesPaths = _jsonLocalizationOptions.AdditionalResourcesPaths;
                options.CacheDuration = _jsonLocalizationOptions.CacheDuration;
                options.SupportedCultureInfos = _jsonLocalizationOptions.SupportedCultureInfos;
                options.FileEncoding = _jsonLocalizationOptions.FileEncoding;
                options.MissingTranslationLogBehavior = MissingTranslationLogBehavior.CollectToJSON;
                options.LocalizationMode = LocalizationMode.I18n;
                options.LocalizerDiagnosticMode = _jsonLocalizationOptions.LocalizerDiagnosticMode;
                options.AssemblyHelper = new AssemblyHelper(Assembly.GetExecutingAssembly());
            });
            
            services.Configure<RequestLocalizationOptions>(options =>
            {
                options.DefaultRequestCulture = _defaultRequestCulture;
                // Formatting numbers, dates, etc.
                options.SupportedCultures = _supportedCultures;
                // UI strings that we have localized.
                options.SupportedUICultures = _supportedCultures;
            });
            
            services.AddHttpContextAccessor();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            
                        
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            
            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = _defaultRequestCulture,
                // Formatting numbers, dates, etc.
                SupportedCultures = _supportedCultures,
                // UI strings that we have localized.
                SupportedUICultures = _supportedCultures
            });
            
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapControllers();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}