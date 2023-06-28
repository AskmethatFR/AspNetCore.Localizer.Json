using AspNetCore.Localizer.Json.JsonOptions;
#if NETSTANDARD
using Microsoft.AspNetCore.Hosting;
#endif
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System.Net.Http;

namespace AspNetCore.Localizer.Json.Localizer
{
    //resource not use, only here to match microsoft interfaces
    internal class JsonStringLocalizerOfT<T> : JsonStringLocalizer, IJsonStringLocalizer<T>, IStringLocalizer<T>
    {
         public JsonStringLocalizerOfT(IOptions<JsonLocalizationOptions> localizationOptions, EnvironmentWrapper env, string baseName
 = null) : base(localizationOptions, env, ModifyBaseName)
        {
        }

        private static string ModifyBaseName => typeof(T).ToString();
    }
}