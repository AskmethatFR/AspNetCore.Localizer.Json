namespace AspNetCore.Localizer.Json.Localizer
{
    public interface IPluralizationRuleSet
    {
        string GetMatchingPluralizationRule(double count);
    }
}
