using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using AspNetCore.Localizer.Json.Format;
using AspNetCore.Localizer.Json.JsonOptions;
using AspNetCore.Localizer.Json.Localizer.Pooling;

namespace AspNetCore.Localizer.Json.Localizer.Modes
{
    internal class LocalizationBasicModeGenerator : LocalizationModeBase, ILocalizationModeGenerator
    {
        public Dictionary<string, LocalizedFormat> ConstructLocalization(
            IEnumerable<string> resourceNames, CultureInfo currentCulture, JsonLocalizationOptions options)
        {
            _options = options;
            
            var localization = new Dictionary<string, LocalizedFormat>();

            string parentCultureName = currentCulture.Parent.Name;
            string defaultCultureName = _options.DefaultCulture?.Name;

            foreach (string resourceName in resourceNames)
            {
                try
                {
                    Dictionary<string, JsonLocalizationFormat>? tempLocalization =
                        options.UseEmbeddedResources
                            ? ReadAndDeserializeEmbeddedResourceStreaming<string, JsonLocalizationFormat>(resourceName, options.FileEncoding)
                            : ReadAndDeserializeFileStreaming<string, JsonLocalizationFormat>(resourceName, options.FileEncoding);

                    if (tempLocalization == null)
                        continue;

                    foreach (KeyValuePair<string, JsonLocalizationFormat> temp in tempLocalization)
                    {
                        LocalizedFormat localizedValue = GetLocalizedValue(currentCulture, parentCultureName, defaultCultureName, temp);
                        AddOrUpdateLocalizedValue(localization, localizedValue, temp);
                    }
                }
                catch (Exception ex) when (ex is JsonException or IOException)
                {
                    if (!options.IgnoreJsonErrors)
                        throw;
                }
            }

            return localization;
        }

        private LocalizedFormat GetLocalizedValue(
            CultureInfo currentCulture,
            string parentCultureName,
            string? defaultCultureName,
            KeyValuePair<string, JsonLocalizationFormat> temp)
        {
            JsonLocalizationFormat localizationFormat = temp.Value;

            if (localizationFormat.Values.TryGetValue(currentCulture.Name, out string value))
            {
                LocalizedFormat format = LocalizedFormatPool.Rent();
                format.IsParent = false;
                format.Value = value;
                return format;
            }

            if (localizationFormat.Values.TryGetValue(parentCultureName, out value))
            {
                LocalizedFormat format = LocalizedFormatPool.Rent();
                format.IsParent = true;
                format.Value = value;
                return format;
            }

            if (localizationFormat.Values.TryGetValue(string.Empty, out value))
            {
                LocalizedFormat format = LocalizedFormatPool.Rent();
                format.IsParent = true;
                format.Value = value;
                return format;
            }

            if (defaultCultureName != null && localizationFormat.Values.TryGetValue(defaultCultureName, out value))
            {
                LocalizedFormat format = LocalizedFormatPool.Rent();
                format.IsParent = true;
                format.Value = value;
                return format;
            }

            LocalizedFormat nullFormat = LocalizedFormatPool.Rent();
            nullFormat.IsParent = false;
            nullFormat.Value = null;
            return nullFormat;
        }

        /// <summary>
        /// Reads and deserializes an embedded resource using streaming to minimize memory allocations.
        /// Tries each assembly in order; first found wins.
        /// </summary>
        private Dictionary<TKey, TValue>? ReadAndDeserializeEmbeddedResourceStreaming<TKey, TValue>(string resourceName, Encoding encoding)
        {
            foreach (var assembly in _options.AssemblyHelper.GetAssemblies())
            {
                using Stream stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    ArraySegment<byte> jsonData = JsonStreamReader.ReadStreamToBuffer(stream, encoding);
                    return JsonSerializer.Deserialize<Dictionary<TKey, TValue>>(jsonData.AsSpan());
                }
            }

            throw new FileNotFoundException($"Embedded resource '{resourceName}' not found.");
        }

        /// <summary>
        /// Reads and deserializes a file using streaming to minimize memory allocations.
        /// </summary>
        private static Dictionary<TKey, TValue>? ReadAndDeserializeFileStreaming<TKey, TValue>(string filePath, Encoding encoding)
        {
            using FileStream stream = File.OpenRead(filePath);
            ArraySegment<byte> jsonData = JsonStreamReader.ReadStreamToBuffer(stream, encoding);
            return JsonSerializer.Deserialize<Dictionary<TKey, TValue>>(jsonData.AsSpan());
        }
    }
}
