using AspNetCore.Localizer.Json.Localizer;
using AspNetCore.Localizer.Json.Test.Helpers;
using Microsoft.Extensions.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;
using System.Reflection;
using AspNetCore.Localizer.Json.JsonOptions;

namespace AspNetCore.Localizer.Json.Test.Localizer
{
    [TestClass]
    public class StringFactoryCreateJsonFileTest
    {
        private JsonStringLocalizer localizer = null;
        public void InitLocalizer(CultureInfo cultureInfo, string baseName = null)
        {
            CultureInfo.CurrentUICulture = cultureInfo;
            localizer = JsonStringLocalizerHelperFactory.Create(new JsonLocalizationOptions()
            {
                DefaultCulture = new CultureInfo("en-US"),
                SupportedCultureInfos = new System.Collections.Generic.HashSet<CultureInfo>()
                {
                     new CultureInfo("fr-FR")
                },
                ResourcesPath = "factory",
                AssemblyHelper = new AssemblyStub(Assembly.GetExecutingAssembly())
            }, baseName);
        }

        [TestMethod]
        public void TestReadName1_StringLocation()
        {
            InitLocalizer(new CultureInfo("fr-FR"));
            LocalizedString result = localizer.GetString("Name1");

            Assert.AreEqual("Mon Nom 1", result);
        }

        [TestMethod]
        public void TestReadName1_BaseName_StringLocation()
        {
            InitLocalizer(new CultureInfo("fr-FR"), "base");
            LocalizedString result = localizer.GetString("Name3");

            Assert.AreEqual("Mon Nom 3", result);

            result = localizer.GetString("Name4");

            Assert.AreEqual("Mon Nom 4", result);

            result = localizer.GetString("Name1");

            Assert.IsTrue(result.ResourceNotFound);
        }

    }
}