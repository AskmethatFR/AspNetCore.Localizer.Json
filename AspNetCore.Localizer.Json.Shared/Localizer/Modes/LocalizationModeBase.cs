using System.Collections.Generic;
using AspNetCore.Localizer.Json.Format;
using AspNetCore.Localizer.Json.JsonOptions;
using AspNetCore.Localizer.Json.Localizer.Pooling;

namespace AspNetCore.Localizer.Json.Localizer.Modes
{
    internal abstract class LocalizationModeBase
    {
        protected JsonLocalizationOptions _options;

        protected static void AddOrUpdateLocalizedValue<T>(Dictionary<string, LocalizedFormat> localization, LocalizedFormat localizedValue, KeyValuePair<string, T> temp)
        {
            if (localizedValue.Value is null)
            {
                LocalizedFormatPool.Return(localizedValue);
                return;
            }

            if (localization.TryGetValue(temp.Key, out var existingValue))
            {
                if (existingValue.IsParent && !localizedValue.IsParent)
                {
                    LocalizedFormatPool.Return(existingValue);
                    localization[temp.Key] = localizedValue;
                }
                else
                {
                    LocalizedFormatPool.Return(localizedValue);
                }
            }
            else
            {
                localization.Add(temp.Key, localizedValue);
            }
        }
    }
}
