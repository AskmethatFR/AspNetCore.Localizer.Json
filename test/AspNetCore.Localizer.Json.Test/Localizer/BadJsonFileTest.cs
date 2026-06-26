using AspNetCore.Localizer.Json.Localizer;
using AspNetCore.Localizer.Json.Test.Helpers;
using Microsoft.Extensions.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json;
using AspNetCore.Localizer.Json.JsonOptions;

namespace AspNetCore.Localizer.Json.Test.Localizer
{
    [TestClass]
    public class BadJsonJsonFileTest
    {
        private JsonStringLocalizer localizer = null;
        public void InitLocalizer(CultureInfo cultureInfo)
        {
            CultureInfo.CurrentUICulture = cultureInfo;
            localizer = JsonStringLocalizerHelperFactory.Create(new JsonLocalizationOptions()
            {
                DefaultCulture = new CultureInfo("en-US"),
                SupportedCultureInfos = new System.Collections.Generic.HashSet<CultureInfo>()
                {
                     new CultureInfo("fr-FR")
                },
                ResourcesPath = $"path",
                IgnoreJsonErrors = true,
                AssemblyHelper = new AssemblyStub(Assembly.GetCallingAssembly()),
            }); ;
        }

        [TestMethod]
        public void TestReadName1_AbsolutePath_StringLocation()
        {
            InitLocalizer(new CultureInfo("fr-FR"));

            LocalizedString result = localizer.GetString("Name1");            

            Assert.AreEqual("Mon Nom 1", result);

            LocalizedString result2 = localizer.GetString("Name3");

            Assert.AreEqual("Name3", result2);
        }

        [TestMethod]
        public void TestReadName1_RelativePath_NullStringLocation()
        {
            InitLocalizer(new CultureInfo("fr-FR"));

            LocalizedString result = localizer.GetString("");

            Assert.AreEqual("", result);
        }

        [TestMethod]
        public void MalformedJson_IgnoreErrorsTrue_DoesNotThrow()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");

            var localizer = JsonStringLocalizerHelperFactory.Create(new JsonLocalizationOptions()
            {
                DefaultCulture = new CultureInfo("en-US"),
                SupportedCultureInfos = new System.Collections.Generic.HashSet<CultureInfo>
                {
                    new CultureInfo("en-US")
                },
                ResourcesPath = "badjson",
                IgnoreJsonErrors = true,
                UseEmbeddedResources = false,
            });

            var result = localizer.GetString("Name1");
            Assert.AreEqual("Name1", result);
        }

        [TestMethod]
        public void MalformedJson_IgnoreErrorsFalse_ThrowsJsonException()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");

            var localizer = JsonStringLocalizerHelperFactory.Create(new JsonLocalizationOptions()
            {
                DefaultCulture = new CultureInfo("en-US"),
                SupportedCultureInfos = new System.Collections.Generic.HashSet<CultureInfo>
                {
                    new CultureInfo("en-US")
                },
                ResourcesPath = "badjson",
                IgnoreJsonErrors = false,
                UseEmbeddedResources = false,
            });

            Assert.ThrowsException<JsonException>(() => localizer.GetString("Name1"));
        }
    }
}