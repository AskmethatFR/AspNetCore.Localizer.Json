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

        public static JsonStringLocalizer Create(JsonLocalizationOptions options, string baseName = null)
        {
            return new JsonStringLocalizer(Options.Create(options), new EnvironmentWrapper(new HostingEnvironmentStub()), baseName);
        }
    }
}
