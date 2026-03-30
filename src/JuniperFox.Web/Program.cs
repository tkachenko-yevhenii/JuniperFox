using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using JuniperFox.Web;
using JuniperFox.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var configuredApiBase = builder.Configuration["ApiBaseAddress"];
var apiBase = string.IsNullOrWhiteSpace(configuredApiBase)
    ? builder.HostEnvironment.BaseAddress
    : configuredApiBase;

var apiBaseUri = Uri.TryCreate(apiBase, UriKind.Absolute, out var absoluteUri)
    ? absoluteUri
    : new Uri(new Uri(builder.HostEnvironment.BaseAddress), apiBase);

builder.Services.AddTransient<IncludeRequestCredentialsHandler>();
builder.Services.AddSingleton<LastOpenedListStorage>();
builder.Services.AddHttpClient("Api", client => client.BaseAddress = apiBaseUri)
    .AddHttpMessageHandler<IncludeRequestCredentialsHandler>();

await builder.Build().RunAsync();
