using AspNetCore.Localizer.Json.JsonOptions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;


namespace AspNetCore.Localizer.Json.Localizer
{
    //resource not use, only here to match microsoft interfaces
    internal class JsonStringLocalizerOfT<T> : JsonStringLocalizer, IJsonStringLocalizer<T>, IStringLocalizer<T>
    {
        public JsonStringLocalizerOfT(IOptions<JsonLocalizationOptions> localizationOptions, EnvironmentWrapper env) : base(localizationOptions, env, ModifyBaseName)
        {
        }


        private static string ModifyBaseName => typeof(T).ToString();
    }
}