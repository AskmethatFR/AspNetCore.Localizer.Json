using AspNetCore.Localizer.Json.Sample.MAUI.Shared;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using AspNetCore.Localizer.Json.Sample.MAUI.Shared.Services;
using AspNetCore.Localizer.Json.Sample.MAUI.Web.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Add device-specific services used by the AspNetCore.Localizer.Json.Sample.MAUI.Shared project
builder.Services.AddSingleton<IFormFactor, FormFactor>();
builder.Services.AddShared();

await builder.Build().RunAsync();
