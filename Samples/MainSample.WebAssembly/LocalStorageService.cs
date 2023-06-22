using Microsoft.JSInterop;

namespace MainSample.WebAssembly;

public class LocalStorageService
{
    private IJSRuntime _js;

    public LocalStorageService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task SetItemAsync<T>(string key, T value)
    {
        await _js.InvokeVoidAsync("localStorageFunctions.setItem", key, value);
    }

    public async Task<T> GetItemAsync<T>(string key)
    {
        return await _js.InvokeAsync<T>("localStorageFunctions.getItem", key);
    }

    public async Task RemoveItemAsync(string key)
    {
        await _js.InvokeVoidAsync("localStorageFunctions.removeItem", key);
    }

    public async Task<T[]> GetAllItemsAsync<T>()
    {
        return await _js.InvokeAsync<T[]>("localStorageFunctions.getAllItems");
    }

    public async Task<KeyValuePair<string, T>[]> GetAllThemeItemsAsync<T>()
    {
        return await _js.InvokeAsync<KeyValuePair<string, T>[]>("localStorageFunctions.getAllThemeItems");
    }
}