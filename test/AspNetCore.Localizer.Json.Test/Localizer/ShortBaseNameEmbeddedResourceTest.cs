using System;
using System.Globalization;
using System.Reflection;
using AspNetCore.Localizer.Json.JsonOptions;
using AspNetCore.Localizer.Json.Localizer;
using AspNetCore.Localizer.Json.Test.Helpers;
using Microsoft.Extensions.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AspNetCore.Localizer.Json.Test.Localizer
{
    // Regression tests for issue #44: a 2-3 letter file basename (e.g. "url")
    // was mistaken for a culture code by the embedded-resource culture filter,
    // so url.json / url.fr.json were never discovered.
    [TestClass]
    public class ShortBaseNameEmbeddedResourceTest
    {
        private static JsonStringLocalizer CreateLocalizer(
            CultureInfo uiCulture,
            string resourcesPath = "i18nShortName",
            string[] additionalPaths = null)
        {
            CultureInfo.CurrentUICulture = uiCulture;
            return JsonStringLocalizerHelperFactory.Create(new JsonLocalizationOptions
            {
                DefaultCulture = new CultureInfo("en-US"),
                SupportedCultureInfos = { new CultureInfo("en-US"), new CultureInfo("fr-FR") },
                ResourcesPath = resourcesPath,
                AdditionalResourcesPaths = additionalPaths ?? Array.Empty<string>(),
                LocalizationMode = LocalizationMode.I18n,
                UseEmbeddedResources = true,
                AssemblyHelper = new AssemblyStub(Assembly.GetExecutingAssembly())
            });
        }

        [TestMethod]
        public void ShortBaseName_NeutralFile_IsDiscovered()
        {
            var localizer = CreateLocalizer(new CultureInfo("en-US"));

            LocalizedString result = localizer["UrlKey"];

            Assert.IsFalse(result.ResourceNotFound);
            Assert.AreEqual("http://en", result.Value);
        }

        [TestMethod]
        public void ShortBaseName_CultureSuffixFile_IsDiscovered()
        {
            var localizer = CreateLocalizer(new CultureInfo("fr-FR"));

            LocalizedString result = localizer["UrlKey"];

            Assert.IsFalse(result.ResourceNotFound);
            Assert.AreEqual("http://fr", result.Value);
        }

        // A short base name carrying a culture suffix that does NOT match the current
        // culture must still be filtered out (the base name is not a culture).
        [TestMethod]
        public void ShortBaseName_NonMatchingCultureSuffix_IsFiltered()
        {
            var localizer = CreateLocalizer(new CultureInfo("en-US"));

            LocalizedString result = localizer["DeOnlyKey"];

            Assert.IsTrue(result.ResourceNotFound);
        }

        // Issue #44 also reproduces when the short-base-name file lives under an
        // AdditionalResourcesPaths entry rather than the primary ResourcesPath.
        [TestMethod]
        public void ShortBaseName_UnderAdditionalResourcesPath_IsDiscovered()
        {
            var localizer = CreateLocalizer(
                new CultureInfo("en-US"),
                resourcesPath: "i18nDoesNotExist",
                additionalPaths: new[] { "i18nExtra" });

            LocalizedString result = localizer["ApiKey"];

            Assert.IsFalse(result.ResourceNotFound);
            Assert.AreEqual("https://api/en", result.Value);
        }

        // Folder convention: culture segment precedes the base name
        // (embedded "...i18nFolder.fr.localization.json"). Exercises the
        // first-segment culture branch of the resolver.
        [TestMethod]
        public void FolderConventionCulture_IsDiscovered()
        {
            var localizer = CreateLocalizer(new CultureInfo("fr-FR"), resourcesPath: "i18nFolder");

            LocalizedString result = localizer["FolderGreeting"];

            Assert.IsFalse(result.ResourceNotFound);
            Assert.AreEqual("Bonjour dossier", result.Value);
        }

        // Discriminates the folder-convention (first-segment) culture branch: a folder-culture
        // file for a NON-current culture must be filtered out. If the first-segment culture were
        // ignored, the file would fall through to "neutral" and load for every culture.
        [TestMethod]
        public void FolderConventionCulture_NonMatching_IsFiltered()
        {
            var localizer = CreateLocalizer(new CultureInfo("fr-FR"), resourcesPath: "i18nFolder");

            LocalizedString result = localizer["GermanFolderKey"];

            Assert.IsTrue(result.ResourceNotFound);
        }
    }
}
