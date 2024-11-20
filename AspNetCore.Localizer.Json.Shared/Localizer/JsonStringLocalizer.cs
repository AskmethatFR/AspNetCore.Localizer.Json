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
using Microsoft.AspNetCore.Components;

namespace AspNetCore.Localizer.Json.Localizer
{
    internal partial class JsonStringLocalizer : JsonStringLocalizerBase, IJsonStringLocalizer
    {
        private readonly ConcurrentDictionary<string, IDictionary<string, string>> _missingJsonValues = new();
        private string _missingTranslations = null;

        private static LocalizedString ConvertToChar(string value, char c, int additionalRepeats = 0) =>
            new LocalizedString(value, new string(c, value.Length + additionalRepeats));

        public LocalizedString this[string name] => GetLocalizedString(name);

        public LocalizedString this[string name, params object[] arguments] => GetLocalizedString(name, arguments);

        private LocalizedString GetLocalizedString(string name, params object[] arguments)
        {
            if (_localizationOptions.Value.LocalizerDiagnosticMode)
                return ConvertToChar(name, 'X');

            string format = GetString(name);
            string value = arguments.Length > 0
                ? GetPluralLocalization(name, format, arguments)
                : (format ?? name);

            return new LocalizedString(name, value, resourceNotFound: format == null);
        }

        private string GetPluralLocalization(string name, string format, object[] arguments)
        {
            if (arguments.LastOrDefault() is bool isPlural)
            {
                string value = GetString(name);
                if (!string.IsNullOrEmpty(value) && value.Contains(_localizationOptions.Value.PluralSeparator))
                {
                    return value.Split(_localizationOptions.Value.PluralSeparator)[isPlural ? 1 : 0];
                }
            }

            return string.Format(format ?? name, arguments);
        }

        public LocalizedString GetPlural(string name, double count, params object[] arguments)
        {
            // Initialize the culture if needed
            if (!IsUICultureCurrentCulture(CultureInfo.CurrentUICulture))
            {
                InitJsonFromCulture(CultureInfo.CurrentUICulture);
            }
            else
            {
                InitJsonFromCulture(_localizationOptions.Value.DefaultCulture);
            }

            // Retrieve the pluralization rule set for the current culture
            IPluralizationRuleSet pluralizationRuleSet = GetPluralizationToUse();
            string applicableRule = pluralizationRuleSet.GetMatchingPluralizationRule(count);

            // Generate the pluralization rule key
            string nameWithRule = name + "." + applicableRule;

            // Attempt to get the localized format
            string format = localization?.TryGetValue(nameWithRule, out var localizedValue) == true
                ? localizedValue.Value
                : null;

            // If the specific rule is not found, try the "Other" rule or fallback
            string fallback = null;
            if (format == null)
            {
                string nameWithOtherRule = name + "." + PluralizationConstants.Other;
                format = localization?.TryGetValue(nameWithOtherRule, out var localizedOtherValue) == true
                    ? localizedOtherValue.Value
                    : (fallback = GetString(name, true)); // Avoid multiple calls
            }

            format ??= fallback;

            // Prepare arguments with `count` as the first element using an optimized list allocation
            var argumentsWithCount = new object[arguments.Length + 1];
            argumentsWithCount[0] = count;
            Array.Copy(arguments, 0, argumentsWithCount, 1, arguments.Length);

            // Format the final localized string
            string value = string.Format(format ?? name, argumentsWithCount);

            // Return the localized string with an indicator if the format was found
            return new LocalizedString(name, value, format != null);
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            InitJsonFromCulture(CultureInfo.CurrentUICulture);

            return localization?.Where(w => includeParentCultures || !w.Value.IsParent)
                .Select(l =>
                    new LocalizedString(l.Key, GetString(l.Key) ?? l.Key, resourceNotFound: GetString(l.Key) == null))
                .OrderBy(s => s.Name);
        }

        public IStringLocalizer WithCulture(CultureInfo culture)
        {
            if (!_localizationOptions.Value.SupportedCultureInfos.Contains(culture))
                _localizationOptions.Value.SupportedCultureInfos.Add(culture);

            CultureInfo.CurrentCulture = culture;

            return new JsonStringLocalizer(_localizationOptions, _env);
        }

