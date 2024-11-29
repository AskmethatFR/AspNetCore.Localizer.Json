# AspNetCore.Localizer.Json
Json Localizer library for .NetCore Asp.net projects

#### Nuget
[![NuGet](https://img.shields.io/nuget/dt/AspNetCore.Localizer.Json.svg)](https://www.nuget.org/packages/AspNetCore.Localizer.Json)
[![NuGet](https://img.shields.io/nuget/dt/AspNetCore.Localizer.Json.Wasm.svg)](https://www.nuget.org/packages/AspNetCore.Localizer.Wasm.Json)

#### Build

[![.NET](https://github.com/CorentinFoviaux/AspNetCore.Localizer.Json/actions/workflows/dotnet.yml/badge.svg)](https://github.com/AskmethatFR/AspNetCore.Localizer.Json/actions/workflows/dotnet.yml)

# Project

This library allows users to use JSON files instead of RESX in an ASP.NET application.
The code tries to be most compliant with Microsoft guidelines.
The library is compatible with NetCore.

# Configuration

An extension method is available for `IServiceCollection`.
You can have a look at the method [here](https://github.com/AlexTeixeira/Askmethat-Aspnet-JsonLocalizer/blob/development/AspNetCore.Localizer.Json/Extensions/JsonLocalizerServiceExtension.cs)

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
    });
```

### Current Options

- **SupportedCultureInfos** : _Default value : _List containing only default culture_ and CurrentUICulture. Optionnal array of cultures that you should provide to plugin. _(Like RequestLocalizationOptions)
- **ResourcesPath** : _Default value : `$"{_env.WebRootPath}/Resources/"`_.  Base path of your resources. The plugin will browse the folder and sub-folders and load all present JSON files.
- **AdditionalResourcePaths** : _Default value : null_. Optionnal array of additional paths to search for resources.
- **CacheDuration** : _Default value : 30 minutes_. We cache all values to memory to avoid loading files for each request, this parameter defines the time after which the cache is refreshed.
- **FileEncoding** : _default value : UTF8_. Specify the file encoding.
- **IsAbsolutePath** : *_default value : false*. Look for an absolute path instead of project path.
- **UseBaseName** : *_default value : false*. Use base name location for Views and constructors like default Resx localization in **ResourcePathFolder**. Please have a look at the documentation below to see the different possiblities for structuring your translation files.
- **Caching** : *_default value: MemoryCache*. Internal caching can be overwritted by using custom class that extends IMemoryCache.
- **PluralSeparator** : *_default value: |*. Seperator used to get singular or pluralized version of localization. More information in *Pluralization*
- **MissingTranslationLogBehavior** : *_default value: LogConsoleError*. Define the logging mode
- **LocalizationMode** : *_default value: Basic*. Define the localization mode for the Json file. Currently Basic and I18n. More information in *LocalizationMode*
- **MissingTranslationsOutputFile** : This enables to specify in which file the missing translations will be written when `MissingTranslationLogBehavior = MissingTranslationLogBehavior.CollectToJSON`, defaults to `MissingTranslations-<locale>.json`
- **IgnoreJsonErrors**: This properly will ignore the JSON errors if set to true. Recommended in production but not in development.
- **LocalizerDiagnosticMode**: When set to true, the localizer will replace all localized with "X". This is designed to identify text that is _not using the localizer_ on the page.

### Search patterns when UseBaseName = true

If UseBaseName is set to true, it will be searched for lingualization files by the following order - skipping the options below if any option before matches.

- If you use a non-typed IStringLocalizer all files in the Resources-directory, including all subdirectories, will be used to find a localization. This can cause unpredictable behavior if the same key is used in multiple files.

- If you use a typed localizer, the following applies - Namespace is the "short namespace" without the root namespace:
  - Nested classes will use the translation file of their parent class.
  - If there is a folder named "Your/Namespace/And/Classname", all contents of this folder will be used.
  - If there is a folder named "Your/Namespace" the folder will be searched for all json-files beginning with your classname.
  - Otherwise there will be searched for a json-file starting with "Your.Namespace.And.Classname" in your Resources-folder.
  - If there any _.shared.json_ file at base path, all the keys that do not exist in other files will be added.

- If you need a base shared files, just add a file named _localization.shared.json_ in your **ResourcesPath**

### Pluralization

In version 2.0.0, Pluralization was introduced.
You are now able to manage a singular (left) and plural (right) version for the same Key.
*PluralSeparator* is used as separator between the two strings.

For example : User|Users for key Users

To use plural string, use parameters from [IStringLocalizer](https://github.com/aspnet/AspNetCore/blob/def36fab1e45ef7f169dfe7b59604d0002df3b7c/src/Mvc/Mvc.Localization/src/LocalizedHtmlString.cs), if last parameters is a boolean, pluralization will be activated.

Pluralization is available with IStringLocalizer, IViewLocalizer and HtmlStringLocalizer :

In version 3.1.1 and above you can have multiple pluralization, to use it, you should
use IJsonStringLocalizer interface and this method ```LocalizedString GetPlural(string key, double count, params object[] arguments)```

**localizer.GetString("Users", true)**;

### Clean Memory Cache

Version 2.2.0+ allows you to clean cache.
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
from **IJsonStringLocalizer** this method ```MarkupString GetHtmlBlazorString(string name, bool shouldTryDefaultCulture = true)```

# Information

**Platform Support**

| Platform      | Version |
|---------------|:-------:|
| NetCore       | 7.0.0+  |
| Blazor Server | 7.0.0+  |
| Blazor Wasm   | 7.0.0+  |



**WithCulture method**

**WhithCulture** method is not implemented and will not be implemented. ASP.NET Team, start to set this method **Obsolete** for version 3 and will be removed in version 4 of asp.net core.

For more information :
https://github.com/AlexTeixeira/Askmethat-Aspnet-JsonLocalizer/issues/46

# Localization mode

As asked on the request #64, Some user want to have the possiblities to manage file with i18n way.
To answer this demand, a localization mode was introduced with default value Basic. Basic version means the the one describe in the previous parts

## I18n

To use the i18n file management, use the the option Localization mode like this : ``` cs LocalizationMode = LocalizationMode.I18n ```.
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
If you need a fallback culture that target all culture, you can create a file named  **localisation.json**. Of course, if this file does not exist, the chosen default culture is the fallback.

**Important: In this mode, the UseBaseName options should be False.**


For more information :
https://github.com/AlexTeixeira/Askmethat-Aspnet-JsonLocalizer/issues/64

# Blazor Wasm

### Specific Wasm Options

- **JsonFileList** : *_default value: null*. List of json files to load. If null, all json files will be loaded.

### Blazor Wasm Specificities

Because of the way Blazor Wasm works, the plugin will not be able to load files from the server.
To avoid this, you should embed your files in the project and use the following code :

``` csproj
 <EmbeddedResource Include="Resources\localization.json">
  <CopyToOutputDirectory>Never</CopyToOutputDirectory>
 </EmbeddedResource>
```

The second specificities is the management of the language files.
Blazor Wasm uses the file path as Assembly name, so you can't have multiple files with the same name.

For example, if you have a file named **localization.json** in the folder **Resources**, 
you can't have another file starting with name **localization** in the folder **Resources**, 
a file with the name **localization.fr.json** will throw an exception.

So you should have different folder for each language culture.

# Performances

After talking with others Devs about my package, they asked my about performance.

``` ini
BenchmarkDotNet v0.14.0, EndeavourOS
AMD Ryzen 9 7950X3D, 1 CPU, 32 logical and 16 physical cores
.NET SDK 9.0.100
[Host]     : .NET 8.0.11 (8.0.1124.51707), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
DefaultJob : .NET 8.0.11 (8.0.1124.51707), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


```
| Method                                          | Mean         | Error        | StdDev       | Median       | Min          | Max          | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------ |-------------:|-------------:|-------------:|-------------:|-------------:|-------------:|---------:|--------:|-------:|-------:|----------:|------------:|
| Localizer                                       |     31.31 ns |     0.122 ns |     0.108 ns |     31.29 ns |     31.09 ns |     31.45 ns |     1.00 |    0.00 |      - |      - |         - |          NA |
| JsonLocalizer                                   |     14.65 ns |     0.288 ns |     0.269 ns |     14.59 ns |     14.34 ns |     15.14 ns |     0.47 |    0.01 | 0.0010 |      - |      48 B |          NA |
| JsonLocalizerWithCreation                       | 44,313.36 ns |   464.509 ns |   434.502 ns | 44,246.52 ns | 43,572.69 ns | 45,136.89 ns | 1,415.47 |   14.25 | 0.6104 | 0.4883 |   31032 B |          NA |
| I18nJsonLocalizerWithCreation                   | 68,861.77 ns | 1,366.375 ns | 2,791.143 ns | 67,083.92 ns | 66,211.72 ns | 75,404.76 ns | 2,199.60 |   88.64 | 5.1270 | 4.8828 |   88483 B |          NA |
| JsonLocalizerWithCreationAndExternalMemoryCache |  2,902.25 ns |    55.891 ns |    59.802 ns |  2,899.28 ns |  2,799.21 ns |  2,995.20 ns |    92.70 |    1.89 | 0.1144 | 0.1106 |    5824 B |          NA |
| JsonLocalizerDefaultCultureValue                |    137.16 ns |     0.663 ns |     0.620 ns |    136.96 ns |    136.47 ns |    138.50 ns |     4.38 |    0.02 | 0.0157 |      - |     264 B |          NA |
| LocalizerDefaultCultureValue                    |    159.02 ns |     1.943 ns |     1.817 ns |    159.17 ns |    156.13 ns |    162.75 ns |     5.08 |    0.06 | 0.0129 |      - |     216 B |          NA |


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
A special thanks to @EricApption for is work to improve the repo and making a very good stuff on migrating to net6 and System.Text.Json & making it working for blazor wasm

# License

[MIT Licence](https://github.com/AlexTeixeira/Askmethat-Aspnet-JsonLocalizer/blob/master/LICENSE)
