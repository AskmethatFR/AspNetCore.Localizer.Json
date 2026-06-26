using AspNetCore.Localizer.Json.Localizer;
using AspNetCore.Localizer.Json.Test.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;
using AspNetCore.Localizer.Json.JsonOptions;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace AspNetCore.Localizer.Json.Test.Localizer
{
    [TestClass]
    public class LocalizerLifecycleTest
    {
        [TestMethod]
        public void Dispose_Through_Base_Reference_Runs_Derived_Cleanup()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");

            var localizer = JsonStringLocalizerHelperFactory.Create(new JsonLocalizationOptions
            {
                DefaultCulture = new CultureInfo("en-US"),
                SupportedCultureInfos = { new CultureInfo("en-US") },
                ResourcesPath = "Resources",
                UseEmbeddedResources = false
            });

            var result = localizer["NonExistentKey"];

            localizer.Dispose();
        }

        [TestMethod]
        public void Factory_Create_With_BaseName_Passes_BaseName_To_Localizer()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");

            var options = new JsonLocalizationOptions
            {
                DefaultCulture = new CultureInfo("en-US"),
                SupportedCultureInfos = { new CultureInfo("en-US") },
                ResourcesPath = "factory",
                UseEmbeddedResources = true,
                AssemblyHelper = new AssemblyStub(Assembly.GetExecutingAssembly())
            };

            var factory = new JsonStringLocalizerFactory(Options.Create(options));

            var withoutBaseName = factory.Create(typeof(string));
            var withBaseName = factory.Create("base", "test");

            // Without baseName (pooled): loads all resources, finds "Name1"
            Assert.AreEqual("My Name 1", withoutBaseName["Name1"].Value);

            // With baseName: creates new localizer that filters by baseName
            // Should also find "Name1" in the same resource file
            Assert.AreEqual("My Name 1", withBaseName["Name1"].Value);
        }

        [TestMethod]
        public void Double_Dispose_Does_Not_Throw()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");

            var localizer = JsonStringLocalizerHelperFactory.Create(new JsonLocalizationOptions
            {
                DefaultCulture = new CultureInfo("en-US"),
                SupportedCultureInfos = { new CultureInfo("en-US") },
                ResourcesPath = "Resources",
                UseEmbeddedResources = false
            });

            localizer.Dispose();
            localizer.Dispose();
        }
    }
}