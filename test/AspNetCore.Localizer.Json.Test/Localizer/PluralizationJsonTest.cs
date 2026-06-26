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
    public class PluralizationJsonTest
    {
        private JsonStringLocalizer localizer = null;
        public void InitLocalizer(char seperator = '|')
        {
            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");

            localizer = JsonStringLocalizerHelperFactory.Create(new JsonLocalizationOptions()
            {
                DefaultCulture = new CultureInfo("en-US"),
                SupportedCultureInfos = new System.Collections.Generic.HashSet<CultureInfo>()
                {
                     new CultureInfo("fr-FR"),
                },
                ResourcesPath = "pluralization",
                PluralSeparator = seperator,
                AssemblyHelper = new AssemblyStub(Assembly.GetCallingAssembly())
            });
        }


        [TestMethod]
        public void Should_Be_Singular_Users()
        {
            // Arrange
            InitLocalizer();

            LocalizedString result = localizer.GetString("PluralUser", false);

            Assert.AreEqual("Utilisateur", result);
        }

        [TestMethod]
        public void Should_Be_Plural_Users()
        {
            InitLocalizer();

            LocalizedString result = localizer.GetString("PluralUser", true);

            Assert.AreEqual("Utilisateurs", result);
        }

        [TestMethod]
        public void Should_Be_PluralWithNoSeperator_ShowDefault()
        {
            InitLocalizer();

            LocalizedString result = localizer.GetString("PluralUserFailed", true);

            Assert.AreEqual("Utilisateurs", result);
        }

        [TestMethod]
        public void Should_Be_Singular_Users_Custom()
        {
            // Arrange
            InitLocalizer('#');

            LocalizedString result = localizer.GetString("CustomPluralUser", false);

            Assert.AreEqual("Utilisateur", result);
        }

        [TestMethod]
        public void Should_Be_Plural_Users_Custom()
        {
            // Arrange
            InitLocalizer('#');

            LocalizedString result = localizer.GetString("CustomPluralUser", true);

            Assert.AreEqual("Utilisateurs", result);
        }

        [TestMethod]
        public void Should_Be_Plural_NotFound()
        {
            // Arrange
            InitLocalizer();

            LocalizedString result = localizer.GetString("NotFound", true);

            Assert.AreEqual("NotFound", result);
        }

        [TestMethod]
        public void GetString_NonBoolLastArgument_DoesNotTriggerPlural()
        {
            InitLocalizer();

            var result = localizer.GetString("PluralUser", 42);

            Assert.AreEqual("Utilisateur|Utilisateurs", result);
        }

        [TestMethod]
        public void GetString_NoArguments_ReturnsFullValue()
        {
            InitLocalizer();

            var result = localizer.GetString("PluralUser");

            Assert.AreEqual("Utilisateur|Utilisateurs", result);
        }

        [TestMethod]
        public void GetString_BoolLastArgument_FormatStringWithSeparator_TriggersPlural()
        {
            InitLocalizer();

            var singular = localizer.GetString("PluralUser", false);
            var plural = localizer.GetString("PluralUser", true);

            Assert.AreEqual("Utilisateur", singular);
            Assert.AreEqual("Utilisateurs", plural);
        }

        [TestMethod]
        public void GetPlural_DifferentCounts_ReturnsExpectedResults()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");

            var localizer = JsonStringLocalizerHelperFactory.Create(new JsonLocalizationOptions()
            {
                DefaultCulture = new CultureInfo("en-US"),
                SupportedCultureInfos = new System.Collections.Generic.HashSet<CultureInfo>
                {
                    new CultureInfo("en-US"),
                    new CultureInfo("fr-FR")
                },
                ResourcesPath = "i18nPluralization",
                LocalizationMode = LocalizationMode.I18n,
                AssemblyHelper = new AssemblyStub(typeof(PluralizationJsonTest).Assembly)
            });

            Assert.AreEqual("1 User", localizer.GetPlural("Title", 1).Value);
            Assert.AreEqual("2 Users", localizer.GetPlural("Title", 2).Value);
            Assert.AreEqual("0 Users", localizer.GetPlural("Title", 0).Value);
        }
    }
}
