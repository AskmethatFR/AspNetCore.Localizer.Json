using AspNetCore.Localizer.Json.Localizer;
using System.Globalization;
using AspNetCore.Localizer.Json.Benchmark.Resources;
using AspNetCore.Localizer.Json.Commons;
using AspNetCore.Localizer.Json.JsonOptions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace AspNetCore.Localizer.Json.Benchmark
{

    [MinColumn, MaxColumn, MemoryDiagnoser, MarkdownExporter]
    public class BenchmarkJSONLocalizer
    {
        private readonly IMemoryCache _cach = new MemoryCache(Options.Create<MemoryCacheOptions>(new MemoryCacheOptions() {}));
        private readonly IMemoryCache _cach2 = new MemoryCache(Options.Create<MemoryCacheOptions>(new MemoryCacheOptions() { }));

        private readonly IStringLocalizer _jsonLocalizer;

        public BenchmarkJSONLocalizer()
        {
            _jsonLocalizer = new JsonStringLocalizer(Options.Create<JsonLocalizationOptions>(new JsonLocalizationOptions()
            {
                DefaultCulture = new CultureInfo("fr-FR"),
                ResourcesPath = "Resources",
                Caching = _cach2,
                AssemblyHelper = new AssemblyHelper("AspNetCore.Localizer.Json.Benchmark"),
                IgnoreJsonErrors = true,
            }));
        }

        [Benchmark(Baseline = true)]
        public string Localizer()
        {
            return SharedResources.BaseName1;
        }

        [Benchmark]
        public string JsonLocalizer()
        {
            return _jsonLocalizer.GetString("BaseName1").Value;
        }

        [Benchmark]
        public string JsonLocalizerWithCreation()
        {
            var localizer = new JsonStringLocalizer(Options.Create(new JsonLocalizationOptions()
            {
                DefaultCulture = new CultureInfo("fr-FR"),
                ResourcesPath = "i18n",
                SupportedCultureInfos = new System.Collections.Generic.HashSet<CultureInfo>()
                {
                    new CultureInfo("fr-FR"),
                    new CultureInfo("en-US"),
                },
                LocalizationMode = LocalizationMode.I18n,
                AssemblyHelper = new AssemblyHelper("AspNetCore.Localizer.Json.Benchmark"),
            }));

            return localizer.GetString("BaseName1");
        }
        
        [Benchmark]
        public string I18nJsonLocalizerWithCreation()
        {
            var localizer = new JsonStringLocalizer(Options.Create(new JsonLocalizationOptions()
            {
                DefaultCulture = new CultureInfo("fr-FR"),
                ResourcesPath = "Resources",
                SupportedCultureInfos = new System.Collections.Generic.HashSet<CultureInfo>()
                {
                    new CultureInfo("fr-FR"),
                    new CultureInfo("en-US"),
                },
                AssemblyHelper = new AssemblyHelper("AspNetCore.Localizer.Json.Benchmark")
            }));
        
            return localizer.GetString("BaseName1");
        }
        
        [Benchmark]
        public string JsonLocalizerWithCreationAndExternalMemoryCache()
        {
            JsonStringLocalizer localizer = new JsonStringLocalizer(Options.Create(new JsonLocalizationOptions()
            {
                DefaultCulture = new CultureInfo("fr-FR"),
                ResourcesPath = "Resources",
                SupportedCultureInfos = new System.Collections.Generic.HashSet<CultureInfo>()
                {
                    new CultureInfo("fr-FR"),
                    new CultureInfo("en-US"),
                },
                Caching = _cach,
                AssemblyHelper = new AssemblyHelper("AspNetCore.Localizer.Json.Benchmark")
            }));
        
            return localizer.GetString("BaseName1");
        }
        
        [Benchmark]
        public string JsonLocalizerDefaultCultureValue()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("pt-PT");
            return _jsonLocalizer.GetString("BaseName1").Value;
        }
        
        [Benchmark]
        public string MicrosoftLocalizerDefaultCultureValue()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("pt-PT");
            return SharedResources.BaseName1;
        }

    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            _ = BenchmarkRunner.Run<BenchmarkJSONLocalizer>();
        }
    }
}
