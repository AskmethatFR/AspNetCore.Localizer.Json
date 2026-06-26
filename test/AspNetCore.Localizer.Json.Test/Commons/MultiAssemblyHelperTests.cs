using System;
using System.Linq;
using System.Reflection;
using AspNetCore.Localizer.Json.Commons;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AspNetCore.Localizer.Json.Test.Commons;

[TestClass]
public class MultiAssemblyHelperTests
{
    [TestMethod]
    public void GetAssemblies_WithAssemblies_ReturnsInOrder()
    {
        var asm1 = Assembly.GetExecutingAssembly();
        var asm2 = typeof(System.Text.Json.JsonSerializer).Assembly;
        var helper = new MultiAssemblyHelper(asm1, asm2);
        var assemblies = helper.GetAssemblies();

        Assert.AreEqual(2, assemblies.Count);
        Assert.AreEqual(asm1, assemblies[0]);
        Assert.AreEqual(asm2, assemblies[1]);
    }

    [TestMethod]
    public void GetAssembly_ReturnsFirstAssembly()
    {
        var asm1 = Assembly.GetExecutingAssembly();
        var asm2 = typeof(System.Text.Json.JsonSerializer).Assembly;
        var helper = new MultiAssemblyHelper(asm1, asm2);

        Assert.AreEqual(asm1, helper.GetAssembly());
    }

    [TestMethod]
    public void GetAssemblies_WithAssemblyNames_LoadsAssemblies()
    {
        var asm1 = Assembly.GetExecutingAssembly();
        var helper = new MultiAssemblyHelper(asm1.GetName().Name);
        var assemblies = helper.GetAssemblies();

        Assert.AreEqual(1, assemblies.Count);
        Assert.AreEqual(asm1.GetName().Name, assemblies[0].GetName().Name);
    }

    [TestMethod]
    public void GetAssemblies_WithThreeAssemblies_ReturnsAll()
    {
        var asm1 = Assembly.GetExecutingAssembly();
        var asm2 = typeof(System.Text.Json.JsonSerializer).Assembly;
        var asm3 = typeof(System.Linq.Enumerable).Assembly;
        var helper = new MultiAssemblyHelper(asm1, asm2, asm3);

        Assert.AreEqual(3, helper.GetAssemblies().Count);
    }

    [TestMethod]
    public void GetAssemblies_ReadOnly_ReturnsSnapshot()
    {
        var asm1 = Assembly.GetExecutingAssembly();
        var helper = new MultiAssemblyHelper(asm1);
        var assemblies = helper.GetAssemblies();

        Assert.IsInstanceOfType(assemblies, typeof(System.Collections.Generic.IReadOnlyList<Assembly>));
    }

    [TestMethod]
    public void Constructor_NullAssemblies_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(() => new MultiAssemblyHelper(default(Assembly[])));
    }

    [TestMethod]
    public void Constructor_NullAssemblyNames_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(() => new MultiAssemblyHelper(default(string[])));
    }

    [TestMethod]
    public void Constructor_EmptyAssemblies_ThrowsArgumentException()
    {
        Assert.ThrowsException<ArgumentException>(() => new MultiAssemblyHelper(Array.Empty<Assembly>()));
    }

    [TestMethod]
    public void Constructor_EmptyAssemblyNames_ThrowsArgumentException()
    {
        Assert.ThrowsException<ArgumentException>(() => new MultiAssemblyHelper(Array.Empty<string>()));
    }
}