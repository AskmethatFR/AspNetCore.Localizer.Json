using AspNetCore.Localizer.Json.Localizer;
using AspNetCore.Localizer.Json.Test.Helpers;
using Microsoft.Extensions.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Globalization;
using AspNetCore.Localizer.Json.JsonOptions;

namespace AspNetCore.Localizer.Json.Test.Localizer
{
    [TestClass]
    public class AbsolutePathJsonFileTest
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
                ResourcesPath = $"{AppContext.BaseDirectory}/path",
                IsAbsolutePath = true
            });
        }

        [TestMethod]
        public void TestReadName1_AbsolutePath_StringLocation()
        {
            InitLocalizer(new CultureInfo("fr-FR"));

            LocalizedString result = localizer.GetString("Name1");            

            Assert.AreEqual("Mon Nom 1", result);

            LocalizedString result2 = localizer.GetString("Name3");

            Assert.IsTrue(result2.ResourceNotFound);

        }

        //[TestMethod]
        //public void TestReadName1_AbsolutePath_TypeLocalizer()
        //{
        //    InitLocalizer(new CultureInfo("fr-FR"));

        //    LocalizedString result = localizer.GetString("Name1");

        //    Assert.AreEqual("Mon Nom 1", result);
        //}

    }
}