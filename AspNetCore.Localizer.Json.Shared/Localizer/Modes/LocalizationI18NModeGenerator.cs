using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using AspNetCore.Localizer.Json.Format;
using AspNetCore.Localizer.Json.JsonOptions;
using AspNetCore.Localizer.Json.Localizer.Pooling;

namespace AspNetCore.Localizer.Json.Localizer.Modes
{
    internal partial class LocalizationI18NModeGenerator : LocalizationModeBase, ILocalizationModeGenerator
    {
        private static readonly Regex CultureNameRegex = new("^[a-zA-Z]{2,3}(?:-[a-zA-Z0-9]{2,8}){0,2}$", RegexOptions.Compiled);

        protected LocalizedFormat GetLocalizedValue(KeyValuePair<string, string> temp, bool isParent)
        {
            LocalizedFormat format = LocalizedFormatPool.Rent();
            format.IsParent = isParent;
            format.Value = temp.Value;
            return format;
        }

        public Dictionary<string, LocalizedFormat> ConstructLocalization(
            IEnumerable<string> resourceNames,
            CultureInfo currentCulture,
            JsonLocalizationOptions options)
        {
            var localization = new Dictionary<string, LocalizedFormat>();

            foreach (string resourceName in resourceNames)
            {
                string cultureName = GetCultureNameFromResource(resourceName, options.UseEmbeddedResources);
                if (!IsRelevantCultureFile(cultureName, currentCulture, options))
                {
                    continue;
                }

                bool isParentForFile = true;
                if (!string.IsNullOrEmpty(cultureName))
                {
                    try
                    {
                        CultureInfo fileCulture = new CultureInfo(cultureName);
                        isParentForFile = fileCulture.Name.Equals(currentCulture.Parent.Name,
                                           StringComparison.OrdinalIgnoreCase)
                                       || fileCulture.IsNeutralCulture;
                    }
                    catch
                    {
                        isParentForFile = true;
                    }
                }

                AddValueToLocalization(localization, options, resourceName, isParentForFile);
            }

            return localization;
        }

        private static bool IsRelevantCultureFile(string cultureName, CultureInfo currentCulture, JsonLocalizationOptions options)
        {
            if (string.IsNullOrEmpty(cultureName))
            {
                return true; // Neutral/invariant files are always allowed
            }

            var allowedCultures = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                currentCulture.Name,
                currentCulture.TwoLetterISOLanguageName
            };

            if (currentCulture.Parent != CultureInfo.InvariantCulture)
            {
                allowedCultures.Add(currentCulture.Parent.Name);
            }

            if (options.DefaultCulture != null)
            {
                allowedCultures.Add(options.DefaultCulture.Name);
                allowedCultures.Add(options.DefaultCulture.TwoLetterISOLanguageName);

                if (options.DefaultCulture.Parent != CultureInfo.InvariantCulture)
                {
                    allowedCultures.Add(options.DefaultCulture.Parent.Name);
                }
            }

            return allowedCultures.Contains(cultureName);
        }

        private static string GetCultureNameFromResource(string resourceName, bool useEmbeddedResources)
        {
            if (useEmbeddedResources)
            {
                // Embedded resource names are dotted (e.g. "Namespace.Resources.fr.localization.json")
                // The culture code is a segment before the file basename
                var nameWithoutExt = Path.GetFileNameWithoutExtension(resourceName) ?? string.Empty;
                var segments = nameWithoutExt.Split('.', StringSplitOptions.RemoveEmptyEntries);

                // Walk segments in reverse to find the closest culture segment
                for (int i = segments.Length - 1; i >= 0; i--)
                {
                    if (CultureNameRegex.IsMatch(segments[i]))
                    {
                        return segments[i];
                    }
                }

                return string.Empty;
            }

            // For file-system paths, check the parent directory name (e.g. "Resources/fr/localization.json")
            var directoryName = Path.GetDirectoryName(resourceName);
            if (!string.IsNullOrEmpty(directoryName))
            {
                var dirSegment = Path.GetFileName(directoryName);
                if (!string.IsNullOrEmpty(dirSegment) && CultureNameRegex.IsMatch(dirSegment))
                {
                    return dirSegment;
                }
            }

            // Check filename suffix (e.g. "localization.fr.json")
            var fileName = Path.GetFileName(resourceName);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName) ?? string.Empty;
            var segments2 = fileNameWithoutExtension.Split('.', StringSplitOptions.RemoveEmptyEntries);

            if (segments2.Length > 1 && CultureNameRegex.IsMatch(segments2[^1]))
            {
                return segments2[^1];
            }

            return string.Empty;
        }

        private void AddValueToLocalization(Dictionary<string, LocalizedFormat> localization, JsonLocalizationOptions options, string resourceName, bool isParent)
        {
            try
            {
                using Stream stream = options.UseEmbeddedResources
                    ? options.AssemblyHelper.GetAssembly().GetManifestResourceStream(resourceName)
                    ?? throw new FileNotFoundException($"La ressource incorporée '{resourceName}' est introuvable.")
                    : File.OpenRead(resourceName);

                ArraySegment<byte> jsonData = JsonStreamReader.ReadStreamToBuffer(stream, options.FileEncoding);

                JsonReaderOptions readerOptions = new JsonReaderOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip
                };

                Utf8JsonReader jsonReader = new Utf8JsonReader(jsonData, readerOptions);

                while (jsonReader.Read())
                {
                    if (jsonReader.TokenType == JsonTokenType.StartObject)
                    {
                        TraverseJson(localization, ref jsonReader, string.Empty, isParent);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Error reading embedded resource '{resourceName}'",
                    ex);
            }
        }

        private void TraverseJson(Dictionary<string, LocalizedFormat> localization, ref Utf8JsonReader jsonReader, string baseKey, bool isParent)
        {
            string currentProperty = baseKey;

            while (jsonReader.Read())
            {
                switch (jsonReader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        string propertyName = jsonReader.GetString();

                        if (string.IsNullOrEmpty(baseKey))
                        {
                            currentProperty = propertyName;
                        }
                        else
                        {
                            currentProperty = string.Concat(baseKey, ".", propertyName);
                        }
                        break;

                    case JsonTokenType.String:
                        string value = jsonReader.GetString() ?? string.Empty;
                        AddOrUpdateLocalizedValue(
                            localization,
                            GetLocalizedValue(new KeyValuePair<string, string>(currentProperty, value), isParent),
                            new KeyValuePair<string, string>(currentProperty, value)
                        );
                        break;

                    case JsonTokenType.StartObject:
                        TraverseJson(localization, ref jsonReader, currentProperty, isParent);
                        break;

                    case JsonTokenType.EndObject:
                        return;

                    case JsonTokenType.StartArray:
                        throw new ArgumentException(
                            "Le JSON i18n est invalide : les tableaux ne sont pas pris en charge.");
                }
            }
        }
    }
}
