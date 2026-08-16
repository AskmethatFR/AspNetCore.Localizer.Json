using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AspNetCore.Localizer.Json.Commons
{
    /// <summary>
    /// Resolves the culture code embedded in a manifest resource name.
    /// Embedded resources use dotted names (e.g. "Namespace.i18n.localization.fr.json").
    /// Two layouts are supported for the segments that follow the resource path:
    ///   - suffix:  "{base}.{culture}"  (e.g. "url.fr")             -> culture is the last segment
    ///   - folder:  "{culture}.{base}"  (e.g. "fr.localization")    -> culture is the first segment
    /// A single segment after the resource path is always the file base name, never a culture,
    /// which is what keeps a short base name such as "url" from being mistaken for a culture code.
    /// </summary>
    internal static class EmbeddedResourceCultureResolver
    {
        private static readonly Regex CultureNameRegex =
            new("^[a-zA-Z]{2,3}(?:-[a-zA-Z0-9]{2,8}){0,2}$", RegexOptions.Compiled);

        public static string GetCulture(string resourceName, params string[] resourcePaths)
        {
            return GetCulture(resourceName, (IEnumerable<string>)resourcePaths);
        }

        public static string GetCulture(string resourceName, IEnumerable<string> resourcePaths)
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(resourceName) ?? string.Empty;
            var segments = nameWithoutExt.Split('.', StringSplitOptions.RemoveEmptyEntries);

            var start = ResolveContentStart(segments, resourcePaths);
            var contentLength = segments.Length - start;

            // A single content segment is the file base name (neutral resource), not a culture.
            if (contentLength <= 1)
            {
                return string.Empty;
            }

            var last = segments[segments.Length - 1];
            if (IsCulture(last))
            {
                return last;
            }

            var first = segments[start];
            if (IsCulture(first))
            {
                return first;
            }

            return string.Empty;
        }

        // The content (base name + optional culture) is everything after the resource-path segment.
        // Any of the configured paths (primary ResourcesPath or an AdditionalResourcesPaths entry)
        // may anchor this particular resource name, so we try them in order and use the first path
        // that occurs. We anchor on the FIRST occurrence of that path so that a resource whose base
        // name equals the folder name (e.g. "interpolation/interpolation.fr.json") does not swallow
        // the trailing culture segment.
        private static int ResolveContentStart(string[] segments, IEnumerable<string> resourcePaths)
        {
            if (resourcePaths == null)
            {
                return 0;
            }

            foreach (var path in resourcePaths)
            {
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                var idx = Array.FindIndex(segments,
                    s => s.Equals(path, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0)
                {
                    return idx + 1;
                }
            }

            return 0;
        }

        private static bool IsCulture(string segment)
        {
            return CultureNameRegex.IsMatch(segment)
                   && !string.Equals(segment, CultureInfo.InvariantCulture.Name, StringComparison.OrdinalIgnoreCase);
        }
    }
}
