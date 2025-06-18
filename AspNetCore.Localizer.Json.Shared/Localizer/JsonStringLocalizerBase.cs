using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;
using AspNetCore.Localizer.Json.Commons;
using AspNetCore.Localizer.Json.Format;
using AspNetCore.Localizer.Json.JsonOptions;
using AspNetCore.Localizer.Json.Localizer.Modes;
using Microsoft.Extensions.Options;

namespace AspNetCore.Localizer.Json.Localizer
{
    internal class JsonStringLocalizerBase
    {
        private readonly string _baseName;

        #region Properties and Constructor

        protected CacheHelper MemCache;
        protected IOptions<JsonLocalizationOptions> LocalizationOptions;
        private TimeSpan _memCacheDuration;

        private const string CacheKey = "LocalizationBlob";
        protected readonly List<string> ResourcesRelativePaths = new();
        private string _currentCulture = string.Empty;
        protected Dictionary<string, LocalizatedFormat> Localization = new();
        private readonly Lazy<Dictionary<string, IPluralizationRuleSet>> _pluralizationRuleSets = new(() => new Dictionary<string, IPluralizationRuleSet>());

        private IAssemblyHelper _assemblyHelper;

        protected JsonStringLocalizerBase(IOptions<JsonLocalizationOptions> localizationOptions)
        {
            Initialize(localizationOptions);
        }
        
        protected JsonStringLocalizerBase(IOptions<JsonLocalizationOptions> localizationOptions, string baseName)
        {
            _baseName = baseName;
            Initialize(localizationOptions);
        }

        private void Initialize(IOptions<JsonLocalizationOptions> localizationOptions)
        {
            LocalizationOptions = localizationOptions;

            _assemblyHelper = localizationOptions.Value.AssemblyHelper;
            
            MemCache = LocalizationOptions.Value.DistributedCache != null
                ? new CacheHelper(LocalizationOptions.Value.DistributedCache)
                : new CacheHelper(LocalizationOptions.Value.Caching);

            _memCacheDuration = LocalizationOptions.Value.CacheDuration;
        }

        #endregion

        #region Cache and Culture Methods

        protected string GetCacheKey(CultureInfo ci) => $"{CacheKey}_{ci.Name}";

        private void SetCurrentCultureToCache(CultureInfo ci) => _currentCulture = ci.Name;

        protected bool IsUiCultureCurrentCulture(CultureInfo ci) =>
            string.Equals(_currentCulture, ci.Name, StringComparison.OrdinalIgnoreCase);

        protected void GetCultureToUse(CultureInfo cultureToUse)
        {
            string[] cacheKeys = {
                GetCacheKey(cultureToUse),
                GetCacheKey(cultureToUse.Parent),
                LocalizationOptions.Value.DefaultCulture != null ? GetCacheKey(LocalizationOptions.Value.DefaultCulture) : null
            };

            foreach (var key in cacheKeys)
            {
                if (key != null && MemCache.TryGetValue(key, out Localization))
                {
                    _currentCulture = cultureToUse.Name;
                    return;
                }
            }

            Localization = Localization ?? new Dictionary<string, LocalizatedFormat>();
        }


        private readonly Dictionary<string, IPluralizationRuleSet> _cachedPluralizationRules = new();

        protected IPluralizationRuleSet GetPluralizationToUse()
        {
            if (!_cachedPluralizationRules.TryGetValue(_currentCulture, out var ruleSet))
            {
                ruleSet = _pluralizationRuleSets.Value.TryGetValue(_currentCulture, out var foundRuleSet)
                    ? foundRuleSet
                    : new DefaultPluralizationRuleSet();
                _cachedPluralizationRules[_currentCulture] = ruleSet;
            }

            return ruleSet;
        }
        #endregion

        #region File Initialization

        protected void AddMissingCultureToSupportedCulture(CultureInfo cultureInfo)
        {
            LocalizationOptions.Value.SupportedCultureInfos.Add(cultureInfo);
        }

