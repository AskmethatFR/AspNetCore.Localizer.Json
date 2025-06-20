﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using AspNetCore.Localizer.Json.Commons;
using AspNetCore.Localizer.Json.Extensions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Localization;

namespace AspNetCore.Localizer.Json.JsonOptions
{
    public class JsonLocalizationOptions : LocalizationOptions
    {
        private const char PLURAL_SEPARATOR = '|';
        private const string DEFAULT_RESOURCES = "Resources";
        private const string DEFAULT_CULTURE = "en-US";
        public const string DEFAULT_MISSING_TRANSLATIONS = "MissingTranslations.json";

        public new string ResourcesPath { get; set; } = DEFAULT_RESOURCES;
        
        /// We cache all values to memory to avoid loading files for each request, this parameter defines the time after which the cache is refreshed.
        public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// This property stores the MemoryCache for the cached translations.
        /// </summary>
        public IMemoryCache Caching { get; set; } = new MemoryCache(new MemoryCacheOptions
        {
        });

        public IDistributedCache DistributedCache { get; set; }

        private CultureInfo defaultCulture = new CultureInfo(DEFAULT_CULTURE);

        /// <summary>
        /// Sets the default culture to use.
        /// </summary>
        public CultureInfo DefaultCulture
        {
            get => defaultCulture;
            set
            {
                if (value != defaultCulture)
                {
                    defaultCulture = value ?? CultureInfo.InvariantCulture;
                }
            }
        }

        private CultureInfo defaultUICulture = new CultureInfo(DEFAULT_CULTURE);

        /// <summary>
        /// Sets the default ui culture to use.
        /// </summary>
        public CultureInfo DefaultUICulture
        {
            get => defaultUICulture;
            set
            {
                if (value != defaultUICulture)
                {
                    defaultUICulture = value ?? CultureInfo.InvariantCulture;
                }
            }
        }

        private HashSet<CultureInfo> supportedCultureInfos = new HashSet<CultureInfo>
        {

        };

        /// <summary>
        /// Optional array of cultures that you should provide to plugin. (Like RequestLocalizationOptions)
        /// </summary>
        public HashSet<CultureInfo> SupportedCultureInfos
        {
            get => supportedCultureInfos;
            set
            {
                if (value != supportedCultureInfos)
                {
                    supportedCultureInfos = value;
                }
            }
        }

        /// <summary>
        /// Specify the file encoding name.
        /// .NET core supported:
        /// https://docs.microsoft.com/en-us/dotnet/api/system.text.encoding?view=netcore-3.1
        /// </summary>
        public string FileEncodingName {
            get => FileEncoding.EncodingName;
            set => FileEncoding = Encoding.GetEncoding(value);
        }

        /// <summary>
        /// Specify the file encoding.
        /// </summary>
        public Encoding FileEncoding { get; set; } = Encoding.UTF8;

        /// <summary>
        /// Separator used to get singular or pluralized version of localization.
        /// </summary>
        public char PluralSeparator { get; set; } = PLURAL_SEPARATOR;

        /// <summary>
        /// Define logging behavior when a translation is not found.
        /// </summary>
        public MissingTranslationLogBehavior MissingTranslationLogBehavior { get; set; } = MissingTranslationLogBehavior.LogConsoleError;

        /// <summary>
        /// Define JSON Files management. See documentation for more information
        /// </summary>
        public LocalizationMode LocalizationMode { get; set; } = LocalizationMode.Basic;

        /// <summary>
        /// Local file name where the missing translation JSON values can be written. See documentation for more information
        /// </summary>
        public string MissingTranslationsOutputFile { get; set; } = DEFAULT_MISSING_TRANSLATIONS;

        /// <summary>
        /// If set to true, the localizer will replace all the translated text with "*" to help identify missing translations.
        /// </summary>
        public bool LocalizerDiagnosticMode { get; set; } = false;
        

        /// <summary>
        /// This properly will ignore the JSON errors if set to true. Recommended in production but not in development.
        /// </summary>
        public bool IgnoreJsonErrors { get; set; } = false;
        
        /// <summary>
        /// Provide a custom assembly helper to load assemblies.
        /// </summary>
        public IAssemblyHelper AssemblyHelper { get; set; } = new AssemblyHelper();

        /// <summary>
        /// Additional paths to search for JSON files.
        /// </summary>
        public string[] AdditionalResourcesPaths { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Determines whether localization files should be loaded from embedded resources.
        /// Defaults to <c>true</c> to keep the previous behaviour.
        /// When set to <c>false</c>, files are loaded directly from the file system.
        /// </summary>
        public bool UseEmbeddedResources { get; set; } = true;
    }
}
