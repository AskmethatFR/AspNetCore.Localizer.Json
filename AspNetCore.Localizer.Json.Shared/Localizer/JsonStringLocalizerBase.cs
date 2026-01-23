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
    internal class JsonStringLocalizerBase : IDisposable
    {
        private readonly string _baseName;
        private bool _disposed = false;

        #region Properties and Constructor

        protected CacheHelper MemCache;
        protected IOptions<JsonLocalizationOptions> LocalizationOptions;
        private TimeSpan _memCacheDuration;

        private const string CacheKey = "LocalizationBlob";
        private string _currentCulture = string.Empty;
        // Lazy-loaded localization dictionary to defer resource loading until first access
        protected Lazy<Dictionary<string, LocalizatedFormat>> _lazyLocalization;

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

            // Initialiser _lazyLocalization avec un dictionnaire vide
            _lazyLocalization = new Lazy<Dictionary<string, LocalizatedFormat>>(
                () => new Dictionary<string, LocalizatedFormat>());

            _localizationMode = LocalizationModeFactory.GetLocalisationFromMode(LocalizationOptions.Value.LocalizationMode);
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
                LocalizationOptions.Value.DefaultCulture != null ?
                    GetCacheKey(LocalizationOptions.Value.DefaultCulture) : null
            };

            foreach (var key in cacheKeys)
            {
                if (key != null && MemCache.TryGetValue(key, out var cachedLocalization))
                {
                    // Réutiliser le Lazy existant au lieu de le recréer
                    UpdateLazyLocalization(cachedLocalization);
                    SetCurrentCultureToCache(cultureToUse);
                    return;
                }
            }
        }

        private readonly Dictionary<string, IPluralizationRuleSet> _cachedPluralizationRules = new();
        private ILocalizationModeGenerator _localizationMode;
        private const int MaxCachedPluralizationRules = 10;

        protected IPluralizationRuleSet GetPluralizationToUse()
        {
            if (!_cachedPluralizationRules.TryGetValue(_currentCulture, out var ruleSet))
            {
                ruleSet = _pluralizationRuleSets.Value.TryGetValue(_currentCulture, out var foundRuleSet)
                    ? foundRuleSet
                    : new DefaultPluralizationRuleSet();

                // Limiter la taille du cache LRU
                if (_cachedPluralizationRules.Count >= MaxCachedPluralizationRules)
                {
                    var oldestKey = _cachedPluralizationRules.Keys.First();
                    _cachedPluralizationRules.Remove(oldestKey);
                    Console.WriteLine($"[MEMORY_DEBUG] Removed oldest pluralization rule for culture: {oldestKey}");
                }

                _cachedPluralizationRules[_currentCulture] = ruleSet;
                Console.WriteLine($"[MEMORY_DEBUG] Cached pluralization rules count: {_cachedPluralizationRules.Count}");
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
            if (!MemCache.TryGetValue(GetCacheKey(currentCulture), out var cachedLocalization))
            {
                ConstructLocalizationObject(currentCulture);
                MemCache.Set(GetCacheKey(currentCulture), _lazyLocalization.Value, _memCacheDuration);
                SetCurrentCultureToCache(currentCulture);
                return false;
            }

            // Réutiliser le Lazy existant au lieu de le recréer
            UpdateLazyLocalization(cachedLocalization);
            SetCurrentCultureToCache(currentCulture);
            return true;
        }

        private void ConstructLocalizationObject(CultureInfo currentCulture)
        {
            // Localization is now lazy-loaded, so we populate it on first access
            var myFiles = GetJsonFilesPath(currentCulture).ToArray();
            if (myFiles.Length > 0)
            {
                Console.Error.WriteLine($"[MEMORY_DEBUG] Loaded {myFiles.Length} files for culture: {currentCulture.Name}");
                // list of files loaded
                Console.Error.WriteLine($"[MEMORY_DEBUG] Loaded files: {string.Join(", ", myFiles)}");
                var newLocalization = _localizationMode.ConstructLocalization(myFiles, currentCulture, LocalizationOptions.Value);

                // Réutiliser le Lazy existant au lieu de le recréer
                UpdateLazyLocalization(newLocalization);
            }
        }

        /// <summary>
        /// Crée un nouveau Lazy<Dictionary> pour chaque culture.
        /// Cela évite la corruption des données lors des changements de culture.
        /// </summary>
        private void UpdateLazyLocalization(Dictionary<string, LocalizatedFormat> newLocalization)
        {
            // Toujours créer un nouveau Lazy pour chaque culture
            // Cela évite la corruption des données quand on réutilise le même dictionnaire
            _lazyLocalization = new Lazy<Dictionary<string, LocalizatedFormat>>(() => newLocalization);
        }

        private IEnumerable<string> GetJsonFilesPath(CultureInfo culture)
        {
            var pathToLook = LocalizationOptions.Value.ResourcesPath ?? string.Empty;
            var additionalPath = LocalizationOptions.Value.AdditionalResourcesPaths ?? Array.Empty<string>();

            var cultureSpecificFile = culture.Name;
            var cultureParentFile = culture.Parent != null && culture.Parent != CultureInfo.InvariantCulture
                ? culture.Parent.Name
                : culture.TwoLetterISOLanguageName;
            var cultureTwoLetterFile = culture.TwoLetterISOLanguageName;
            var cultureCandidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                cultureSpecificFile,
                cultureParentFile,
                cultureTwoLetterFile
            };

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
                        IsRelevantCultureFile(name, cultureCandidates, defaultFile))
                    .ToList();
            }

            List<string> files = new();
            IEnumerable<string> directories = new[] { pathToLook }.Concat(additionalPath)
                .Where(p => !string.IsNullOrWhiteSpace(p));

            var searchRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var dir in directories)
            {
                if (Path.IsPathRooted(dir))
                {
                    searchRoots.Add(dir);
                }
                else
                {
                    searchRoots.Add(Path.Combine(AppContext.BaseDirectory, dir));
                    searchRoots.Add(Path.Combine(Directory.GetCurrentDirectory(), dir));
                }
            }

            foreach (var searchPath in searchRoots)
            {
                if (!Directory.Exists(searchPath))
                {
                    continue;
                }

                foreach (var file in Directory.GetFiles(searchPath, "*.json", SearchOption.AllDirectories))
                {
                    var fileName = Path.GetFileName(file);

                    if (WithBaseName(file) &&
                        IsRelevantCultureFile(fileName, cultureCandidates, defaultFile))
                    {
                        files.Add(file);
                    }
                }
            }

            files.Sort(StringComparer.OrdinalIgnoreCase);
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
            HashSet<string> cultureCandidates,
            string defaultFile)
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

                if (cultureCandidates.Contains(lastSegment) && !isInvariantCulture)
                {
                    return true;
                }
            }

            return false;
        }
        #endregion

        #region IDisposable Implementation

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    MemCache?.Dispose();

                    // Clear collections
                    _cachedPluralizationRules?.Clear();

                    // Do not clear _lazyLocalization: it may be shared via cache and clearing
                    // would wipe cached translations for other scopes.
                    if (_pluralizationRuleSets?.IsValueCreated == true)
                    {
                        _pluralizationRuleSets.Value?.Clear();
                    }
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
