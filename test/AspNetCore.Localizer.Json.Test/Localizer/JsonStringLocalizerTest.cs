﻿using AspNetCore.Localizer.Json.Test.Helpers;
using Microsoft.Extensions.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;
using System.Linq;
using AspNetCore.Localizer.Json.JsonOptions;
using AspNetCore.Localizer.Json.Localizer;


namespace AspNetCore.Localizer.Json.Test.Localizer
{
    [TestClass]
    public class JsonStringLocalizerTest
    {
        [TestInitialize]
        public void Init()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
        }

        [TestMethod]
        public void Should_Read_Base_Name1()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");

            // Arrange           
            JsonStringLocalizer localizer = JsonStringLocalizerHelperFactory.Create(new JsonLocalizationOptions()
            {
                DefaultCulture = new CultureInfo("fr-FR")
            });

            LocalizedString result = localizer.GetString("BaseName1");

            Assert.AreEqual("Mon Nom de Base 1", result);
        }

        [TestMethod]
        public void Should_Read_Base_NotFound()
        {
            // Arrange
            JsonStringLocalizer localizer = JsonStringLocalizerHelperFactory.Create(new JsonLocalizationOptions()
            {
                DefaultCulture = new CultureInfo("en-US")
            });
            LocalizedString result = localizer.GetString("Nop");

            Assert.AreEqual("Nop", result);

        }

        [TestMethod]
        public void Should_Read_Base_UseDefault()
        {
            // Arrange
            JsonStringLocalizer localizer = JsonStringLocalizerHelperFactory.Create(new JsonLocalizationOptions()
            {
                DefaultCulture = new CultureInfo("en-US")
            });

            LocalizedString result = localizer.GetString("NoFrench");

            Assert.AreEqual("No more french", result);
        }

        [TestMethod]
        public void Should_Read_Base_Name1_US()
        {
            // Arrange
            JsonStringLocalizer localizer = JsonStringLocalizerHelperFactory.Create(new JsonLocalizationOptions()
            {
                DefaultCulture = new CultureInfo("en-US")
            });

            LocalizedString result = localizer.GetString("BaseName1");

            Assert.AreEqual("My Base Name 1", result);
        }

        [TestMethod]
        public void Should_Read_Default_Name1_US()
        {
            // Arrange
            JsonStringLocalizer localizer = JsonStringLocalizerHelperFactory.Create(new JsonLocalizationOptions()
            {
                DefaultCulture = new CultureInfo("en-US"),
                SupportedCultureInfos = new System.Collections.Generic.HashSet<CultureInfo>()
                {
                     new CultureInfo("de-DE")
                }
            });
            LocalizedString result = localizer.GetString("BaseName1");

            Assert.AreEqual("My Base Name 1", result);
        }

        [TestMethod]
        public void Should_Read_CaseInsensitive_CultureName()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");

            JsonStringLocalizer localizer = JsonStringLocalizerHelperFactory.Create(new JsonLocalizationOptions()
            {
                DefaultCulture = new CultureInfo("en-US"),
                SupportedCultureInfos = new System.Collections.Generic.HashSet<CultureInfo>()
                {
                     new CultureInfo("fr-FR")
                }
            });

            LocalizedString result = localizer.GetString("CaseInsensitiveCultureName");
            Assert.AreEqual("French", result);
        }

        [TestMethod]
        public void Should_Read_CaseInsensitive_UseDefault()
        {
            JsonStringLocalizer localizer = JsonStringLocalizerHelperFactory.Create(new JsonLocalizationOptions()
            {
                DefaultCulture = new CultureInfo("en-US"),
                SupportedCultureInfos = new System.Collections.Generic.HashSet<CultureInfo>()
                {
                     new CultureInfo("de-DE")
                }
            });

            LocalizedString result = localizer.GetString("CaseInsensitiveCultureName");
            Assert.AreEqual("US English", result);
        }

        [TestMethod]
        public void Should_GetAllStrings_ByCaseInsensitiveCultureName()
        {
            
            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");

            JsonStringLocalizer localizer = JsonStringLocalizerHelperFactory.Create(new JsonLocalizationOptions()
            {
                DefaultCulture = new CultureInfo("fr-FR")
            });

            string[] expected = new[] {
                "Mon Nom de Base 1",
                "Mon Nom de Base 2",
                "French"
            };
            LocalizedString result = localizer.GetString("BaseName1");

            string[] results = localizer.GetAllStrings().Select(x => x.Value).ToArray();
            CollectionAssert.AreEquivalent(expected, results);
        }

        [TestMethod]
        public void Should_SwitchCulture_WithoutReloadingLocalizer()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");

            JsonStringLocalizer localizer = JsonStringLocalizerHelperFactory.Create(new JsonLocalizationOptions()
            {
                DefaultCulture = new CultureInfo("fr-FR")
            });

            LocalizedString result = localizer.GetString("BaseName1");

            Assert.AreEqual("Mon Nom de Base 1", result);

            CultureInfo.CurrentUICulture = new CultureInfo("en-US");

            result = localizer.GetString("BaseName1");

            Assert.AreEqual("My Base Name 1", result);
        }
        
        [TestMethod]
        public void Should_SwitchCulture_FromWithCulture()
        {
            JsonStringLocalizer localizer = JsonStringLocalizerHelperFactory.Create(new JsonLocalizationOptions()
            {
                DefaultCulture = new CultureInfo("fr-FR")
            });

            localizer.WithCulture(new CultureInfo("en-US"));
            var result = localizer.GetString("BaseName1");

            Assert.AreEqual("My Base Name 1", result);
        }
    }
}
