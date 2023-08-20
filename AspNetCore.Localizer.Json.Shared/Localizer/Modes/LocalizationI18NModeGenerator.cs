using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using AspNetCore.Localizer.Json.Format;
using AspNetCore.Localizer.Json.JsonOptions;

namespace AspNetCore.Localizer.Json.Localizer.Modes
{
    internal partial class LocalizationI18NModeGenerator : LocalizationModeBase, ILocalizationModeGenerator
    {
        private LocalizatedFormat GetLocalizedValue(KeyValuePair<string, string> temp, bool isParent)
        {
            return new LocalizatedFormat()
            {
                IsParent = isParent,
                Value = temp.Value as string
            };
        }

        public ConcurrentDictionary<string, LocalizatedFormat> ConstructLocalization(IEnumerable<string> myFiles,
            CultureInfo currentCulture,
            JsonLocalizationOptions options)
        {
            _options = options;

            string[] enumerable = myFiles as string[] ?? myFiles.ToArray();
            List<string> neutralFiles = enumerable.Where(file => Path.GetFileName(file)
                .Count(s => s.CompareTo('.') == 0) == 1).ToList();
            bool isInvariantCulture =
                currentCulture.DisplayName == CultureInfo.InvariantCulture.ThreeLetterISOLanguageName;

            string[] files = isInvariantCulture
                ? new string[] { }
                : enumerable.Where(file => Path.GetFileName(file).Split('.').Any(
                    s => (s.IndexOf(currentCulture.Name, StringComparison.OrdinalIgnoreCase) >= 0
                          || s.IndexOf(currentCulture.Parent.Name, StringComparison.OrdinalIgnoreCase) >= 0)
                )).ToArray();


            if (files.Any() && !isInvariantCulture)
            {
                foreach (string file in files)
                {
                    string fileName = Path.GetFileName(file);
                    string cultureName = GetCultureNameFromFile(fileName);
                    CultureInfo fileCulture = new CultureInfo(cultureName);

                    bool isParent =
                        fileCulture.Name.Equals(currentCulture.Parent.Name, StringComparison.OrdinalIgnoreCase);

                    if (fileCulture.Name.Equals(currentCulture.Name, StringComparison.OrdinalIgnoreCase) ||
                        isParent && fileCulture.Name != "json")
                    {
                        AddValueToLocalization(options, file, isParent);
                    }
                }
            }
            else
            {
                if (neutralFiles.Any())
                {
                    foreach (string neutralFile in neutralFiles)
                        AddValueToLocalization(options, neutralFile, true);
                }
            }

            return localization;
        }

        private static string GetCultureNameFromFile(string fileName)
        {
            string[] split = fileName.Split('.');
            if (split.Length > 2)
            {
                return split[^2];
            }

            return string.Empty;
        }

        private static readonly JsonDocumentOptions Options = new JsonDocumentOptions() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

        internal void AddValueToLocalization(JsonLocalizationOptions options, string file, bool isParent)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(ReadFile(options, file), Options);

                AddValues(doc.RootElement, null, isParent);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Error reading file '{file}'",ex);
            }

        }

        internal void AddValues(JsonElement element, string baseName, bool isParent)
        {
            // Json Object could either contain an array or an object or just values
            // For the field names, navigate to the root or the first element
            JsonElement input = element;


            // check if the object is of type JObject. 
            // If yes, read the properties of that JObject
            if (input.ValueKind == JsonValueKind.Object)
            {
                // Read Properties
                JsonElement.ObjectEnumerator properties = input.EnumerateObject();

                // Loop through all the properties of that JObject
                foreach (JsonProperty property in properties)
                {
                    // Check if there are any sub-fields (nested)
                    if (property.Value.ValueKind == JsonValueKind.Object)
                    {
                        // If yes, enter the recursive loop to extract sub-field names
                        string newBaseName = String.IsNullOrEmpty(baseName)
                            ? property.Name
                            : String.Format("{0}.{1}", baseName, property.Name);
                        AddValues(property.Value, newBaseName, isParent);
                    }
                    else if (property.Value.ValueKind == JsonValueKind.Array)
                    {
                        throw new ArgumentException("Invalid i18n Json");
                    }
                    else
                    {
                        // If there are no sub-fields, the property name is the field name
                        KeyValuePair<string, string> temp = new KeyValuePair<string, string>(
                            String.IsNullOrEmpty(baseName)
                                ? property.Name
                                : $"{baseName}.{property.Name}",
                            property.Value.ToString());

                        LocalizatedFormat localizedValue = GetLocalizedValue(temp, isParent);
                        AddOrUpdateLocalizedValue(
                            localizedValue,
                            temp
                        );
                    }
                }

            }
        }
    }
}