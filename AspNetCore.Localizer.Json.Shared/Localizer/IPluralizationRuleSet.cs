using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Text;

namespace AspNetCore.Localizer.Json.Localizer
{
    public interface IPluralizationRuleSet
    {
        string GetMatchingPluralizationRule(double count);
    }
}
