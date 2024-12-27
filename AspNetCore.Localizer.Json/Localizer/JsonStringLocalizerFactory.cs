

using System;
using AspNetCore.Localizer.Json.JsonOptions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace AspNetCore.Localizer.Json.Localizer
{
    /// <summary>
    /// Factory the create the JsonStringLocalizer
    /// </summary>
    public class JsonStringLocalizerFactory : IStringLocalizerFactory
    {
        private readonly IOptions<JsonLocalizationOptions> _localizationOptions;

        public JsonStringLocalizerFactory(IOptions<JsonLocalizationOptions> localizationOptions = null)
        {
            _localizationOptions = localizationOptions ?? throw new ArgumentNullException(nameof(localizationOptions));
        }


         public IStringLocalizer Create(Type resourceSource)
        {
            return new JsonStringLocalizer(_localizationOptions);
        }

        public IStringLocalizer Create(string baseName, string location)
        {
            return new JsonStringLocalizer(_localizationOptions);
        }
    }
}