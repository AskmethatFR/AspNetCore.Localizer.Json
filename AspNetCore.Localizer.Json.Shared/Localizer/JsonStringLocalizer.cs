#nullable enable
using AspNetCore.Localizer.Json.Extensions;
using AspNetCore.Localizer.Json.Format;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AspNetCore.Localizer.Json.JsonOptions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;

namespace AspNetCore.Localizer.Json.Localizer
{
    internal partial class JsonStringLocalizer : JsonStringLocalizerBase, IJsonStringLocalizer
    {
        private readonly string? _missingTranslationsFile = null;
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _missingTranslations = new();

        public JsonStringLocalizer(IOptions<JsonLocalizationOptions> localizationOptions) : base(localizationOptions)
        {
            _missingTranslationsFile = localizationOptions.Value.MissingTranslationsOutputFile;
        }

        public JsonStringLocalizer(IOptions<JsonLocalizationOptions> localizationOptions, string baseName) : base(
            localizationOptions, baseName)
        {
            _missingTranslationsFile = localizationOptions.Value.MissingTranslationsOutputFile;
        }

        public void Reset()
        {
            // Reset any state that needs to be cleared when returning to the pool
            _missingTranslations.Clear();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _missingTranslations.Clear();
            }

            base.Dispose(disposing);
        }

        private static LocalizedString ConvertToChar(string value, char c, int additionalRepeats = 0) =>
            new LocalizedString(value, new string(c, value.Length + additionalRepeats));

        public LocalizedString this[string name] => GetLocalizedString(name);

        public LocalizedString this[string name, params object[] arguments] => GetLocalizedString(name, arguments);

        private LocalizedString GetLocalizedString(string name, params object[] arguments)
        {
            if (LocalizationOptions.Value.LocalizerDiagnosticMode)
            {
                return ConvertToChar(name, 'X');
            }

            string? value = GetString(name);
            bool resourceNotFound = value == null;

            value ??= name; // Utilise le nom comme valeur par défaut si aucune valeur n'est trouvée

            value = FormatString(value, arguments);

            return new LocalizedString(name, value, resourceNotFound);
        }


        private string FormatString(string value, object[]? arguments)
        {
            if (arguments is { Length: > 0 })
            {
                value = string.Format(value, arguments);

                if (arguments[^1] is bool isPlural)
                {
                    if (!string.IsNullOrEmpty(value) && value.Contains(LocalizationOptions.Value.PluralSeparator))
                    {
                        var parts = value.Split(new[] { LocalizationOptions.Value.PluralSeparator }, 2,
                            StringSplitOptions.None);
                        return parts.Length > 1 ? parts[isPlural ? 1 : 0] : value;
                    }
                }
            }

            return value;
        }

        public LocalizedString GetPlural(string name, double count, params object[] arguments)
        {
            InitCorrectJsonCulture();

            IPluralizationRuleSet pluralizationRuleSet = GetPluralizationToUse();
            string applicableRule = pluralizationRuleSet.GetMatchingPluralizationRule(count);

            string nameWithRule = $"{name}.{applicableRule}";

            if (_localizationCache.TryGetValue(_currentCulture, out var dict))
            {
                if (dict.TryGetValue(nameWithRule, out var localizedValue))
                {
                    return FormatLocalizedString(name, localizedValue.Value, count, arguments);
                }

                string nameWithOtherRule = $"{name}.{PluralizationConstants.Other}";
                if (dict.TryGetValue(nameWithOtherRule, out var localizedOtherValue))
                {
                    return FormatLocalizedString(name, localizedOtherValue.Value, count, arguments);
                }
            }

            string fallback = GetString(name, true);
            return FormatLocalizedString(name, fallback ?? name, count, arguments, fallback == null);
        }


        private LocalizedString FormatLocalizedString(string name, string format, double count, object[] arguments,
            bool resourceNotFound = false)
        {
            // unshift the count to the arguments array
            object[] argumentsWithCount = new object[arguments.Length + 1];
            argumentsWithCount[0] = count;

            // copy the rest of the arguments
            Array.Copy(arguments, 0, argumentsWithCount, 1, arguments.Length);

            // format the string
            string value = string.Format(format, argumentsWithCount);
            return new LocalizedString(name, value, resourceNotFound);
        }

        private CultureInfo InitCorrectJsonCulture(bool shouldTryDefaultCulture = false)
        {
            // Initialize the culture if needed
            var culture = CultureInfo.CurrentUICulture ?? CultureInfo.DefaultThreadCurrentUICulture ?? CultureInfo.InvariantCulture;
            if (!IsUiCultureCurrentCulture(culture))
            {
                InitJsonFromCulture(culture);
            }

            return culture;
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            InitCorrectJsonCulture();

            if (!_localizationCache.TryGetValue(_currentCulture, out var dict))
            {
                return Enumerable.Empty<LocalizedString>();
            }

            return dict.Where(w => includeParentCultures || !w.Value.IsParent)
                .Select(l =>
                    new LocalizedString(l.Key, l.Value.Value ?? l.Key, resourceNotFound: l.Value.Value == null))
                .OrderBy(s => s.Name);
        }

