using AspNetCore.Localizer.Json.Sample.MAUI.Shared.Services;

namespace AspNetCore.Localizer.Json.Sample.MAUI.Services;

public class FormFactor : IFormFactor
{
    public string GetFormFactor()
    {
        return DeviceInfo.Idiom.ToString();
    }

    public string GetPlatform()
    {
        return DeviceInfo.Platform.ToString() + " - " + DeviceInfo.VersionString;
    }
}
