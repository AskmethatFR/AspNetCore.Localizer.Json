using System.Reflection;
using AspNetCore.Localizer.Json.Commons;

namespace AspNetCore.Localizer.Json.Test.Helpers;

public class AssemblyStub : IAssemblyHelper
{
    private readonly Assembly _assembly;
    public AssemblyStub(Assembly assembly)
    {
        _assembly = assembly;
    }
    public Assembly GetAssembly()
    {
        return _assembly;
    }
}