using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using AspNetCore.Localizer.Json.Format;
using AspNetCore.Localizer.Json.JsonOptions;

namespace AspNetCore.Localizer.Json.Localizer.Modes
{
    internal partial class LocalizationI18NModeGenerator : LocalizationModeBase, ILocalizationModeGenerator
    {
        protected LocalizatedFormat GetLocalizedValue(KeyValuePair<string, string> temp, bool isParent)
        {
            return new LocalizatedFormat()
            {
                IsParent = isParent,
                Value = temp.Value
            };
        }

        public Dictionary<string, LocalizatedFormat> ConstructLocalization(
            IEnumerable<string> myFiles,
            CultureInfo currentCulture,
            JsonLocalizationOptions options)
        {
            var filesList = myFiles.ToList();
            bool isInvariantCulture =
                currentCulture.Name.Equals(CultureInfo.InvariantCulture.Name, StringComparison.OrdinalIgnoreCase);

            // Collect culture-specific and neutral files separately
            var cultureSpecificFiles = filesList.Where(file =>
            {
                var fileName = Path.GetFileName(file);
                return fileName.Contains(currentCulture.Name, StringComparison.OrdinalIgnoreCase) ||
                       (!isInvariantCulture &&
                        fileName.Contains(currentCulture.Parent.Name, StringComparison.OrdinalIgnoreCase));
            }).ToList();

            // If culture-specific files are found, skip loading neutral files
            if (cultureSpecificFiles.Any())
            {
                foreach (string file in cultureSpecificFiles)
                {
                    string cultureName = GetCultureNameFromFile(Path.GetFileName(file));
                    if (!string.IsNullOrEmpty(cultureName))
                    {
                        var fileCulture = new CultureInfo(cultureName);
                        bool isParent = fileCulture.Name.Equals(currentCulture.Parent.Name,
                            StringComparison.OrdinalIgnoreCase);

                        if (fileCulture.Name.Equals(currentCulture.Name, StringComparison.OrdinalIgnoreCase) ||
                            (isParent && fileCulture.Name != "json"))
                        {
                            AddValueToLocalization(options, file, isParent);
                        }
                    }
                }
            }
            else
            {
                // If no culture-specific files, load the neutral files
                var neutralFiles = filesList.Where(file => Path.GetFileName(file).Count(s => s == '.') == 1);
                foreach (string neutralFile in neutralFiles)
                {
                    AddValueToLocalization(options, neutralFile, true);
                }
            }

            return localization;
        }


        private static string GetCultureNameFromFile(string fileName)
        {
            // Optimized with ReadOnlySpan to avoid allocation
            ReadOnlySpan<char> fileNameSpan = fileName.AsSpan();
            int lastDotIndex = fileNameSpan.LastIndexOf('.');
            if (lastDotIndex > 0)
            {
                int secondLastDotIndex = fileNameSpan.Slice(0, lastDotIndex).LastIndexOf('.');
                if (secondLastDotIndex >= 0)
                {
                    return fileNameSpan.Slice(secondLastDotIndex + 1, lastDotIndex - secondLastDotIndex - 1).ToString();
                }
            }

            return string.Empty;
        }

        private static readonly JsonDocumentOptions Options = new JsonDocumentOptions()
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        internal void AddValueToLocalization(JsonLocalizationOptions options, string file, bool isParent)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(ReadFile(options, file), Options);
                AddValues(doc.RootElement, null, isParent);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Error reading file '{file}'", ex);
            }
        }

        private void AddValues(JsonElement element, string baseName, bool isParent)
        {
            Stack<(JsonElement element, string baseName)> stack = new Stack<(JsonElement, string)>();
            stack.Push((element, baseName));

            while (stack.Count > 0)
            {
                var (currentElement, currentBaseName) = stack.Pop();

                if (currentElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (JsonProperty property in currentElement.EnumerateObject())
                    {
                        var newBaseName = new StringBuilder(currentBaseName);
                        if (!string.IsNullOrEmpty(currentBaseName))
                        {
                            newBaseName.Append('.').Append(property.Name);
                        }
                        else
                        {
                            newBaseName.Append(property.Name);
                        }

                        if (property.Value.ValueKind == JsonValueKind.Object)
                        {
                            stack.Push((property.Value, newBaseName.ToString()));
                        }
                        else if (property.Value.ValueKind == JsonValueKind.Array)
                        {
                            throw new ArgumentException("Invalid i18n Json");
                        }
                        else
                        {
                            string key = newBaseName.ToString();
                            var localizedValue = GetLocalizedValue(new KeyValuePair<string, string>(key, property.Value.GetString()), isParent);
                            AddOrUpdateLocalizedValue(localizedValue, new KeyValuePair<string, string>(key, property.Value.ToString()));
                        }
                    }
                }
            }
        }
    }
}