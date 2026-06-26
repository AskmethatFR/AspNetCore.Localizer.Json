using System.Collections.Generic;
using AspNetCore.Localizer.Json.Format;
using AspNetCore.Localizer.Json.JsonOptions;
using AspNetCore.Localizer.Json.Localizer.Pooling;

namespace AspNetCore.Localizer.Json.Localizer.Modes
{
    internal abstract class LocalizationModeBase
    {
        protected JsonLocalizationOptions _options;

        protected static void AddOrUpdateLocalizedValue<T>(Dictionary<string, LocalizatedFormat> localization, LocalizatedFormat localizedValue, KeyValuePair<string, T> temp)
        {
            if (localizedValue.Value is null)
            {
                LocalizatedFormatPool.Return(localizedValue);
                return;
            }

            if (localization.TryGetValue(temp.Key, out var existingValue))
            {
                if (existingValue.IsParent && !localizedValue.IsParent)
                {
                    LocalizatedFormatPool.Return(existingValue);
                    localization[temp.Key] = localizedValue;
                }
                else
                {
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
