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
            IEnumerable<string> resourceNames,
            CultureInfo currentCulture,
            JsonLocalizationOptions options)
        {
            foreach (var resourceName in resourceNames)
            {
                string cultureName = GetCultureNameFromResource(resourceName);
                if (!string.IsNullOrEmpty(cultureName))
                {
                    bool isParent;
                    try
                    {
                        var fileCulture = new CultureInfo(cultureName);
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
            ReadOnlySpan<char> resourceSpan = resourceName.AsSpan();
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


        private static readonly JsonDocumentOptions JsonOptions = new JsonDocumentOptions()
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        private void AddValueToLocalization(JsonLocalizationOptions options, string resourceName, bool isParent)
        {
            try
            {
                using Stream stream = options.UseEmbeddedResources
                    ? options.AssemblyHelper.GetAssembly().GetManifestResourceStream(resourceName)
                    ?? throw new FileNotFoundException($"La ressource incorporée '{resourceName}' est introuvable.")
                    : File.OpenRead(resourceName);

                if (stream.CanSeek)
                {
                    Span<byte> bom = stackalloc byte[3];
                    stream.ReadExactly(bom);
                    if (!bom.SequenceEqual(new byte[] { 0xEF, 0xBB, 0xBF }))
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                    }
                }

                byte[] buffer = new byte[8192]; 
                int bytesRead;

                var readerOptions = new JsonReaderOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip
                };

                using MemoryStream memoryStream = new MemoryStream();

                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    memoryStream.Write(buffer, 0, bytesRead);
                }

                ReadOnlySpan<byte> jsonData = new ReadOnlySpan<byte>(memoryStream.ToArray());

                var jsonReader = new Utf8JsonReader(jsonData, readerOptions);

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
                        currentProperty = string.IsNullOrEmpty(baseKey)
                            ? jsonReader.GetString()
                            : $"{baseKey}.{jsonReader.GetString()}";
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