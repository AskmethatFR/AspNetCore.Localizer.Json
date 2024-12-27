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
    public class JsonStringLocalizerCustomResourcesTest
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
                ResourcesPath = "json_files",
                AssemblyHelper = new AssemblyStub(Assembly.GetExecutingAssembly())
            });
            
        }

        [TestMethod]
        public void Should_Read_Name1()
        {
            // Arrange
            InitLocalizer(new CultureInfo("fr-FR"));

            LocalizedString result = localizer.GetString("Name1");

            Assert.AreEqual("Mon Nom 1", result);
        }

        [TestMethod]
        public void Should_Read_NotFound()
        {
            // Arrange
            InitLocalizer(new CultureInfo("fr-FR"));

            LocalizedString result = localizer.GetString("Nop");

            Assert.AreEqual("Nop", result);
        }

        [TestMethod]
        public void Should_Read_Name1_US()
        {
            // Arrange
            InitLocalizer(new CultureInfo("en-US"));

            LocalizedString result = localizer.GetString("Name1");

            Assert.AreEqual("My Name 1", result);
        }
    }
}
