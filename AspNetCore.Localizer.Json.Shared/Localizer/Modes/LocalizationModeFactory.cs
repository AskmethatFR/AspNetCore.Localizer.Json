using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Reflection;
using AspNetCore.Localizer.Json.Format;
using AspNetCore.Localizer.Json.JsonOptions;

namespace AspNetCore.Localizer.Json.Localizer.Modes
{
    internal static class LocalizationModeFactory
    {
        public static ILocalizationModeGenerator GetLocalisationFromMode(
            LocalizationMode localizationMode, Assembly assembly = null)
        {
            ILocalizationModeGenerator localizationModeGenerator = null;

            switch (localizationMode)
            {
                case LocalizationMode.BlazorWasm:
                    localizationModeGenerator = new LocalizationBlazorWasmModeGenerator(assembly);
                    break;
                case LocalizationMode.I18n:
                    localizationModeGenerator = new LocalizationI18NModeGenerator();
                    break;

                default:
                    localizationModeGenerator = new LocalizationBasicModeGenerator();
                    break;
            };

            return localizationModeGenerator;
        }
    }
}