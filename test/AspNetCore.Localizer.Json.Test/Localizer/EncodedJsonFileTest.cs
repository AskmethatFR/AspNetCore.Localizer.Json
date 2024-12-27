using System.Collections.Generic;
using AspNetCore.Localizer.Json.Test.Helpers;
using Microsoft.Extensions.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;
using System.Reflection;
using System.Text;
using AspNetCore.Localizer.Json.JsonOptions;
using AspNetCore.Localizer.Json.Localizer;

namespace AspNetCore.Localizer.Json.Test.Localizer
{
    [TestClass]
    public class EncodedJsonFileTest
    {

        [TestMethod]
        public void TestReadName1_ISOEncoding()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
            var localizer = InitJsonStringLocalizer(new System.Collections.Generic.HashSet<CultureInfo>()
            {
                new CultureInfo("fr-FR")
            });

            LocalizedString result = localizer.GetString("Name1");

            Assert.AreEqual("Mon Nom 1", result);
        }

        private static JsonStringLocalizer InitJsonStringLocalizer(HashSet<CultureInfo> supportedCultureInfos)
        {
            JsonStringLocalizer localizer = JsonStringLocalizerHelperFactory.Create(new JsonLocalizationOptions()
            {
                DefaultCulture = new CultureInfo("en-US"),
                SupportedCultureInfos = supportedCultureInfos,
                ResourcesPath = "encoding",
                FileEncoding = Encoding.GetEncoding("ISO-8859-1"),
                AssemblyHelper = new AssemblyStub(Assembly.GetCallingAssembly())
            });
            return localizer;
        }

        [TestMethod]
        public void TestReadName1_ISOEncoding_SpecialChar()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("pt-PT");
            JsonStringLocalizer localizer = InitJsonStringLocalizer(new HashSet<CultureInfo>()
            {
                new CultureInfo("fr-FR"),
                new CultureInfo("pt-PT")
            });

            LocalizedString result = localizer.GetString("Name1");

            Assert.AreEqual("Eu so joão", result);
        }

    }
}
