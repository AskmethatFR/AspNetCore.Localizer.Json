using System.Reflection;

namespace AspNetCore.Localizer.Json.Commons
{
    public interface IAssemblyHelper
    {
        Assembly GetAssembly();
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
    }
}