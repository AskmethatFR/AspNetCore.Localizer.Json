using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AspNetCore.Localizer.Json.Commons
{
    public class MultiAssemblyHelper : IAssemblyHelper
    {
        private readonly IReadOnlyList<Assembly> _assemblies;

        public MultiAssemblyHelper(params Assembly[] assemblies)
        {
            if (assemblies == null) throw new ArgumentNullException(nameof(assemblies));
            if (assemblies.Length == 0) throw new ArgumentException("At least one assembly is required.", nameof(assemblies));
            _assemblies = assemblies.ToList().AsReadOnly();
        }

        public MultiAssemblyHelper(params string[] assemblyNames)
        {
            if (assemblyNames == null) throw new ArgumentNullException(nameof(assemblyNames));
            if (assemblyNames.Length == 0) throw new ArgumentException("At least one assembly name is required.", nameof(assemblyNames));
            _assemblies = assemblyNames
                .Select(Assembly.Load)
                .ToList()
                .AsReadOnly();
        }

        public Assembly GetAssembly() => _assemblies[0];

        public IReadOnlyList<Assembly> GetAssemblies() => _assemblies;
    }
}