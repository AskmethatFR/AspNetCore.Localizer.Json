using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using AspNetCore.Localizer.Json.Caching;
using AspNetCore.Localizer.Json.Format;
using AspNetCore.Localizer.Json.JsonOptions;
using AspNetCore.Localizer.Json.Localizer.Modes;
using Microsoft.Extensions.Options;

namespace AspNetCore.Localizer.Json.Localizer
{
    internal class JsonStringLocalizerBase
    {
        #region Properties and Constructor

        protected readonly CacheHelper _memCache;
        protected readonly IOptions<JsonLocalizationOptions> _localizationOptions;
        private readonly EnvironmentWrapper _environment;
        protected readonly string _baseName;
        protected readonly TimeSpan _memCacheDuration;

        protected const string CACHE_KEY = "LocalizationBlob";
        protected readonly List<string> resourcesRelativePaths = new();
        protected string currentCulture = string.Empty;
        protected Dictionary<string, LocalizatedFormat> localization = new();
        protected readonly Lazy<Dictionary<string, IPluralizationRuleSet>> pluralizationRuleSets = new(() => new Dictionary<string, IPluralizationRuleSet>());

        public JsonStringLocalizerBase(
            IOptions<JsonLocalizationOptions> localizationOptions,
            EnvironmentWrapper environment = null,
            string baseName = null)
        {
            _baseName = CleanBaseName(baseName);
            _localizationOptions = localizationOptions;
            _environment = environment;

            ValidateOptions();

            _memCache = _localizationOptions.Value.DistributedCache != null
                ? new CacheHelper(_localizationOptions.Value.DistributedCache)
                : new CacheHelper(_localizationOptions.Value.Caching);

            _memCacheDuration = _localizationOptions.Value.CacheDuration;
        }
        #endregion

        #region Validation

        private void ValidateOptions()
        {
            if (_localizationOptions.Value.LocalizationMode == LocalizationMode.I18n && _localizationOptions.Value.UseBaseName)
                throw new ArgumentException("UseBaseName can't be activated with I18n localisation mode");

            if (_environment?.IsWasm == true && (_localizationOptions.Value.JsonFileList?.Length ?? 0) == 0)
                throw new ArgumentException("JsonFileList is required in Client WASM mode");
        }
        #endregion

        #region Cache and Culture Methods

        protected string GetCacheKey(CultureInfo ci) => $"{CACHE_KEY}_{ci.Name}";

        private void SetCurrentCultureToCache(CultureInfo ci) => currentCulture = ci.Name;

        protected bool IsUICultureCurrentCulture(CultureInfo ci) =>
            string.Equals(currentCulture, ci.Name, StringComparison.OrdinalIgnoreCase);

        protected void GetCultureToUse(CultureInfo cultureToUse)
        {
            var culturesToTry = new[]
            {
                cultureToUse,
                cultureToUse.Parent,
                _localizationOptions.Value.DefaultCulture
            };

            foreach (var culture in culturesToTry)
            {
                if (_memCache.TryGetValue(GetCacheKey(culture), out localization))
                {
                    SetCurrentCultureToCache(culture);
                    return;
                }
            }

            localization = new Dictionary<string, LocalizatedFormat>();
        }

        protected IPluralizationRuleSet GetPluralizationToUse()
        {
            return pluralizationRuleSets.Value.TryGetValue(currentCulture, out var ruleSet)
                ? ruleSet
                : new DefaultPluralizationRuleSet();
        }
        #endregion

        #region File Initialization

        protected void AddMissingCultureToSupportedCulture(CultureInfo cultureInfo)
        {
            if (!_localizationOptions.Value.SupportedCultureInfos.Contains(cultureInfo))
            {
                _localizationOptions.Value.SupportedCultureInfos.Add(cultureInfo);
            }
        }

        protected bool InitJsonStringLocalizer(CultureInfo currentCulture)
        {
            _memCache.TryGetValue(GetCacheKey(currentCulture), out localization);
            var fromMemCache = localization is not null;
            if (!fromMemCache)
            {
                ConstructLocalizationObject(resourcesRelativePaths, currentCulture);
                _memCache.Set(GetCacheKey(currentCulture), localization, _memCacheDuration);
            }
            
            return fromMemCache;
        }

        private void ConstructLocalizationObject(List<string> jsonPath, CultureInfo currentCulture)
        {
            localization ??= new Dictionary<string, LocalizatedFormat>();

            if (_environment?.IsWasm == true && (_localizationOptions.Value.JsonFileList?.Length ?? 0) == 0)
                throw new ArgumentException("JsonFileList is required in Client WASM mode");

            var myFiles = GetJsonFilesPath(jsonPath);
            localization = LocalizationModeFactory
                .GetLocalisationFromMode(_localizationOptions.Value.LocalizationMode, _localizationOptions.Value.Assembly)
                .ConstructLocalization(myFiles, currentCulture, _localizationOptions.Value);
        }

        private IEnumerable<string> GetJsonFilesPath(List<string> jsonPaths)
        {
            const string searchPattern = "*.json";
            const string sharedSearchPattern = "*.shared.json";

            List<string> files = new();

            foreach (var jsonPath in jsonPaths)
            {
                string basePath = jsonPath;

                if (_localizationOptions.Value.UseBaseName && !string.IsNullOrWhiteSpace(_baseName))
                {
                    basePath = Path.Combine(jsonPath, TransformNameToPath(_baseName));
                    if (Directory.Exists(basePath))
                    {
                        files.AddRange(Directory.GetFiles(basePath, searchPattern, SearchOption.TopDirectoryOnly));
                    }
                }
                else
                {
                    files.AddRange(Directory.GetFiles(basePath, searchPattern, SearchOption.AllDirectories));
                }

                // Add shared files
                files.AddRange(Directory.GetFiles(basePath, sharedSearchPattern, SearchOption.TopDirectoryOnly));
                files.AddRange(Directory.GetFiles(jsonPath, "localization.shared.json", SearchOption.TopDirectoryOnly));
            }

            return files.Distinct();
        }
        #endregion

        #region Helper Methods

        private string TransformNameToPath(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            // Use Span<char> to replace '.' with the directory separator, to minimize allocations
            ReadOnlySpan<char> nameSpan = name.AsSpan();
            Span<char> transformedName = stackalloc char[name.Length];
            for (int i = 0; i < nameSpan.Length; i++)
            {
                transformedName[i] = nameSpan[i] == '.' ? Path.DirectorySeparatorChar : nameSpan[i];
            }
            return new string(transformedName);
        }

        private string CleanBaseName(string baseName)
        {
            if (string.IsNullOrEmpty(baseName)) return string.Empty;

            var plusIdx = baseName.IndexOf('+');
            return plusIdx == -1 ? baseName : baseName.Substring(0, plusIdx);
        }
        #endregion
    }
}