        private string GetString(string name, bool shouldTryDefaultCulture = true)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            CultureInfo? culture = shouldTryDefaultCulture && !IsUICultureCurrentCulture(CultureInfo.CurrentUICulture)
                ? CultureInfo.CurrentUICulture
                : null;

            if (culture != null)
                InitJsonFromCulture(culture);

            if (localization != null && localization.TryGetValue(name, out LocalizatedFormat localizedValue))
                return localizedValue.Value;

            if (shouldTryDefaultCulture)
            {
                InitJsonFromCulture(_localizationOptions.Value.DefaultCulture);
                return GetString(name, false);
            }

            HandleMissingTranslation(name, culture);
            return null;
        }

        private void HandleMissingTranslation(string name, CultureInfo? culture)
        {
            var cultureName = culture?.TwoLetterISOLanguageName ?? "default";

            if (_localizationOptions.Value.MissingTranslationLogBehavior ==
                MissingTranslationLogBehavior.LogConsoleError)
            {
                Console.Error.WriteLine($"'{name}' does not contain any translation for {cultureName}");
            }

            if (_localizationOptions.Value.MissingTranslationLogBehavior == MissingTranslationLogBehavior.CollectToJSON)
            {
                if (!_missingJsonValues.TryGetValue(cultureName, out var localeMissingValues))
                {
                    localeMissingValues = new ConcurrentDictionary<string, string>();
                    _missingJsonValues.TryAdd(cultureName, localeMissingValues);
                }

                if (localeMissingValues.TryAdd(name, name))
                {
                    Console.Error.WriteLine($"'{name}' added to missing values");
                    WriteMissingTranslations();
                }
            }
        }

        public MarkupString GetHtmlBlazorString(string name, bool shouldTryDefaultCulture = true) =>
            new MarkupString(GetString(name, shouldTryDefaultCulture));

        private void InitJsonFromCulture(CultureInfo cultureInfo)
        {
            InitJsonStringLocalizer(cultureInfo);
            AddMissingCultureToSupportedCulture(cultureInfo);
            GetCultureToUse(cultureInfo);
        }

        public void ClearMemCache(IEnumerable<CultureInfo> culturesToClearFromCache = null)
        {
            foreach (var cultureInfo in culturesToClearFromCache ??
                                        _localizationOptions.Value.SupportedCultureInfos.ToArray())
                _memCache.Remove(GetCacheKey(cultureInfo));
        }

        public void ReloadMemCache(IEnumerable<CultureInfo> reloadCulturesToCache = null)
        {
            ClearMemCache();
            foreach (var cultureInfo in reloadCulturesToCache ??
                                        _localizationOptions.Value.SupportedCultureInfos.ToArray())
                InitJsonFromCulture(cultureInfo);
        }

        private void WriteMissingTranslations()
        {
            if (!string.IsNullOrWhiteSpace(_missingTranslations) && (_missingJsonValues?.Count ?? 0) > 0)
            {
                try
                {
                    foreach (var locale in _missingJsonValues)
                    {
                        if (locale.Value is null) continue;

                        var json = JsonSerializer.Serialize(locale.Value);
                        var newFile = Path.ChangeExtension($"{Path.GetFileNameWithoutExtension(_missingTranslations)}-{locale.Key}", Path.GetExtension(_missingTranslations));
                        Console.Error.WriteLine($"Writing {locale.Value.Count} missing translations to {Path.GetFullPath(newFile)}");

                        lock (_missingJsonValues)
                        {
                            File.WriteAllText(newFile, json);
                        }
                    }
                }
                catch (Exception)
                {
                    Console.Error.WriteLine($"Cannot write missing translations to {Path.GetFullPath(_missingTranslations)}");
                }
            }
        }

        /// <summary>
        /// Get the full path of a JSON resource.
        /// </summary>
        /// <param name="path">Relative or absolute path of the resource.</param>
        /// <returns>Full path to the resource.</returns>
        private string GetJsonRelativePath(string path)
        {
            // If the path is absolute, return it directly.
            if (_localizationOptions.Value.IsAbsolutePath)
            {
                return path;
            }

            // Handle relative or unspecified paths.
            if (string.IsNullOrEmpty(path))
            {
                // Use the "Resources" directory in the application root path.
                return Path.Combine(_env.ContentRootPath, "Resources");
            }

            // If a relative path is provided, combine it with the base directory of the application.
            return Path.Combine(AppContext.BaseDirectory, path);
        }
    }
}
