using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using AspNetCore.Localizer.Json.Format;
using AspNetCore.Localizer.Json.JsonOptions;

namespace AspNetCore.Localizer.Json.Localizer.Modes
{
    internal interface ILocalizationModeGenerator
    {
        ConcurrentDictionary<string, LocalizatedFormat> ConstructLocalization(
            IEnumerable<string> myFiles, CultureInfo currentCulture, JsonLocalizationOptions options);

    }
}