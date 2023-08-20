using AspNetCore.Localizer.Json.Extensions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System;
using AspNetCore.Localizer.Json.JsonOptions;

namespace AspNetCore.Localizer.Json.Localizer
{
    /// <summary>
    /// Factory the create the JsonStringLocalizer
    /// </summary>
    public class JsonStringLocalizerFactory : IStringLocalizerFactory
    {
        private readonly IOptions<JsonLocalizationOptions> _localizationOptions;

            private readonly EnvironmentWrapper _env;
        
         public JsonStringLocalizerFactory(
            EnvironmentWrapper env,
            IOptions<JsonLocalizationOptions> localizationOptions = null)
        {
            _env = env;
            _localizationOptions = localizationOptions ?? throw new ArgumentNullException(nameof(localizationOptions));
        }


         public IStringLocalizer Create(Type resourceSource)
        {
            return new JsonStringLocalizer(_localizationOptions, _env);
        }

        public IStringLocalizer Create(string baseName, string location)
        {
            baseName = _localizationOptions.Value.UseBaseName ? baseName : string.Empty;
            return new JsonStringLocalizer(_localizationOptions, _env, baseName);
        }
    }
}