using AspNetCore.Localizer.Json.JsonOptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AspNetCore.Localizer.Json.Localizer.Modes
{
    internal partial class LocalizationI18NModeGenerator
    {
        private static string ReadFile(JsonLocalizationOptions options, string file)
        {
            return File.ReadAllText(file, options.FileEncoding);
        }
    }
}
