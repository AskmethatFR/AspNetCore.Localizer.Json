using AspNetCore.Localizer.Json.Sample.MAUI.Shared.Services;

namespace AspNetCore.Localizer.Json.Sample.MAUI.Web.Services;

public class FormFactor : IFormFactor
{
    public string GetFormFactor()
    {
        return "Web";
    }

    public string GetPlatform()
    {
        return Environment.OSVersion.ToString();
    }
}
