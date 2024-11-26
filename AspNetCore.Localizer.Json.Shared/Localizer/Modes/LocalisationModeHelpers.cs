using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using AspNetCore.Localizer.Json.JsonOptions;

namespace AspNetCore.Localizer.Json.Localizer.Modes
{
    public static class LocalisationModeHelpers
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions()
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        public static Dictionary<T, U> ReadAndDeserializeFile<T, U>(string file, Encoding encoding)
        {
            try
            {
                using FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 8192, useAsync: false);
                using StreamReader reader = new StreamReader(stream, encoding);
                
                string content = reader.ReadToEnd();
                
                return JsonSerializer.Deserialize<Dictionary<T, U>>(content, JsonOptions);
            }
            catch (IOException ioEx)
            {
                throw new IOException($"Error reading file '{file}'", ioEx);
            }
            catch (JsonException jsonEx)
            {
                throw new JsonException($"Error deserializing JSON from file '{file}'", jsonEx);
            }
        }
    }
}