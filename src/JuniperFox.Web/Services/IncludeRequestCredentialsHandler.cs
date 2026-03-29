using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace JuniperFox.Web.Services;

/// <summary>Sends cross-origin API requests with cookies (auth) from the browser.</summary>
public sealed class IncludeRequestCredentialsHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
        return base.SendAsync(request, cancellationToken);
    }
}
