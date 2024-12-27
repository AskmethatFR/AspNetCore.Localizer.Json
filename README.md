# AspNetCore.Localizer.Json

Json Localizer library for .NetCore Asp.net projects

#### Nuget

[![NuGet](https://img.shields.io/nuget/dt/AspNetCore.Localizer.Json.svg)](https://www.nuget.org/packages/AspNetCore.Localizer.Json)

#### Build

[![.NET](https://github.com/AskmethatFR/AspNetCore.Localizer.Json/actions/workflows/dotnet.yml/badge.svg)](https://github.com/AskmethatFR/AspNetCore.Localizer.Json/actions/workflows/dotnet.yml)

# IMPORTANT

From version 1.0.0, the library use only EmbeddedResource to load the files.

# Project

This library allows users to use JSON files instead of RESX in an ASP.NET application.
The code tries to be most compliant with Microsoft guidelines.
The library is compatible with NetCore.

# Configuration

An extension method is available for `IServiceCollection`.
You can have a look at the
method [here](https://github.com/AlexTeixeira/Askmethat-Aspnet-JsonLocalizer/blob/development/AspNetCore.Localizer.Json/Extensions/JsonLocalizerServiceExtension.cs)

## Options

A set of options is available.
You can define them like this :

``` cs
services.AddJsonLocalization(options => {
        options.CacheDuration = TimeSpan.FromMinutes(15);
        options.ResourcesPath = "mypath";
        options.FileEncoding = Encoding.GetEncoding("ISO-8859-1");
        options.SupportedCultureInfos = new HashSet<CultureInfo>()
        {
          new CultureInfo("en-US"),
          new CultureInfo("fr-FR")
        };
        options.AssemblyHelper = new AssemblyHelper("MyAssembly");
    });
```

### Current Options

- **SupportedCultureInfos** : _Default value : _List containing only default culture_ and CurrentUICulture. Optionnal
  array of cultures that you should provide to plugin. _(Like RequestLocalizationOptions)
- **ResourcesPath** : _Default value : `$"{_env.WebRootPath}/Resources/"`_. Base path of your resources. The plugin will
  browse the folder and sub-folders and load all present JSON files.
- **AdditionalResourcesPaths** : _Default value : null_. Optionnal array of additional paths to search for resources.
- **CacheDuration** : _Default value : 30 minutes_. We cache all values to memory to avoid loading files for each
  request, this parameter defines the time after which the cache is refreshed.
- **FileEncoding** : _default value : UTF8_. Specify the file encoding.
- **Caching** : *_default value: MemoryCache*. Internal caching can be overwritted by using custom class that extends
  IMemoryCache.
- **PluralSeparator** : *_default value: |*. Seperator used to get singular or pluralized version of localization. More
  information in *Pluralization*
- **MissingTranslationLogBehavior** : *_default value: LogConsoleError*. Define the logging mode
- **LocalizationMode** : *_default value: Basic*. Define the localization mode for the Json file. Currently Basic and
  I18n. More information in *LocalizationMode*
- **MissingTranslationsOutputFile** : This enables to specify in which file the missing translations will be written
  when `MissingTranslationLogBehavior = MissingTranslationLogBehavior.CollectToJSON`, defaults to
  `MissingTranslations-<locale>.json`
- **IgnoreJsonErrors**: This properly will ignore the JSON errors if set to true. Recommended in production but not in
  development.
- **LocalizerDiagnosticMode**: When set to true, the localizer will replace all localized with "X". This is designed to
  identify text that is _not using the localizer_ on the page.
- **AssemblyHelper**: This is used to load the resources from a specific assembly. If not set, the plugin will use the
  entry assembly.

### Assemblies

To be able to load culture files from an assembly, you should use set WithCulture="false" in csproj file.

``` xml
<ItemGroup>
        <EmbeddedResource Include="Resources\localization.json" WithCulture="false">
        </EmbeddedResource>
        <EmbeddedResource Include="i18n\*.json" WithCulture="false">
        </EmbeddedResource>
    </ItemGroup>
```

### Pluralization

You are now able to manage a singular (left) and plural (right) version for the same Key.
*PluralSeparator* is used as separator between the two strings.

For example : User|Users for key Users

To use plural string, use parameters
from [IStringLocalizer](https://github.com/aspnet/AspNetCore/blob/def36fab1e45ef7f169dfe7b59604d0002df3b7c/src/Mvc/Mvc.Localization/src/LocalizedHtmlString.cs),
if last parameters is a boolean, pluralization will be activated.

Pluralization is available with IStringLocalizer, IViewLocalizer and HtmlStringLocalizer :

You can have multiple pluralization, to use it, you should
use IJsonStringLocalizer interface and this method
```LocalizedString GetPlural(string key, double count, params object[] arguments)```

**localizer.GetString("Users", true)**;

### Clean Memory Cache

We allows you to clean cache.
It's usefull when you want's tu update in live some translations.

**Example**

``` cs
public class HomeController{
  private readonly IJsonStringLocalizer _localizer;
  
  public HomeController(IJsonStringLocalizer<HomeController> localizer)
  {
      _localizer = localizer;
      _localizer.ClearMemCache(new List<CultureInfo>()
      {
          new CultureInfo("en-US")
      });
  }
}
```

# Blazor Server HTML parsing

As you know, Blazor Server does not provide IHtmlLocalizer. To avoid this, you can now use
from **IJsonStringLocalizer** this method
```MarkupString GetHtmlBlazorString(string name, bool shouldTryDefaultCulture = true)```

# Information

**Platform Support**

| Platform      | Version |
|---------------|:-------:|
| NetCore       | 9.0.0+  |
| Blazor Server | 9.0.0+  |
| Blazor Wasm   | 9.0.0+  |
| Blazor MAUI   | 9.0.0+  |

# Localization mode

As asked on the request #64, Some user want to have the possiblities to manage file with i18n way.
To answer this demand, a localization mode was introduced with default value Basic. Basic version means the the one
describe in the previous parts

## I18n

To use the i18n file management, use the the option Localization mode like this :
``` cs LocalizationMode = LocalizationMode.I18n ```.
After that, you should be able to use this json :

``` json
{
   "Name": "Name",
   "Color": "Color"
}
```

**File name**

File name are important for some purpose (Culture looking, parent culture, fallback).

Please use this pattern : **[fileName].[culture].json**
If you need a fallback culture that target all culture, you can create a file named  **localisation.json**. Of course,
if this file does not exist, the chosen default culture is the fallback.

# Performances

After talking with others Devs about my package, they asked my about performance.

``` ini
BenchmarkDotNet v0.14.0, EndeavourOS
AMD Ryzen 9 7950X3D, 1 CPU, 32 logical and 16 physical cores
.NET SDK 9.0.100
  [Host]     : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
```

| Method                                          |          Mean |        Error |       StdDev |           Min |           Max |    Ratio | RatioSD |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
|-------------------------------------------------|--------------:|-------------:|-------------:|--------------:|--------------:|---------:|--------:|-------:|-------:|----------:|------------:|
| Localizer                                       |      31.83 ns |     0.140 ns |     0.117 ns |      31.62 ns |      32.00 ns |     1.00 |    0.01 |      - |      - |         - |          NA |
| JsonLocalizer                                   |      11.70 ns |     0.030 ns |     0.027 ns |      11.67 ns |      11.75 ns |     0.37 |    0.00 |      - |      - |         - |          NA |
| JsonLocalizerWithCreation                       | 140,762.12 ns | 2,813.586 ns | 5,810.543 ns | 126,991.04 ns | 150,169.03 ns | 4,422.62 |  181.61 | 3.9063 | 3.4180 |  203362 B |          NA |
| I18nJsonLocalizerWithCreation                   |  87,809.74 ns |   285.333 ns |   238.266 ns |  87,211.17 ns |  88,223.01 ns | 2,758.90 |   12.16 | 9.7656 | 9.5215 |  166091 B |          NA |
| JsonLocalizerWithCreationAndExternalMemoryCache |   4,411.59 ns |    63.052 ns |    58.979 ns |   4,301.21 ns |   4,500.86 ns |   138.61 |    1.86 | 0.1373 | 0.1297 |    7144 B |          NA |
| JsonLocalizerDefaultCultureValue                |      76.77 ns |     1.097 ns |     0.857 ns |      74.63 ns |      77.90 ns |     2.41 |    0.03 | 0.0129 |      - |     216 B |          NA |
| MicrosoftLocalizerDefaultCultureValue           |     108.21 ns |     1.132 ns |     1.059 ns |     106.70 ns |     110.48 ns |     3.40 |    0.03 | 0.0043 |      - |     216 B |          NA |

# Contributors

<table>
  <tr>
    <td align="center">
      <a href="https://github.com/lethek">
        <img src="https://avatars2.githubusercontent.com/u/52574?s=460&v=4" width="100px;" alt="Michael Monsour"/>
        <br />
        <sub><b>Michael Monsour</b></sub>
      </a>
    </td>
     <td align="center">
      <a href="https://github.com/lugospod">
        <img src="https://avatars1.githubusercontent.com/u/29342608?s=460&v=4" width="100px;" alt="Luka Gospodnetic"/>
        <br />
        <sub><b>Luka Gospodnetic</b></sub>
      </a>
    </td>
     <td align="center">
      <a href="https://github.com/Compufreak345">
        <img src="https://avatars3.githubusercontent.com/u/10026694?s=460&v=4" width="100px;" alt="Christoph Sonntag"/>
        <br />
        <sub><b>Christoph Sonntag</b></sub>
      </a>
    </td>
     <td align="center">
      <a href="https://github.com/Dunning-Kruger">
        <img src="https://avatars0.githubusercontent.com/u/1564825?s=460&v=4" width="100px;" alt="Nacho"/>
        <br />
        <sub><b>Nacho</b></sub>
      </a>
    </td>
     <td align="center">
      <a href="https://github.com/AshleyMedway">
        <img src="https://avatars3.githubusercontent.com/u/1255596?s=460&v=4" width="100px;" alt="Ashley Medway"/>
        <br />
        <sub><b>Ashley Medway</b></sub>
      </a>
    </td>
     <td align="center">
      <a href="https://github.com/NoPasaran0218">
        <img src="https://avatars2.githubusercontent.com/u/25226807?s=460&v=4" width="100px;" alt="Serhii Voitovych"/>
        <br />
        <sub><b>Serhii Voitovych</b></sub>
      </a>
    </td>
    <td align="center">
        <a href="https://github.com/JamesHill3">
            <img src="https://avatars0.githubusercontent.com/u/1727474?s=460&v=4" width="100px;" alt="James Hill"/>
            <br />
            <sub><b>James Hill</b></sub>
        </a>
    </td>
    <td align="center">
        <a href="https://github.com/Czirok">
            <img src="https://avatars2.githubusercontent.com/u/1266377?s=460&v=4" width="100px;" alt="Ferenc Czirok"/>
            <br />
            <sub><b>Ferenc Czirok</b></sub>
        </a>
    </td>
     <td align="center">
            <a href="https://github.com/rohanreddyg">
                <img src="https://avatars0.githubusercontent.com/u/240114?s=400&v=4" width="100px;" alt="rohanreddyg"/>
                <br />
                <sub><b>rohanreddyg</b></sub>
            </a>
        </td>
 <td align="center">
            <a href="https://github.com/rickszyr">
                <img src="https://avatars.githubusercontent.com/u/10763102?s=460&v=4" width="100px;" alt="rickszyr"/>
                <br />
                <sub><b>rickszyr</b></sub>
            </a>
        </td>
 <td align="center">
            <a href="https://github.com/ErikApption">
                <img src="https://avatars.githubusercontent.com/u/3179656?s=460&u=4a6b52f80b64f5951d3d04b4cfe18ac7f050a52a&v=4" width="100px;" alt="ErikApption"/>
                <br />
                <sub><b>ErikApption</b></sub>
            </a>
        </td>
  </tr>

</table>

A special thanks to @Compufreak345 for is hard work. He did a lot for this repo.<br/><br/>
A special thanks to @EricApption for is work to improve the repo and making a very good stuff on migrating to net6 and
System.Text.Json & making it working for blazor wasm

# License

[MIT Licence](https://github.com/AlexTeixeira/Askmethat-Aspnet-JsonLocalizer/blob/master/LICENSE)
