﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using AspNetCore.Localizer.Json.Format;
using AspNetCore.Localizer.Json.JsonOptions;

namespace AspNetCore.Localizer.Json.Localizer.Modes
{
    internal class LocalizationBasicModeGenerator : LocalizationModeBase, ILocalizationModeGenerator
    {
        public ConcurrentDictionary<string, LocalizatedFormat> ConstructLocalization(
            IEnumerable<string> myFiles, CultureInfo currentCulture, JsonLocalizationOptions options)
        {
            _options = options;

            foreach (string file in myFiles)
            {
                try
                {
                    var tempLocalization = LocalisationModeHelpers.ReadAndDeserializeFile<string, JsonLocalizationFormat>(file, options.FileEncoding);

                    if (tempLocalization == null)
                        continue;

                    foreach (var temp in tempLocalization)
                    {
                        var localizedValue = GetLocalizedValue(currentCulture, temp);
                        AddOrUpdateLocalizedValue(localizedValue, temp);
                    }
                }
                catch (Exception)
                {
                    if (!options.IgnoreJsonErrors)
                        throw;
                }
            }

            return localization;
        }

        private LocalizatedFormat GetLocalizedValue(CultureInfo currentCulture,
            KeyValuePair<string, JsonLocalizationFormat> temp)
        {
            var localizationFormat = temp.Value;
            bool isParent = false;
            string value = null;

            if (localizationFormat.Values.TryGetValue(currentCulture.Name, out value))
            {
                return new LocalizatedFormat()
                {
                    IsParent = false,
                    Value = value
                };
            }

            if (localizationFormat.Values.TryGetValue(currentCulture.Parent.Name, out value))
            {
                isParent = true;
            }
            else
            {
                if (localizationFormat.Values.TryGetValue(string.Empty, out value))
                {
                    isParent = true;
                }
                else if (_options.DefaultCulture != null &&
                         localizationFormat.Values.TryGetValue(_options.DefaultCulture.Name, out value))
                {
                    isParent = true;
                }
            }

            return new LocalizatedFormat()
            {
                IsParent = isParent,
                Value = value
            };
        }
    }
}
