using AspNetCore.Localizer.Json.Localizer;
using AspNetCore.Localizer.Json.Test.Helpers;
using Microsoft.Extensions.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using AspNetCore.Localizer.Json.JsonOptions;
using LocalizedString = Microsoft.Extensions.Localization.LocalizedString;
using System.IO;

namespace AspNetCore.Localizer.Json.Test.Localizer
{
    [TestClass]
    public class DiagnosticTranslationTests
    {
        [TestInitialize]
        public void Init()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
        }

        [TestMethod]
        public void Should_Blank_Name()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");

            // Arrange           
            JsonStringLocalizer localizer = JsonStringLocalizerHelperFactory.Create(new JsonLocalizationOptions()
            {
                DefaultCulture = new CultureInfo("fr-FR"),
                LocalizerDiagnosticMode = true
            });

            LocalizedString result = localizer.GetString("BaseName1");

            Assert.AreEqual("XXXXXXXXX", result);
        }
    }
}
