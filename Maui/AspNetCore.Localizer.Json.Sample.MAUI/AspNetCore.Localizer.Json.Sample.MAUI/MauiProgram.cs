using Microsoft.Extensions.Logging;
using AspNetCore.Localizer.Json.Sample.MAUI.Shared.Services;
using AspNetCore.Localizer.Json.Sample.MAUI.Services;

namespace AspNetCore.Localizer.Json.Sample.MAUI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // Add device-specific services used by the AspNetCore.Localizer.Json.Sample.MAUI.Shared project
        builder.Services.AddSingleton<IFormFactor, FormFactor>();

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
