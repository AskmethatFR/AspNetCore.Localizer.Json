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
        protected ConcurrentDictionary<string, LocalizatedFormat> localization =
            new ConcurrentDictionary<string, LocalizatedFormat>();

        protected JsonLocalizationOptions _options;

        protected void AddOrUpdateLocalizedValue<T>(LocalizatedFormat localizedValue, KeyValuePair<string, T> temp)
        {
            // Skip if the localized value is null
            if (localizedValue.Value is null)
            {
                return;
            }

            // Use GetOrAdd to avoid multiple accesses
            localization.AddOrUpdate(
                temp.Key,
                localizedValue,
                (key, existingValue) => existingValue.IsParent ? localizedValue : existingValue
            );
        }
    }
}