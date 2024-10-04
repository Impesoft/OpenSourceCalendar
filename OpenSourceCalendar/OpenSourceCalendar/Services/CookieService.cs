using CasaAdelia.Services;
using Microsoft.JSInterop;

namespace CasaAdelia.Components.Account.Pages;

public class CookieService : ICookieService
{
    private readonly IJSRuntime _jsRuntime;

    public CookieService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task SetCookieAsync(string name, string value, int days)
    {
        var module = await _jsRuntime.InvokeAsync<IJSObjectReference>(
             "import", "./Components/Account/Pages/Login.razor.js");
        await _jsRuntime.InvokeVoidAsync("setCookie", name, value, days);
    }
}
