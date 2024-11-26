using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AspNetCore.Localizer.Json.Format;
using AspNetCore.Localizer.Json.JsonOptions;

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
                return;
            }

            // Manually add or update to avoid using ConcurrentDictionary
            if (localization.ContainsKey(temp.Key))
            {
                if (localization[temp.Key].IsParent)
                {
                    localization[temp.Key] = localizedValue;
                }
            }
            else
            {
                localization.Add(temp.Key, localizedValue);
            }
        }
    }
}