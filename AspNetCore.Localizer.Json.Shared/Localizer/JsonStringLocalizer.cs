#nullable enable
using AspNetCore.Localizer.Json.Extensions;
using AspNetCore.Localizer.Json.Format;
using Microsoft.Extensions.Localization;
using System;
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
        private readonly Dictionary<string, IDictionary<string, string>> _missingJsonValues = new();
        private string _missingTranslations = null;

        private static LocalizedString ConvertToChar(string value, char c, int additionalRepeats = 0) =>
            new LocalizedString(value, new string(c, value.Length + additionalRepeats));

        public LocalizedString this[string name] => GetLocalizedString(name);

        public LocalizedString this[string name, params object[] arguments] => GetLocalizedString(name, arguments);

        private LocalizedString GetLocalizedString(string name, params object[] arguments)
        {
            if (_localizationOptions.Value.LocalizerDiagnosticMode)
                return ConvertToChar(name, 'X');

            string? value = GetString(name);
            if (value == null)
            {
                return new LocalizedString(name, name, resourceNotFound: true);
            }

            value = FormatString(value, arguments);
       
            return new LocalizedString(name, value, resourceNotFound: false);
        }

        private string FormatString(string value, object[]? arguments)
        {
            // Vérifie si des arguments sont présents et formate la chaîne
            if (arguments != null && arguments.Length > 0)
            {
                value = string.Format(value, arguments);
            }

            if (arguments != null && arguments.LastOrDefault() is bool isPlural)
            {
                if (!string.IsNullOrEmpty(value) && value.Contains(_localizationOptions.Value.PluralSeparator))
                {
                    var parts = value.Split(_localizationOptions.Value.PluralSeparator);
                    return parts.Length > 1 ? parts[isPlural ? 1 : 0] : value;
                }
            }

            return value;
        }

        public LocalizedString GetPlural(string name, double count, params object[] arguments)
        {
            InitCorrectJsonCulture();

            // Retrieve the pluralization rule set for the current culture
            IPluralizationRuleSet pluralizationRuleSet = GetPluralizationToUse();
            string applicableRule = pluralizationRuleSet.GetMatchingPluralizationRule(count);

            // Generate the pluralization rule key
            string nameWithRule = name + "." + applicableRule;

            // Attempt to get the localized format
            if (localization?.TryGetValue(nameWithRule, out var localizedValue) == true)
            {
                return FormatLocalizedString(name, localizedValue.Value, count, arguments);
            }

            // If the specific rule is not found, try the "Other" rule or fallback
            string nameWithOtherRule = name + "." + PluralizationConstants.Other;
            if (localization?.TryGetValue(nameWithOtherRule, out var localizedOtherValue) == true)
            {
                return FormatLocalizedString(name, localizedOtherValue.Value, count, arguments);
            }

            string fallback = GetString(name, true);
            return FormatLocalizedString(name, fallback ?? name, count, arguments, fallback == null);
        }

        private LocalizedString FormatLocalizedString(string name, string format, double count, object[] arguments, bool resourceNotFound = false)
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
            var culture = CultureInfo.DefaultThreadCurrentUICulture ?? CultureInfo.CurrentUICulture;
            if (!IsUICultureCurrentCulture(culture))
            {
                InitJsonFromCulture(culture);
            }

            return culture;
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            InitCorrectJsonCulture();

            return localization?.Where(w => includeParentCultures || !w.Value.IsParent)
                .Select(l =>
                    new LocalizedString(l.Key, l.Value.Value ?? l.Key, resourceNotFound: l.Value.Value == null))
                .OrderBy(s => s.Name);
        }

        public IStringLocalizer WithCulture(CultureInfo culture)
        {
            _localizationOptions.Value.SupportedCultureInfos.Add(culture);

            CultureInfo.CurrentCulture = culture;

            return new JsonStringLocalizer(_localizationOptions, _env);
        }

        private readonly Dictionary<string, string> _localStringCache = new();

        private string? GetString(string name, bool shouldTryDefaultCulture = true)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            // Check cache before looking up
            if (_localStringCache.TryGetValue(name, out string? cachedValue))
            {
                return cachedValue;
            }

            var culture = InitCorrectJsonCulture(true);

            if (localization != null && localization.TryGetValue(name, out LocalizatedFormat? localizedValue))
            {
                _localStringCache[name] = localizedValue.Value; // Cache the result
                return localizedValue.Value;
            }

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
                    localeMissingValues = new Dictionary<string, string>();
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
