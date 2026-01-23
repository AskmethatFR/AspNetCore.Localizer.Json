using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using AspNetCore.Localizer.Json.Format;
using AspNetCore.Localizer.Json.JsonOptions;
using AspNetCore.Localizer.Json.Localizer.Pooling;

namespace AspNetCore.Localizer.Json.Localizer.Modes
{
    internal partial class LocalizationI18NModeGenerator : LocalizationModeBase, ILocalizationModeGenerator
    {
        protected LocalizatedFormat GetLocalizedValue(KeyValuePair<string, string> temp, bool isParent)
        {
            LocalizatedFormat format = LocalizatedFormatPool.Rent();
            format.IsParent = isParent;
            format.Value = temp.Value;
            return format;
        }

        public Dictionary<string, LocalizatedFormat> ConstructLocalization(
            IEnumerable<string> resourceNames,
            CultureInfo currentCulture,
            JsonLocalizationOptions options)
        {
            // Créer un nouveau dictionnaire pour éviter les données résiduelles
            localization = new Dictionary<string, LocalizatedFormat>();
            
            foreach (string resourceName in resourceNames)
            {
                string cultureName = GetCultureNameFromResource(resourceName);
                if (!string.IsNullOrEmpty(cultureName))
                {
                    bool isParent;
                    try
                    {
                        CultureInfo fileCulture = new CultureInfo(cultureName);
                        isParent = fileCulture.Name.Equals(currentCulture.Parent.Name,
                                       StringComparison.OrdinalIgnoreCase)
                                   || fileCulture.IsNeutralCulture;
                    }
                    catch
                    {
                        isParent = true;
                    }

                    AddValueToLocalization(options, resourceName, isParent);
                }
            }

            return localization;
        }

        private static string GetCultureNameFromResource(string resourceName)
        {
            string resourceFileName = Path.GetFileName(resourceName);
            ReadOnlySpan<char> resourceSpan = resourceFileName.AsSpan();
            int lastDotIndex = resourceSpan.LastIndexOf('.');
            if (lastDotIndex > 0)
            {
                int secondLastDotIndex = resourceSpan.Slice(0, lastDotIndex).LastIndexOf('.');
                if (secondLastDotIndex >= 0)
                {
                    return resourceSpan.Slice(secondLastDotIndex + 1, lastDotIndex - secondLastDotIndex - 1).ToString();
                }
            }

            return string.Empty;
        }

        private void AddValueToLocalization(JsonLocalizationOptions options, string resourceName, bool isParent)
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
                        TraverseJson(ref jsonReader, string.Empty, isParent);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Erreur lors de la lecture de la ressource incorporée '{resourceName}'",
                    ex);
            }
        }

        private void TraverseJson(ref Utf8JsonReader jsonReader, string baseKey, bool isParent)
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
                            GetLocalizedValue(new KeyValuePair<string, string>(currentProperty, value), isParent),
                            new KeyValuePair<string, string>(currentProperty, value)
                        );
                        break;

                    case JsonTokenType.StartObject:
                        TraverseJson(ref jsonReader, currentProperty, isParent);
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
