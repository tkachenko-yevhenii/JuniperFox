using Microsoft.JSInterop;

namespace JuniperFox.Web.Services;

public sealed class LastOpenedListStorage(IJSRuntime js)
{
    public async ValueTask<Guid?> GetAsync()
    {
        var s = await js.InvokeAsync<string?>("JuniperFox.getLastOpenedListId");
        if (string.IsNullOrEmpty(s) || !Guid.TryParse(s, out var id))
            return null;
        return id;
    }

    public ValueTask SetAsync(Guid id) =>
        js.InvokeVoidAsync("JuniperFox.setLastOpenedListId", id.ToString());

    public ValueTask ClearAsync() =>
        js.InvokeVoidAsync("JuniperFox.setLastOpenedListId", "");
}
