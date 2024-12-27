using System.Globalization;
using System.Reflection;
using AspNetCore.Localizer.Json.JsonOptions;
using AspNetCore.Localizer.Json.Localizer;
using AspNetCore.Localizer.Json.Test.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AspNetCore.Localizer.Json.Test.Localizer;

[TestClass]
public class InterporlationTests
{
    private const string InterpolationKey = "This workspace has consumed {0}% of its budget. It is recommended to monitor the usage as the workspace resources will be deleted if the budget is exceeded.";
    private JsonStringLocalizer localizer = null;
    public void InitLocalizer(string currentCulture = "en-US")
    {
        CultureInfo.CurrentUICulture = new CultureInfo(currentCulture);

        localizer = JsonStringLocalizerHelperFactory.Create(new JsonLocalizationOptions()
        {
            DefaultCulture = new CultureInfo("en-US"),
            SupportedCultureInfos = new System.Collections.Generic.HashSet<CultureInfo>()
            {
                new CultureInfo("fr-FR"),
                new CultureInfo("es-US"),
            },
            ResourcesPath = "interpolation",
            LocalizationMode = LocalizationMode.I18n,
            AssemblyHelper = new AssemblyStub(Assembly.GetCallingAssembly())
        });
    }
    
    [TestMethod]
    public void TestEnglishInterpolation()
    {
        InitLocalizer();

        var result = localizer[InterpolationKey, 50];

        var expected = "This workspace has consumed 50% of its budget. It is recommended to monitor the usage as the workspace resources will be deleted if the budget is exceeded.";
        Assert.AreEqual(expected, result.Value);
    }
    
    [TestMethod]
    public void TestFrenchInterpolation()
    {
        InitLocalizer("fr-FR");

        
        var result = localizer[InterpolationKey, 50];

        var expected =
            "Cet espace de travail a consommé 50% de son budget. Il est recommandé de surveiller l'utilisation, car les ressources de l'espace de travail seront supprimées si le budget est dépassé.";
        Assert.AreEqual(expected, result.Value);
        
        
    }
    
}