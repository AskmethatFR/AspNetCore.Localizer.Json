using AspNetCore.Localizer.Json.Sample.MAUI.Shared.Services;

namespace AspNetCore.Localizer.Json.Sample.MAUI.Web.Client.Services;

public class FormFactor : IFormFactor
{
    public string GetFormFactor()
    {
        return "WebAssembly";
    }

    public string GetPlatform()
    {
        return Environment.OSVersion.ToString();
    }
}
