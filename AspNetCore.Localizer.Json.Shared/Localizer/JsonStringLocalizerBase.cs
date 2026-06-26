using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
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
        private static readonly Regex CultureNameRegex = new("^[a-zA-Z]{2,3}(?:-[a-zA-Z0-9]{2,8}){0,2}$", RegexOptions.Compiled);

        #region Properties and Constructor

        protected CacheHelper MemCache;
        protected IOptions<JsonLocalizationOptions> LocalizationOptions;
        private TimeSpan _memCacheDuration;

        private const string CacheKey = "LocalizationBlob";
        protected string _currentCulture = string.Empty;
        // Lazy-loaded localization dictionary to defer resource loading until first access
        protected ConcurrentDictionary<string, Dictionary<string, LocalizedFormat>> _localizationCache = new();

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
                    SetCurrentCultureToCache(cultureToUse);
                    UpdateLazyLocalization(cultureToUse.Name, cachedLocalization);
                    return;
                }
            }
        }

        private readonly ConcurrentDictionary<string, IPluralizationRuleSet> _cachedPluralizationRules = new();
        private ILocalizationModeGenerator _localizationMode;

        protected IPluralizationRuleSet GetPluralizationToUse()
        {
            return _cachedPluralizationRules.GetOrAdd(_currentCulture, culture =>
            {
                return _pluralizationRuleSets.Value.TryGetValue(culture, out var foundRuleSet)
                    ? foundRuleSet
                    : new DefaultPluralizationRuleSet();
            });
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
                if (_localizationCache.TryGetValue(currentCulture.Name, out var dict))
                {
                    MemCache.Set(GetCacheKey(currentCulture), dict, _memCacheDuration);
                }
                SetCurrentCultureToCache(currentCulture);
                return false;
            }

            SetCurrentCultureToCache(currentCulture);
            UpdateLazyLocalization(currentCulture.Name, cachedLocalization);
            return true;
        }

        private void ConstructLocalizationObject(CultureInfo currentCulture)
        {
            var myFiles = GetJsonFilesPath(currentCulture).ToArray();
            if (myFiles.Length > 0)
            {
                var newLocalization = _localizationMode.ConstructLocalization(myFiles, currentCulture, LocalizationOptions.Value);

                UpdateLazyLocalization(currentCulture.Name, newLocalization);
            }
        }

        private void UpdateLazyLocalization(string culture, Dictionary<string, LocalizedFormat> newLocalization)
        {
            _localizationCache[culture] = newLocalization;
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
                var assemblies = _assemblyHelper.GetAssemblies();
                var resourceNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var asm in assemblies)
                {
                    foreach (var name in asm.GetManifestResourceNames())
                    {
                        resourceNames.Add(name);
                    }
                }

                return resourceNames
                    .Where(name =>
                        (additionalPath.Any(path => name.Contains($".{path}.", StringComparison.OrdinalIgnoreCase)) ||
                        name.Contains($".{pathToLook}.", StringComparison.OrdinalIgnoreCase)) &&
                        WithBaseName(name) &&
                        IsRelevantEmbeddedCultureFile(name, cultureCandidates))
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

            if (!resourceName.EndsWith(defaultFile, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(resourceName) ?? string.Empty;
            var nameSegments = fileNameWithoutExtension.Split('.', StringSplitOptions.RemoveEmptyEntries);

            // No culture suffix in the filename: treat as neutral resource.
            if (nameSegments.Length == 1 || !CultureNameRegex.IsMatch(nameSegments[^1]))
            {
                return true;
            }

            var cultureSegment = nameSegments[^1];
            bool isInvariantCulture = string.Equals(cultureSegment, CultureInfo.InvariantCulture.Name, StringComparison.OrdinalIgnoreCase);

            return cultureCandidates.Contains(cultureSegment) && !isInvariantCulture;
        }

        /// <summary>
        /// Filters embedded resource names by culture. Embedded resources use dotted names
        /// (e.g. "Namespace.Resources.fr.localization.json") where the culture code is a
        /// path segment, not a filename suffix.
        /// </summary>
        private bool IsRelevantEmbeddedCultureFile(string resourceName,
            HashSet<string> cultureCandidates)
        {
            if (!resourceName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var nameWithoutExt = Path.GetFileNameWithoutExtension(resourceName) ?? string.Empty;
            var segments = nameWithoutExt.Split('.', StringSplitOptions.RemoveEmptyEntries);

            // Find the resource path segment and only look after it for culture codes
            var pathToLook = LocalizationOptions.Value.ResourcesPath ?? "Resources";
            var resourcePathIndex = Array.FindIndex(segments,
                s => s.Equals(pathToLook, StringComparison.OrdinalIgnoreCase));
            var startIndex = resourcePathIndex >= 0 ? resourcePathIndex + 1 : 0;

            for (var i = startIndex; i < segments.Length; i++)
            {
                if (CultureNameRegex.IsMatch(segments[i]) &&
                    !string.Equals(segments[i], CultureInfo.InvariantCulture.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return cultureCandidates.Contains(segments[i]);
                }
            }

            // No culture segment found: treat as neutral/default resource.
            return true;
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
                    _cachedPluralizationRules.Clear();

                    _localizationCache.Clear();
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
