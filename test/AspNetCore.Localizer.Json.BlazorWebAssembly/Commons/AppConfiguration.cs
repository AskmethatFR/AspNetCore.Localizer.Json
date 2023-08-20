using System.Text.Json;
using AspNetCore.Localizer.Json.JsonOptions;

namespace AspNetCore.Localizer.Json.BlazorWebAssembly.Commons
{
    public class AppConfiguration
    {
        public JsonLocalizationOptions JsonLocalizationOptions { get; set; }
    }
}