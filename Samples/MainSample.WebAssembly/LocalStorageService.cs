using Microsoft.JSInterop;

namespace MainSample.WebAssembly;

public class LocalStorageService(IJSRuntime js)
{
    public async Task SetItemAsync<T>(string key, T value)
    {
        await js.InvokeVoidAsync("localStorageFunctions.setItem", key, value);
    }

    public async Task<T> GetItemAsync<T>(string key)
    {
        return await js.InvokeAsync<T>("localStorageFunctions.getItem", key);
    }

    public async Task RemoveItemAsync(string key)
    {
        await js.InvokeVoidAsync("localStorageFunctions.removeItem", key);
    }

    public async Task<T[]> GetAllItemsAsync<T>()
    {
        return await js.InvokeAsync<T[]>("localStorageFunctions.getAllItems");
    }

    public async Task<KeyValuePair<string, T>[]> GetAllThemeItemsAsync<T>()
    {
        return await js.InvokeAsync<KeyValuePair<string, T>[]>("localStorageFunctions.getAllThemeItems");
    }
}