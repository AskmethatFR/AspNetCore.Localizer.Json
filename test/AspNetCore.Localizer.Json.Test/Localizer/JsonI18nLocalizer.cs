using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using AspNetCore.Localizer.Json.JsonOptions;
using AspNetCore.Localizer.Json.Localizer;
using AspNetCore.Localizer.Json.Test.Helpers;
using Microsoft.Extensions.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AspNetCore.Localizer.Json.Test.Localizer
{
    [TestClass]
    public class JsonI18nLocalizer
    {
        private JsonLocalizationOptions _jsonLocalizationOptions;
        [TestInitialize]
        public void Init()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            
            _jsonLocalizationOptions = new JsonLocalizationOptions()
            {
                DefaultCulture = new CultureInfo("fr-FR"),
                SupportedCultureInfos = new HashSet<CultureInfo>()
                {
                    new CultureInfo("fr-FR"),
                    new CultureInfo("en-US")
                },
                ResourcesPath = $"i18n",
                LocalizationMode = LocalizationMode.I18n,
                AssemblyHelper = new AssemblyStub(Assembly.GetExecutingAssembly())
            };

        }
        
        [TestMethod]
        public void I18n_GetNameTranslation_ShouldBeFrenchTranslation()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");

            // Arrange           
            JsonStringLocalizer localizer = JsonStringLocalizerHelperFactory.Create(_jsonLocalizationOptions);

            var result = localizer.GetString("Name");

            Assert.AreEqual("Nom",result.Value);
        }
        
        [TestMethod]
        public void I18n_GetColorTranslation_ShouldBeUSTranslation()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");

            // Arrange           
            JsonStringLocalizer localizer = JsonStringLocalizerHelperFactory.Create(_jsonLocalizationOptions);

            var result = localizer.GetString("Color");

            Assert.AreEqual("Color",result.Value);
        }
        
        [TestMethod]
        public void I18n_GetNameTranslation_ShouldBeFrenchThenUSTranslation()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");

            // Arrange           
           JsonStringLocalizer localizer = JsonStringLocalizerHelperFactory.Create(_jsonLocalizationOptions);

            var frResult = localizer.GetString("Name");

            Assert.AreEqual("Nom",frResult.Value);
            
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            var usResult = localizer.GetString("Name");

            Assert.AreEqual("Nom",frResult.Value);
        }
        
        [TestMethod]
        public void I18n_Should_Read_Base_NotFound()
        {
            // Arrange
            JsonStringLocalizer localizer = JsonStringLocalizerHelperFactory.Create(_jsonLocalizationOptions);
            LocalizedString result = localizer.GetString("Nop");

            Assert.AreEqual("Nop", result);

        }
        
        [TestMethod]
        public void I18n_Should_ReadHirarchicalValue()
        {
            // Arrange
            JsonStringLocalizer localizer = JsonStringLocalizerHelperFactory.Create(_jsonLocalizationOptions);
            LocalizedString result = localizer.GetString("Restricted.Sentence");

            Assert.AreEqual("You are not available is this area", result);

        }
    }
}