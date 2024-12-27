using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace AspNetCore.Localizer.Json.Localizer
{
    public interface IResourceReader
    {
        string ReadEmbeddedResource(string resourceName, Encoding encoding);
    }

    public class EmbeddedResourceReader : IResourceReader
    {
        private readonly Assembly _assembly;

        public EmbeddedResourceReader(Assembly assembly)
        {
            _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
        }

        public string ReadEmbeddedResource(string resourceName, Encoding encoding)
        {
            if (string.IsNullOrWhiteSpace(resourceName))
                throw new ArgumentException("Resource name cannot be null or empty.", nameof(resourceName));

            var resourceStream = _assembly.GetManifestResourceStream(resourceName);
            if (resourceStream == null)
                throw new FileNotFoundException(
                    $"Embedded resource '{resourceName}' not found in assembly '{_assembly.FullName}'.");

            using var reader = new StreamReader(resourceStream, encoding);
            return reader.ReadToEnd();
        }
    }
}