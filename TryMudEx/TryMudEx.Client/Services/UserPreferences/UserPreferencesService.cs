namespace TryMudEx.Client.Services.UserPreferences;

using System.Threading.Tasks;
using Blazored.LocalStorage;

public interface IUserPreferencesService
{
    /// <summary>
    /// Saves UserPreferences in local storage
    /// </summary>
    /// <param name="userPreferences">The userPreferences to save in the local storage</param>
    public Task SaveUserPreferences(UserPreferences userPreferences);
        
    /// <summary>
    /// Loads UserPreferences in local storage
    /// </summary>
    /// <returns>UserPreferences object. Null when no settings were found.</returns>
    public Task<UserPreferences> LoadUserPreferences();
}
    
public class UserPreferencesService : IUserPreferencesService
{
    private readonly ILocalStorageService _localStorage;
    private const string Key = "userPreferences";
        
    public UserPreferencesService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }
        
    public async Task SaveUserPreferences(UserPreferences userPreferences)
    {
        await _localStorage.SetItemAsync(Key, userPreferences);
    }

    public async Task<UserPreferences> LoadUserPreferences()
    {
        return await _localStorage.GetItemAsync<UserPreferences>(Key);
    }
}