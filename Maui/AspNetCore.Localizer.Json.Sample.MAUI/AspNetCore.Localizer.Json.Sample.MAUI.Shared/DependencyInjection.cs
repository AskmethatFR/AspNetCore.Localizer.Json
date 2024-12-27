using System.Globalization;
using System.Reflection;
using AspNetCore.Localizer.Json.Commons;
using AspNetCore.Localizer.Json.Extensions;
using AspNetCore.Localizer.Json.JsonOptions;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCore.Localizer.Json.Sample.MAUI.Shared;

public static class DependencyInjection
{
    public static IServiceCollection AddShared(this IServiceCollection services)
    {
        
        services.AddJsonLocalization(options => {
            options.CacheDuration = TimeSpan.FromMinutes(15);
            options.ResourcesPath = "Resources";
            options.LocalizationMode = LocalizationMode.I18n;
            options.SupportedCultureInfos = [new CultureInfo("fr-FR")];
            options.AssemblyHelper = new AssemblyHelper(Assembly.Load("AspNetCore.Localizer.Json.Sample.MAUI.Shared"));
        });

        return services;
    }
}