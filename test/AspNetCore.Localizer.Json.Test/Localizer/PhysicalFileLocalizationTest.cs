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
    public class PhysicalFileLocalizationTest
    {
        [TestMethod]
        public void Should_Read_From_Physical_File()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");

            JsonStringLocalizer localizer = JsonStringLocalizerHelperFactory.Create(new JsonLocalizationOptions()
            {
                DefaultCulture = new CultureInfo("en-US"),
                SupportedCultureInfos = [ new CultureInfo("en-US") ],
                ResourcesPath = "physical",
                UseEmbeddedResources = false
            });

            LocalizedString result = localizer.GetString("BaseName1");
            Assert.AreEqual("My Base Name 1", result.Value);
        }
    }
}
