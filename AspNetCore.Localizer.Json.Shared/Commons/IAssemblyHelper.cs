using System.Collections.Generic;
using System.Reflection;

namespace AspNetCore.Localizer.Json.Commons
{
    public interface IAssemblyHelper
    {
        Assembly GetAssembly();

        IReadOnlyList<Assembly> GetAssemblies() => new[] { GetAssembly() };
    }

    public class AssemblyHelper : IAssemblyHelper
    {
        private readonly Assembly _assembly;

        public AssemblyHelper(Assembly assembly)
        {
            _assembly = assembly;
        }
        
        public AssemblyHelper(string assemblyName)
        {
            _assembly = Assembly.Load(assemblyName);
        }

        public AssemblyHelper()
        {
            
        }
        
        public Assembly GetAssembly()
        {
            return _assembly ?? Assembly.GetCallingAssembly();
        }

        public IReadOnlyList<Assembly> GetAssemblies()
        {
            return new[] { GetAssembly() };
        }
    }
}