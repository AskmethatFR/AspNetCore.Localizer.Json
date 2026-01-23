using System.Collections.Generic;
using AspNetCore.Localizer.Json.Format;
using AspNetCore.Localizer.Json.JsonOptions;
using AspNetCore.Localizer.Json.Localizer.Pooling;

namespace AspNetCore.Localizer.Json.Localizer.Modes
{
    internal abstract class LocalizationModeBase
    {
        protected Dictionary<string, LocalizatedFormat> localization =
            new Dictionary<string, LocalizatedFormat>();

        protected JsonLocalizationOptions _options;

        protected void AddOrUpdateLocalizedValue<T>(LocalizatedFormat localizedValue, KeyValuePair<string, T> temp)
        {
            // Skip if the localized value is null
            if (localizedValue.Value is null)
            {
                LocalizatedFormatPool.Return(localizedValue);
                return;
            }

            // Optimize dictionary access to minimize redundant lookups
            if (localization.TryGetValue(temp.Key, out var existingValue))
            {
                if (existingValue.IsParent && !localizedValue.IsParent)
                {
                    // Return the old value to the pool before replacing
                    LocalizatedFormatPool.Return(existingValue);
                    localization[temp.Key] = localizedValue;
                }
                else
                {
                    // Return the new value to the pool if not used
                    LocalizatedFormatPool.Return(localizedValue);
                }
            }
            else
            {
                localization.Add(temp.Key, localizedValue);
            }
        }
    }
}
