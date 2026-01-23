using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCore.Localizer.Json.Sample.I18NTestVersion10.Controllers;

[Route("[controller]/[action]")]
public class LanguageController : ControllerBase
{
    [HttpGet]
    public IActionResult Set(string culture, string fallbackUrl)
    {
        var newCulture = CultureInfo.GetCultureInfo(culture);
        CultureInfo.CurrentUICulture = newCulture;
        CultureInfo.CurrentCulture = newCulture;

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(
                new RequestCulture(culture: newCulture, uiCulture: newCulture)));

        return LocalRedirect(fallbackUrl);
    }
}
