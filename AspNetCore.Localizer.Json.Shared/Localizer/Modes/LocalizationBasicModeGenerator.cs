using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using AspNetCore.Localizer.Json.Format;
using AspNetCore.Localizer.Json.JsonOptions;

namespace AspNetCore.Localizer.Json.Localizer.Modes
{
    internal class LocalizationBasicModeGenerator : LocalizationModeBase, ILocalizationModeGenerator
    {
        public Dictionary<string, LocalizatedFormat> ConstructLocalization(
            IEnumerable<string> resourceNames, CultureInfo currentCulture, JsonLocalizationOptions options)
        {
            _options = options;

            var parentCultureName = currentCulture.Parent.Name;
            var defaultCultureName = _options.DefaultCulture?.Name;

            foreach (var resourceName in resourceNames)
            {
                try
                {
                    Dictionary<string, JsonLocalizationFormat>? tempLocalization =
                        options.UseEmbeddedResources
                            ? ReadAndDeserializeEmbeddedResource<string, JsonLocalizationFormat>(resourceName, options.FileEncoding)
                            : ReadAndDeserializeFile<string, JsonLocalizationFormat>(resourceName, options.FileEncoding);

                    if (tempLocalization == null)
                        continue;

                    foreach (var temp in tempLocalization)
                    {
                        var localizedValue = GetLocalizedValue(currentCulture, parentCultureName, defaultCultureName, temp);
                        AddOrUpdateLocalizedValue(localizedValue, temp);
                    }
                }
                catch
                {
                    if (!options.IgnoreJsonErrors)
                        throw;
                }
            }

            return localization;
        }

        private LocalizatedFormat GetLocalizedValue(
            CultureInfo currentCulture,
            string parentCultureName,
            string? defaultCultureName,
            KeyValuePair<string, JsonLocalizationFormat> temp)
        {
            var localizationFormat = temp.Value;

            if (localizationFormat.Values.TryGetValue(currentCulture.Name, out var value))
            {
                return new LocalizatedFormat
                {
                    IsParent = false,
                    Value = value
                };
            }

            if (localizationFormat.Values.TryGetValue(parentCultureName, out value))
            {
                return new LocalizatedFormat
                {
                    IsParent = true,
                    Value = value
                };
            }

            if (localizationFormat.Values.TryGetValue(string.Empty, out value))
            {
                return new LocalizatedFormat
                {
                    IsParent = true,
                    Value = value
                };
            }

            if (defaultCultureName != null && localizationFormat.Values.TryGetValue(defaultCultureName, out value))
            {
                return new LocalizatedFormat
                {
                    IsParent = true,
                    Value = value
                };
            }

            return new LocalizatedFormat
            {
                IsParent = false,
                Value = null
            };
        }

        private Dictionary<TKey, TValue>? ReadAndDeserializeEmbeddedResource<TKey, TValue>(string resourceName, Encoding encoding)
        {
            var assembly = _options.AssemblyHelper.GetAssembly();

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new FileNotFoundException($"Embedded resource '{resourceName}' not found.");
            }

            using var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: false);
            var json = reader.ReadToEnd();
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<TKey, TValue>>(json);
        }

        private static Dictionary<TKey, TValue>? ReadAndDeserializeFile<TKey, TValue>(string filePath, Encoding encoding)
        {
            var json = File.ReadAllText(filePath, encoding);
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<TKey, TValue>>(json);
        }
    }
}