        public IStringLocalizer WithCulture(CultureInfo culture)
        {
            var clonedOptions = new JsonLocalizationOptions
            {
                ResourcesPath = LocalizationOptions.Value.ResourcesPath,
                CacheDuration = LocalizationOptions.Value.CacheDuration,
                Caching = LocalizationOptions.Value.Caching,
                DistributedCache = LocalizationOptions.Value.DistributedCache,
                DefaultCulture = LocalizationOptions.Value.DefaultCulture,
                DefaultUICulture = LocalizationOptions.Value.DefaultUICulture,
                SupportedCultureInfos = new HashSet<CultureInfo>(LocalizationOptions.Value.SupportedCultureInfos)
                {
                    culture
                },
                FileEncodingName = LocalizationOptions.Value.FileEncodingName,
                PluralSeparator = LocalizationOptions.Value.PluralSeparator,
                MissingTranslationLogBehavior = LocalizationOptions.Value.MissingTranslationLogBehavior,
                LocalizationMode = LocalizationOptions.Value.LocalizationMode,
                MissingTranslationsOutputFile = LocalizationOptions.Value.MissingTranslationsOutputFile,
                LocalizerDiagnosticMode = LocalizationOptions.Value.LocalizerDiagnosticMode,
                IgnoreJsonErrors = LocalizationOptions.Value.IgnoreJsonErrors,
                AssemblyHelper = LocalizationOptions.Value.AssemblyHelper,
                AdditionalResourcesPaths = LocalizationOptions.Value.AdditionalResourcesPaths,
                UseEmbeddedResources = LocalizationOptions.Value.UseEmbeddedResources,
                CacheMaxSize = LocalizationOptions.Value.CacheMaxSize,
                MaxMissingTranslations = LocalizationOptions.Value.MaxMissingTranslations,
                MissingTranslationRetention = LocalizationOptions.Value.MissingTranslationRetention
            };

            return new JsonStringLocalizer(Options.Create(clonedOptions));
        }

        private string? GetString(string name, bool shouldTryDefaultCulture = true)
        {
            if (string.IsNullOrEmpty(name))
            {
                Console.Error.WriteLine(
                $"You are trying to locate an empty string, please verify");
                return string.Empty;
            }

            // Initialize culture BEFORE lookup to ensure culture-aware access
            var culture = InitCorrectJsonCulture(true);

            if (_localizationCache.TryGetValue(_currentCulture, out var dict) && dict.TryGetValue(name, out LocalizedFormat? localizedValue))
            {
                return localizedValue.Value;
            }

            if (shouldTryDefaultCulture)
            {
                InitJsonFromCulture(LocalizationOptions.Value.DefaultCulture);
                return GetString(name, false);
            }

            HandleMissingTranslation(name, culture);
            return null;
        }

        public MarkupString GetHtmlBlazorString(string name, bool shouldTryDefaultCulture = true) =>
            new MarkupString(GetString(name, shouldTryDefaultCulture));

        private void InitJsonFromCulture(CultureInfo cultureInfo)
        {
            var isFromMemCache = InitJsonStringLocalizer(cultureInfo);
            if (!isFromMemCache)
            {
                AddMissingCultureToSupportedCulture(cultureInfo);
                GetCultureToUse(cultureInfo);
            }
        }

        public void ClearMemCache(IEnumerable<CultureInfo> culturesToClearFromCache = null)
        {
            foreach (var cultureInfo in culturesToClearFromCache ??
                                        LocalizationOptions.Value.SupportedCultureInfos.ToArray())
                MemCache.Remove(GetCacheKey(cultureInfo));
        }

        public void ReloadMemCache(IEnumerable<CultureInfo> reloadCulturesToCache = null)
        {
            ClearMemCache();
            foreach (var cultureInfo in reloadCulturesToCache ??
                                        LocalizationOptions.Value.SupportedCultureInfos.ToArray())
                InitJsonFromCulture(cultureInfo);
        }

        private void HandleMissingTranslation(string name, CultureInfo culture)
        {
            if (LocalizationOptions.Value.MissingTranslationLogBehavior == MissingTranslationLogBehavior.LogConsoleError)
            {
                Console.Error.WriteLine($"Missing translation: '{name}' for culture '{culture.Name}'");
            }
            else if (LocalizationOptions.Value.MissingTranslationLogBehavior == MissingTranslationLogBehavior.CollectToJSON)
            {
                WriteMissingTranslations(name, culture);
            }
        }

        private void WriteMissingTranslations(string name, CultureInfo culture)
        {
            var cultureName = culture?.Name ?? "default";

            var cultureDict = _missingTranslations.GetOrAdd(cultureName, _ => new ConcurrentDictionary<string, string>());

            cultureDict[name] = name;

            // Limiter la taille du cache des traductions manquantes
            if (cultureDict.Count > LocalizationOptions.Value.MaxMissingTranslations)
            {
                var keysToRemove = cultureDict.Keys
                    .Take(cultureDict.Count - LocalizationOptions.Value.MaxMissingTranslations)
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    cultureDict.TryRemove(key, out _);
                }
            }

            // Write to file
            if (!string.IsNullOrEmpty(_missingTranslationsFile))
            {
                var extension = Path.GetExtension(_missingTranslationsFile);
                var nameWithoutExtension = Path.GetFileNameWithoutExtension(_missingTranslationsFile);
                var fileNameWithCulture = $"{nameWithoutExtension}-{cultureName}{extension}";

                try
                {
                    var json = JsonSerializer.Serialize(cultureDict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value), new JsonSerializerOptions { WriteIndented = true });
                    Task.Run(() => WriteFileSynchronously(fileNameWithCulture, json));
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error writing missing translations to file: {ex.Message}");
                }
            }
        }

        private static void WriteFileSynchronously(string path, string json)
        {
            try
            {
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error writing missing translations to file: {ex.Message}");
            }
        }
    }
}
