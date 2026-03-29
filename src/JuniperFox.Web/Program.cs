using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using JuniperFox.Web;
using JuniperFox.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBase = builder.Configuration["ApiBaseAddress"]
    ?? throw new InvalidOperationException(
        "Configure ApiBaseAddress (see wwwroot/appsettings.json).");

builder.Services.AddTransient<IncludeRequestCredentialsHandler>();
builder.Services.AddSingleton<LastOpenedListStorage>();
builder.Services.AddHttpClient("Api", client => client.BaseAddress = new Uri(apiBase))
    .AddHttpMessageHandler<IncludeRequestCredentialsHandler>();

await builder.Build().RunAsync();
