using AspNetCore.Localizer.Json.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AspNetCore.Localizer.Json.Test.Extensions
{
    [TestClass]
    public class AddJsonLocalizationExtensionTests
    {
        [TestMethod]
        public void AddJsonLocalization_WithNullSetupAction_DoesNotThrow()
        {
            var services = new ServiceCollection();
            services.AddJsonLocalization(null);
        }
    }
}
