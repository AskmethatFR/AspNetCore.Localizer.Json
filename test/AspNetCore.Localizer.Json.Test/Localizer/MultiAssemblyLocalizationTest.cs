#nullable enable
using System.Globalization;
using System.Reflection;
using AspNetCore.Localizer.Json.Commons;
using AspNetCore.Localizer.Json.JsonOptions;
using AspNetCore.Localizer.Json.Localizer;
using AspNetCore.Localizer.Json.Test.Helpers;
using AspNetCore.Localizer.Json.TestMultiAssembly;
using Microsoft.Extensions.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AspNetCore.Localizer.Json.Test.Localizer
{
    [TestClass]
    public class MultiAssemblyLocalizationTest
    {
        private static JsonStringLocalizer CreateLocalizer(string resourcesPath, string[]? additionalPaths = null)
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            return JsonStringLocalizerHelperFactory.Create(new JsonLocalizationOptions
            {
                DefaultCulture = new CultureInfo("en-US"),
                ResourcesPath = resourcesPath,
                AdditionalResourcesPaths = additionalPaths ?? System.Array.Empty<string>(),
                AssemblyHelper = new MultiAssemblyHelper(
                    Assembly.GetExecutingAssembly(),
                    typeof(MultiAssemblyBResourcesMarker).Assembly)
            });
        }

        [TestMethod]
        public void TwoAssemblies_ResourcesFromBothDiscovered()
        {
            var localizer = CreateLocalizer("multiAssemblyA", new[] { "multiAssemblyB" });

            LocalizedString result1 = localizer["Key1"];
            LocalizedString result2 = localizer["Key2"];

            Assert.AreEqual("Value1 from AssemblyA", result1.Value);
            Assert.AreEqual("Value2 from AssemblyB", result2.Value);
        }

        [TestMethod]
        public void TwoAssemblies_FirstAssemblyWinsOnOverlap()
        {
            var localizer = CreateLocalizer("multiAssemblyA", new[] { "multiAssemblyB" });

            LocalizedString result = localizer["SharedKey"];

            Assert.AreEqual("Shared from AssemblyA", result.Value);
        }

        [TestMethod]
        public void TwoAssemblies_SingleResourcePath_StillFindsResource()
        {
            var localizer = CreateLocalizer("multiAssemblyB");

            LocalizedString result = localizer["Key2"];

            Assert.AreEqual("Value2 from AssemblyB", result.Value);
        }

        [TestMethod]
        public void SingleAssembly_MultiAssemblyHelper_BackwardCompatible()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            var localizer = JsonStringLocalizerHelperFactory.Create(new JsonLocalizationOptions
            {
                DefaultCulture = new CultureInfo("en-US"),
                ResourcesPath = "multiAssemblyA",
                AssemblyHelper = new MultiAssemblyHelper(Assembly.GetExecutingAssembly())
            });

            LocalizedString result = localizer["Key1"];

            Assert.AreEqual("Value1 from AssemblyA", result.Value);
        }

        [TestMethod]
        public void TwoAssemblies_NonExistentPath_FallsBackToKeyName()
        {
            var localizer = CreateLocalizer("nonExistentPath");

            LocalizedString result = localizer["SomeKey"];

            Assert.AreEqual("SomeKey", result.Value);
        }
    }
}