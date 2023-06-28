using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Text.Json;
using AspNetCore.Localizer.Json.JsonOptions;

namespace AspNetCore.Localizer.Json.Localizer.Modes
{
    public static class LocalisationModeHelpers
    {
        public static ConcurrentDictionary<T, U> ReadAndDeserializeFile<T,U>(string file, Encoding encoding)
        {
            return 
                JsonSerializer.Deserialize<ConcurrentDictionary<T, U>>(
                    File.ReadAllText(file, encoding), new JsonSerializerOptions() { ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true});
        }
    }
}