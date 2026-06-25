

using System;
using AspNetCore.Localizer.Json.JsonOptions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace AspNetCore.Localizer.Json.Localizer
{
    /// <summary>
    /// Factory the create the JsonStringLocalizer
    /// </summary>
    internal class JsonStringLocalizerFactory : IStringLocalizerFactory
    {
        private readonly IOptions<JsonLocalizationOptions> _localizationOptions;
        private readonly ObjectPool<JsonStringLocalizer> _localizerPool;

        public JsonStringLocalizerFactory(IOptions<JsonLocalizationOptions> localizationOptions = null)
        {
            _localizationOptions = localizationOptions ?? throw new ArgumentNullException(nameof(localizationOptions));

            // Configure the pool with a default policy
            var policy = new JsonStringLocalizerPoolPolicy(_localizationOptions);
            _localizerPool = new DefaultObjectPool<JsonStringLocalizer>(policy, Environment.ProcessorCount * 2);
        }

        public IStringLocalizer Create(Type resourceSource)
        {
            return _localizerPool.Get();
        }

        public IStringLocalizer Create(string baseName, string location)
        {
            return new JsonStringLocalizer(_localizationOptions, baseName);
        }
    }

    /// <summary>
    /// Pool policy for JsonStringLocalizer
    /// </summary>
    internal class JsonStringLocalizerPoolPolicy : IPooledObjectPolicy<JsonStringLocalizer>
    {
        private readonly IOptions<JsonLocalizationOptions> _localizationOptions;

        public JsonStringLocalizerPoolPolicy(IOptions<JsonLocalizationOptions> localizationOptions)
        {
            _localizationOptions = localizationOptions;
        }

        public JsonStringLocalizer Create()
        {
            return new JsonStringLocalizer(_localizationOptions);
        }

        public bool Return(JsonStringLocalizer obj)
        {
            // Reset any state if needed before returning to the pool
            return true;
        }
    }
}
