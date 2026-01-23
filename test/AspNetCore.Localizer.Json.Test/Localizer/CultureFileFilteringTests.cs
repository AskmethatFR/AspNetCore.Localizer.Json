using AspNetCore.Localizer.Json.JsonOptions;
using AspNetCore.Localizer.Json.Localizer;
using AspNetCore.Localizer.Json.Test.Helpers;
using Microsoft.Extensions.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AspNetCore.Localizer.Json.Test.Localizer
{
    [TestClass]
    public class CultureFileFilteringTests
    {
        [TestMethod]
        public void Should_Not_Use_Unrelated_Culture_Files()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");

            JsonStringLocalizer localizer = JsonStringLocalizerHelperFactory.Create(new JsonLocalizationOptions
            {
                DefaultCulture = new CultureInfo("en-US"),
                SupportedCultureInfos = [ new CultureInfo("en-US") ],
                ResourcesPath = "cultureFiltering",
                LocalizationMode = LocalizationMode.I18n,
                UseEmbeddedResources = false,
                AssemblyHelper = new AssemblyStub(Assembly.GetCallingAssembly())
            });

            LocalizedString englishOnly = localizer.GetString("EnglishOnly");
            Assert.AreEqual("Hello from en-US", englishOnly.Value);
            Assert.IsFalse(englishOnly.ResourceNotFound);

            LocalizedString oldCultureOnly = localizer.GetString("OldCultureOnly");
            Assert.AreEqual("OldCultureOnly", oldCultureOnly.Value);
            Assert.IsTrue(oldCultureOnly.ResourceNotFound);
        }

        [TestMethod]
        public void Should_Filter_I18n_Files_For_Current_Culture()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
            var options = new JsonLocalizationOptions
            {
                DefaultCulture = new CultureInfo("en-US"),
                SupportedCultureInfos = [new CultureInfo("fr-FR")],
                ResourcesPath = "i18nLeak",
                LocalizationMode = LocalizationMode.I18n,
                UseEmbeddedResources = true,
                AssemblyHelper = new AssemblyStub(Assembly.GetExecutingAssembly())
            };

            var generatorType = typeof(JsonStringLocalizer).Assembly.GetType("AspNetCore.Localizer.Json.Localizer.Modes.LocalizationI18NModeGenerator");
            var generator = Activator.CreateInstance(generatorType, nonPublic: true);
            var construct = generatorType?.GetMethod("ConstructLocalization", BindingFlags.Instance | BindingFlags.Public);

            var resourceNames = Assembly.GetExecutingAssembly()
                .GetManifestResourceNames()
                .Where(n => n.Contains("i18nLeak.localization", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            var localization = construct?.Invoke(generator, new object[] { resourceNames, new CultureInfo("fr-FR"), options }) as IDictionary;

            Assert.IsNotNull(localization);
            Assert.AreEqual(1, localization.Count);

            var greetingEntry = localization["Greeting"];
            var valueProp = greetingEntry?.GetType().GetProperty("Value");
            Assert.AreEqual("Bonjour", valueProp?.GetValue(greetingEntry));
        }
    }
}
