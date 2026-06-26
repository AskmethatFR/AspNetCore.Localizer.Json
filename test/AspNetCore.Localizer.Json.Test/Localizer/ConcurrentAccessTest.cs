using AspNetCore.Localizer.Json.Localizer;
using AspNetCore.Localizer.Json.Test.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AspNetCore.Localizer.Json.JsonOptions;

namespace AspNetCore.Localizer.Json.Test.Localizer
{
    [TestClass]
    public class ConcurrentAccessTest
    {
        [TestMethod]
        public void Parallel_Culture_Switching_No_Exception()
        {
            var cultureFr = new CultureInfo("fr-FR");
            var cultureEn = new CultureInfo("en-US");

            CultureInfo.CurrentUICulture = cultureEn;

            var localizer = JsonStringLocalizerHelperFactory.Create(new JsonLocalizationOptions
            {
                DefaultCulture = cultureEn,
                SupportedCultureInfos = new()
                {
                    cultureEn,
                    cultureFr
                },
                ResourcesPath = "json_files",
                AssemblyHelper = new AssemblyStub(Assembly.GetExecutingAssembly())
            });

            var cultures = new[] { cultureEn, cultureFr };
            var exceptions = new ConcurrentBag<Exception>();

            Parallel.For(0, 100, _ =>
            {
                foreach (var culture in cultures)
                {
                    CultureInfo.CurrentUICulture = culture;

                    try
                    {
                        var val = localizer["Name1"].Value;
                        var val2 = localizer["Name2"].Value;
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                }
            });

            Assert.IsTrue(exceptions.IsEmpty,
                $"Concurrent access threw {exceptions.Count} exception(s): {string.Join("; ", exceptions.Select(e => e.GetType().Name + ": " + e.Message).Distinct())}");
        }

        [TestMethod]
        public void Parallel_Pluralization_No_Exception()
        {
            var cultureFr = new CultureInfo("fr-FR");
            var cultureEn = new CultureInfo("en-US");

            CultureInfo.CurrentUICulture = cultureEn;

            var localizer = JsonStringLocalizerHelperFactory.Create(new JsonLocalizationOptions
            {
                DefaultCulture = cultureEn,
                SupportedCultureInfos = new()
                {
                    cultureEn,
                    cultureFr
                },
                ResourcesPath = "pluralization",
                AssemblyHelper = new AssemblyStub(Assembly.GetExecutingAssembly())
            });

            var cultures = new[] { cultureEn, cultureFr };
            var exceptions = new ConcurrentBag<Exception>();

            Parallel.For(0, 100, _ =>
            {
                foreach (var culture in cultures)
                {
                    CultureInfo.CurrentUICulture = culture;

                    try
                    {
                        var val = localizer.GetPlural("PluralUser", 1).Value;
                        var val2 = localizer.GetPlural("PluralUser", 2).Value;
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                }
            });

            Assert.IsTrue(exceptions.IsEmpty,
                $"Concurrent pluralization access threw {exceptions.Count} exception(s): {string.Join("; ", exceptions.Select(e => e.GetType().Name + ": " + e.Message).Distinct())}");
        }

        [TestMethod]
        public void Parallel_Mixed_Access_No_Exception()
        {
            var cultureFr = new CultureInfo("fr-FR");
            var cultureEn = new CultureInfo("en-US");

            CultureInfo.CurrentUICulture = cultureEn;

            var localizer = JsonStringLocalizerHelperFactory.Create(new JsonLocalizationOptions
            {
                DefaultCulture = cultureEn,
                SupportedCultureInfos = new()
                {
                    cultureEn,
                    cultureFr
                },
                ResourcesPath = "json_files",
                AssemblyHelper = new AssemblyStub(Assembly.GetExecutingAssembly()),
                PluralSeparator = '|'
            });

            var cultures = new[] { cultureEn, cultureFr };
            var exceptions = new ConcurrentBag<Exception>();

            Parallel.For(0, 100, _ =>
            {
                foreach (var culture in cultures)
                {
                    CultureInfo.CurrentUICulture = culture;

                    try
                    {
                        var val = localizer["Name1"].Value;
                        var val2 = localizer.GetPlural("Name1", 1).Value;
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                }
            });

            Assert.IsTrue(exceptions.IsEmpty,
                $"Concurrent mixed access threw {exceptions.Count} exception(s): {string.Join("; ", exceptions.Select(e => e.GetType().Name + ": " + e.Message).Distinct())}");
        }
    [TestMethod]
        public void Parallel_Culture_Switching_With_Reload()
        {
            var cultureFr = new CultureInfo("fr-FR");
            var cultureEn = new CultureInfo("en-US");

            CultureInfo.CurrentUICulture = cultureEn;

            var localizer = JsonStringLocalizerHelperFactory.Create(new JsonLocalizationOptions
            {
                DefaultCulture = cultureEn,
                SupportedCultureInfos = new()
                {
                    cultureEn,
                    cultureFr
                },
                ResourcesPath = "json_files",
                AssemblyHelper = new AssemblyStub(Assembly.GetExecutingAssembly())
            });

            var cultures = new[] { cultureEn, cultureFr };
            var exceptions = new ConcurrentBag<Exception>();

            Parallel.For(0, 50, i =>
            {
                foreach (var culture in cultures)
                {
                    CultureInfo.CurrentUICulture = culture;

                    try
                    {
                        var val = localizer["Name1"].Value;
                        localizer.ReloadMemCache(new[] { culture });
                        var val2 = localizer["Name2"].Value;
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                }
            });

            Assert.IsTrue(exceptions.IsEmpty,
                $"Concurrent access with reload threw {exceptions.Count} exception(s): {string.Join("; ", exceptions.Select(e => e.GetType().Name + ": " + e.Message).Distinct())}");
        }

        [TestMethod]
        public void Parallel_GetPlural_HighContention()
        {
            var cultureFr = new CultureInfo("fr-FR");
            var cultureEn = new CultureInfo("en-US");

            CultureInfo.CurrentUICulture = cultureEn;

            var localizer = JsonStringLocalizerHelperFactory.Create(new JsonLocalizationOptions
            {
                DefaultCulture = cultureEn,
                SupportedCultureInfos = new()
                {
                    cultureEn,
                    cultureFr
                },
                ResourcesPath = "pluralization",
                AssemblyHelper = new AssemblyStub(Assembly.GetExecutingAssembly())
            });

            var cultures = new[] { cultureEn, cultureFr };
            var exceptions = new ConcurrentBag<Exception>();

            Parallel.For(0, 200, i =>
            {
                foreach (var culture in cultures)
                {
                    CultureInfo.CurrentUICulture = culture;

                    try
                    {
                        for (int count = 0; count <= 5; count++)
                        {
                            var val = localizer.GetPlural("PluralUser", count).Value;
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                }
            });

            Assert.IsTrue(exceptions.IsEmpty,
                $"High contention pluralization threw {exceptions.Count} exception(s): {string.Join("; ", exceptions.Select(e => e.GetType().Name + ": " + e.Message).Distinct())}");
        }

        [TestMethod]
        public void Parallel_Mixed_Read_Write_NoDataLoss()
        {
            var cultureEn = new CultureInfo("en-US");

            CultureInfo.CurrentUICulture = cultureEn;

            var localizer = JsonStringLocalizerHelperFactory.Create(new JsonLocalizationOptions
            {
                DefaultCulture = cultureEn,
                SupportedCultureInfos = { cultureEn },
                ResourcesPath = "json_files",
                AssemblyHelper = new AssemblyStub(Assembly.GetExecutingAssembly())
            });

            var exceptions = new ConcurrentBag<Exception>();

            Parallel.For(0, 100, _ =>
            {
                try
                {
                    var val1 = localizer["Name1"].Value;
                    var val2 = localizer["Name2"].Value;

                    if (val1 != "My Name 1")
                        exceptions.Add(new InvalidOperationException($"Expected 'My Name 1' got '{val1}'"));
                    if (val2 != "My Name 2")
                        exceptions.Add(new InvalidOperationException($"Expected 'My Name 2' got '{val2}'"));
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            Assert.IsTrue(exceptions.IsEmpty,
                $"Data loss/concurrent read threw {exceptions.Count} exception(s): {string.Join("; ", exceptions.Select(e => e.GetType().Name + ": " + e.Message).Distinct())}");
        }

        [TestMethod]
        public void Parallel_DeepFolder_Localization()
        {
            var cultureFr = new CultureInfo("fr-FR");
            var cultureEn = new CultureInfo("en-US");

            CultureInfo.CurrentUICulture = cultureEn;

            var localizer = JsonStringLocalizerHelperFactory.Create(new JsonLocalizationOptions
            {
                DefaultCulture = cultureEn,
                SupportedCultureInfos = new()
                {
                    cultureEn,
                    cultureFr
                },
                ResourcesPath = "json",
                AssemblyHelper = new AssemblyStub(typeof(AspNetCore.Localizer.Json.TestSample.Pages.DeepFolder.DeepPageModel).Assembly)
            });

            var cultures = new[] { cultureEn, cultureFr };
            var exceptions = new ConcurrentBag<Exception>();

            Parallel.For(0, 50, _ =>
            {
                foreach (var culture in cultures)
                {
                    CultureInfo.CurrentUICulture = culture;

                    try
                    {
                        var val = localizer["DeepHello"].Value;
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                }
            });

            Assert.IsTrue(exceptions.IsEmpty,
                $"Deep folder concurrent access threw {exceptions.Count} exception(s): {string.Join("; ", exceptions.Select(e => e.GetType().Name + ": " + e.Message).Distinct())}");
        }
    }
}