using System.Reflection;
using AspNetCore.Localizer.Json.JsonOptions;

namespace AspNetCore.Localizer.Json.Localizer.Modes
{
    internal static class LocalizationModeFactory
    {
        public static ILocalizationModeGenerator GetLocalisationFromMode(
            LocalizationMode localizationMode, Assembly assembly = null)
        {
            return localizationMode == LocalizationMode.I18n 
                ? new LocalizationI18NModeGenerator() 
                : new LocalizationBasicModeGenerator();
        }
    }
}
