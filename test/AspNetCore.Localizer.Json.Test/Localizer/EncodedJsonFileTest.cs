﻿using AspNetCore.Localizer.Json.Test.Helpers;
using Microsoft.Extensions.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;
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
            JsonStringLocalizer localizer = JsonStringLocalizerHelperFactory.Create(new JsonLocalizationOptions()
            {
                DefaultCulture = new CultureInfo("en-US"),
                SupportedCultureInfos = new System.Collections.Generic.HashSet<CultureInfo>()
                {
                     new CultureInfo("fr-FR")
                },
                ResourcesPath = "encoding",
                FileEncoding = Encoding.GetEncoding("ISO-8859-1")
            });

            LocalizedString result = localizer.GetString("Name1");

            Assert.AreEqual("Mon Nom 1", result);
        }

        [TestMethod]
        public void TestReadName1_ISOEncoding_SpecialChar()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("pt-PT");
            JsonStringLocalizer localizer = JsonStringLocalizerHelperFactory.Create(new JsonLocalizationOptions()
            {
                DefaultCulture = new CultureInfo("en-US"),
                SupportedCultureInfos = new System.Collections.Generic.HashSet<CultureInfo>()
                {
                     new CultureInfo("fr-FR"),
                     new CultureInfo("pt-PT")
                },
                ResourcesPath = "encoding",
                FileEncoding = Encoding.GetEncoding("ISO-8859-1")
            });

            LocalizedString result = localizer.GetString("Name1");

            Assert.AreEqual("Eu so joão", result);
        }

    }
}
