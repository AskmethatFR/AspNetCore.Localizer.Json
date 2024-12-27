using AspNetCore.Localizer.Json.JsonOptions;
using AspNetCore.Localizer.Json.Localizer;
using Microsoft.Extensions.Options;

namespace AspNetCore.Localizer.Json.Test.Helpers
{
    internal class JsonStringLocalizerHelperFactory
    {
        public JsonStringLocalizerHelperFactory()
        {
        }

        public static JsonStringLocalizer Create(JsonLocalizationOptions options)
        {
            return new JsonStringLocalizer(Options.Create(options));
        }
        
        public static JsonStringLocalizer Create(JsonLocalizationOptions options, string baseName)
        {
            return new JsonStringLocalizer(Options.Create(options), baseName);
        }
    }
}