        protected bool InitJsonStringLocalizer(CultureInfo currentCulture)
        {
            if (!MemCache.TryGetValue(GetCacheKey(currentCulture), out Localization))
            {
                ConstructLocalizationObject(currentCulture);
                MemCache.Set(GetCacheKey(currentCulture), Localization, _memCacheDuration);
                return false;
            }
            
            return true;
        }

        private void ConstructLocalizationObject(CultureInfo currentCulture)
        {
            Localization ??= new Dictionary<string, LocalizatedFormat>();

            var myFiles = GetJsonFilesPath(currentCulture).ToArray(); 
            if (myFiles.Length > 0)
            {
                var localizationMode = LocalizationModeFactory.GetLocalisationFromMode(LocalizationOptions.Value.LocalizationMode);
                Localization = localizationMode.ConstructLocalization(myFiles, currentCulture, LocalizationOptions.Value);
            }
        }

        private IEnumerable<string> GetJsonFilesPath(CultureInfo culture)
        {
            var pathToLook = LocalizationOptions.Value.ResourcesPath ?? string.Empty;
            var additionalPath = LocalizationOptions.Value.AdditionalResourcesPaths ?? Array.Empty<string>();

            var cultureSpecificFile = $"{culture.Name}";
            var cultureNeutralFile = $"{culture.TwoLetterISOLanguageName}";

            var defaultFile = $".json";

            if (LocalizationOptions.Value.UseEmbeddedResources)
            {
                var assembly = _assemblyHelper.GetAssembly();
                var resourceNames = assembly.GetManifestResourceNames();

                return resourceNames
                    .Where(name =>
                        (additionalPath.Any(path => name.Contains($".{path}.", StringComparison.OrdinalIgnoreCase)) ||
                        name.Contains($".{pathToLook}.", StringComparison.OrdinalIgnoreCase)) &&
                        WithBaseName(name) &&
                        IsRelevantCultureFile(name, cultureSpecificFile, cultureNeutralFile, defaultFile, culture))
                    .ToList();
            }

            List<string> files = new();
            IEnumerable<string> directories = new[] { pathToLook }.Concat(additionalPath)
                .Where(p => !string.IsNullOrWhiteSpace(p));

            foreach (var dir in directories)
            {
                var searchPath = Path.IsPathRooted(dir) ? dir : Path.Combine(AppContext.BaseDirectory, dir);
                if (!Directory.Exists(searchPath))
                {
                    continue;
                }

                foreach (var file in Directory.GetFiles(searchPath, "*.json", SearchOption.AllDirectories))
                {
                    var fileName = Path.GetFileName(file);

                    if (WithBaseName(file) &&
                        IsRelevantCultureFile(fileName, cultureSpecificFile, cultureNeutralFile, defaultFile, culture))
                    {
                        files.Add(file);
                    }
                }
            }

            return files;
        }
        
        
        
        private bool WithBaseName(string name)
        {
            if (string.IsNullOrEmpty(_baseName))
            {
                return true;
            }
            
            return name.Contains(_baseName, StringComparison.OrdinalIgnoreCase);
        }
        private bool IsRelevantCultureFile(string resourceName,
            string cultureSpecificFile,
            string cultureNeutralFile,
            string defaultFile, CultureInfo culture)
        {
            
            if (resourceName.EndsWith(defaultFile, StringComparison.OrdinalIgnoreCase))
            {
                var prefix = resourceName.Substring(0, resourceName.LastIndexOf(defaultFile, StringComparison.OrdinalIgnoreCase));
                var lastSegment = prefix.Split('.').Last();
                
                var allCultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
                
                //if the last segment is not a culture, we take the file because if neutral culture is not the same as the parent culture
                if (!allCultures.Any(c => c.Name.Equals(lastSegment, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
                
                bool isInvariantCulture = string.Equals(lastSegment, CultureInfo.InvariantCulture.Name, StringComparison.OrdinalIgnoreCase);
                
                if (lastSegment.Equals(cultureSpecificFile, StringComparison.OrdinalIgnoreCase) ||
                    (lastSegment.Equals(cultureNeutralFile, StringComparison.OrdinalIgnoreCase) &&
                     !isInvariantCulture))
                {
                    return true;
                }
            }

            return false;
        }
        #endregion
    }
}
