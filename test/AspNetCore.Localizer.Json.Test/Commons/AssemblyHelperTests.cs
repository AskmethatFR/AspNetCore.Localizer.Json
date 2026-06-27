using System.Linq;
using System.Reflection;
using AspNetCore.Localizer.Json.Commons;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AspNetCore.Localizer.Json.Test.Commons;

[TestClass]
public class AssemblyHelperTests
{
    [TestMethod]
    public void GetAssemblies_Default_ReturnsSingleElementList()
    {
        var helper = new AssemblyHelper();
        var assemblies = helper.GetAssemblies();

        Assert.AreEqual(1, assemblies.Count);
        Assert.IsNotNull(assemblies[0]);
    }

    [TestMethod]
    public void GetAssemblies_WithAssembly_ReturnsSingleElementList()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var helper = new AssemblyHelper(assembly);
        var assemblies = helper.GetAssemblies();

        Assert.AreEqual(1, assemblies.Count);
        Assert.AreEqual(assembly, assemblies[0]);
    }

    [TestMethod]
    public void GetAssemblies_WithAssemblyName_ReturnsSingleElementList()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var helper = new AssemblyHelper(assembly.GetName().Name);
        var assemblies = helper.GetAssemblies();

        Assert.AreEqual(1, assemblies.Count);
        Assert.IsNotNull(assemblies[0]);
    }
}