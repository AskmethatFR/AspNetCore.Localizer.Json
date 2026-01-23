using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCore.Localizer.Json.Sample.I18nTest.Controllers;

[Route("[controller]/[action]")]
public class LanguageController : ControllerBase
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LanguageController(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }


    [HttpGet]
    public IActionResult Set(string culture, string fallbackUrl)
    {
        var newCulture = CultureInfo.GetCultureInfo(culture);
        //change current thread culture
        CultureInfo.CurrentUICulture = newCulture;
        CultureInfo.CurrentCulture = newCulture;

        _httpContextAccessor.HttpContext.Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(
                new RequestCulture(
                    culture: newCulture,
                    uiCulture: newCulture)));

        return LocalRedirect(fallbackUrl);
    }

}
