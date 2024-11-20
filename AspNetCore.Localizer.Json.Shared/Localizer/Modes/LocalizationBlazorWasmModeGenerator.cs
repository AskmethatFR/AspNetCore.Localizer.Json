using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using AspNetCore.Localizer.Json.Format;
using AspNetCore.Localizer.Json.JsonOptions;
using AspNetCore.Localizer.Json.Localizer.Modes;

internal class LocalizationBlazorWasmModeGenerator : LocalizationI18NModeGenerator
{
    private readonly Assembly resourceAssembly;

    public LocalizationBlazorWasmModeGenerator(Assembly resourceAssembly)
    {
        this.resourceAssembly = resourceAssembly;
    }

    public new ConcurrentDictionary<string, LocalizatedFormat> ConstructLocalization(IEnumerable<string> myFiles,
        CultureInfo currentCulture,
        JsonLocalizationOptions options)
    {
        _options = options;

        var filesList = myFiles.ToList();
        bool isInvariantCulture = currentCulture.Equals(CultureInfo.InvariantCulture);

        if (isInvariantCulture)
        {
            // If the culture is invariant, directly check for neutral files
            var neutralFiles = filesList
                .Where(file => Path.GetFileName(file).Count(s => s == '.') == 1)
                .ToList();

            if (neutralFiles.Any())
            {
                foreach (var neutralFile in neutralFiles)
                {
                    AddValueToLocalization(options, neutralFile, true);
                }
            }

            return localization;
        }

        // Handle culture-specific files
        var cultureSpecificFiles = filesList
            .Where(file => IsCultureFileForCurrentOrParent(file, currentCulture))
            .ToList();

        if (cultureSpecificFiles.Any())
        {
            foreach (var file in cultureSpecificFiles)
            {
                string fileName = Path.GetFileName(file);
                string cultureName = GetCultureNameFromFile(fileName);
                if (!string.IsNullOrEmpty(cultureName))
                {
                    var fileCulture = new CultureInfo(cultureName);
                    bool isParent = fileCulture.Name.Equals(currentCulture.Parent.Name, StringComparison.OrdinalIgnoreCase);

                    if (fileCulture.Name.Equals(currentCulture.Name, StringComparison.OrdinalIgnoreCase) ||
                        (isParent && fileCulture.Name != "json"))
                    {
                        AddValueToLocalization(options, file, isParent);
                    }
                }
            }
        }

        return localization;
    }

    private static string GetCultureNameFromFile(string fileName)
    {
        // Optimized to avoid unnecessary allocations
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

    private static bool IsCultureFileForCurrentOrParent(string file, CultureInfo currentCulture)
    {
        // Method to check if the file is related to the current culture or its parent
        string fileName = Path.GetFileName(file);
        return fileName.Contains(currentCulture.Name, StringComparison.OrdinalIgnoreCase) ||
               fileName.Contains(currentCulture.Parent.Name, StringComparison.OrdinalIgnoreCase);
    }
}
