# AspNetCore.Localizer.Json

Json Localizer library for .NetCore Asp.net projects

#### Nuget

[![NuGet](https://img.shields.io/nuget/dt/AspNetCore.Localizer.Json.svg)](https://www.nuget.org/packages/AspNetCore.Localizer.Json)

#### Build

[![.NET](https://github.com/AskmethatFR/AspNetCore.Localizer.Json/actions/workflows/dotnet.yml/badge.svg)](https://github.com/AskmethatFR/AspNetCore.Localizer.Json/actions/workflows/dotnet.yml)

# Project

This library allows users to use JSON files instead of RESX in an ASP.NET application.
It supports both **embedded resources** and **physical files** (choose via `UseEmbeddedResources`).
The code tries to be most compliant with Microsoft guidelines.
The library is compatible with .NET 8/9/10.

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

### Current Options (key ones)

- **SupportedCultureInfos** : cultures list (like RequestLocalizationOptions).  
- **ResourcesPath** : base path of resources. For physical files, set a folder (e.g. `i18n`) and ensure files are copied to output.  
- **AdditionalResourcesPaths** : extra folders (e.g. `common_i18n`).  
- **UseEmbeddedResources** : default `true`. Set `false` to load JSON from disk.  
- **CacheDuration** : memory cache duration (default 30 minutes).  
- **CacheMaxSize** : max entries in serialized cache (LRU eviction).  
- **MaxMissingTranslations / MissingTranslationRetention** : bounds + TTL for collected missing translations.  
- **FileEncoding** : default UTF-8.  
- **LocalizationMode** : `Basic` or `I18n`.  
- **PluralSeparator** : default `|`.  
- **MissingTranslationLogBehavior** : default `LogConsoleError` (or `CollectToJSON`).  
- **MissingTranslationsOutputFile** : target filename when collecting missing translations.  
- **IgnoreJsonErrors** : ignore JSON errors (recommended in production).  
- **LocalizerDiagnosticMode** : replace localized text with `X` to spot missing localizations.  
- **AssemblyHelper** : select assembly for embedded resources.

### Embedded vs Files

- **Embedded**: use `WithCulture="false"` to avoid satellite assemblies and keep resources discoverable.  
  ```xml
  <ItemGroup>
    <EmbeddedResource Include="Resources\localization.json" WithCulture="false" />
    <EmbeddedResource Include="i18n\*.json" WithCulture="false" />
  </ItemGroup>
  ```
- **Files (UseEmbeddedResources = false)**: JSON files must be copied to the output folder.  
  ```xml
  <ItemGroup>
    <Content Update="i18n\**\*.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
    <Content Update="common_i18n\**\*.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
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
| NetCore       | 8.0.0+  |
| Blazor Server | 8.0.0+  |
| Blazor Wasm   | 8.0.0+  |
| Blazor MAUI   | 8.0.0+  |

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

Parent fallback: for `fr-FR`, both `fr-FR` and `fr` files are considered (when present) before the default fallback.

# Performances (latest)

``` ini
BenchmarkDotNet v0.15.8, Linux CachyOS
AMD Ryzen 9 7950X3D 2.99GHz, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.0, 42.42.42.42424), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.0 (10.0.0, 42.42.42.42424), X64 RyuJIT x86-64-v4
```

| Method                                          | Mean            | Allocated | Δ vs previous (latency) |
|-------------------------------------------------|----------------:|----------:|--------------------------:|
| Localizer                                       | 21.425 ns       | -        | -32.7%                    |
| JsonLocalizer                                   | 7.672 ns        | -        | -34.4%                    |
| JsonLocalizerWithCreation                       | 115,225.380 ns  | 201,281 B| -18.1% (alloc -1.0%)      |
| I18nJsonLocalizerWithCreation                   | 87,230.438 ns   | 149,281 B| -0.7%  (alloc -10.1%)     |
| JsonLocalizerWithCreationAndExternalMemoryCache | 3,308.470 ns    | 5,576 B  | -25.0% (alloc -21.9%)     |
| JsonLocalizerDefaultCultureValue                | 53.005 ns       | 216 B    | -31.0%                    |
| MicrosoftLocalizerDefaultCultureValue           | 67.402 ns       | 216 B    | -37.7%                    |

Key levers:
- Streaming JSON (MemoryPool/ArrayPool), zero-copy on hot paths.
- Pooling of `LocalizatedFormat`.
- Bounded LRU cache + lock-free (ReaderWriterLockSlim) for serialized distributed cache.
- Optimized i18n key concatenation.
- Bounded/TTL collection of missing translations to avoid memory leaks.

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
