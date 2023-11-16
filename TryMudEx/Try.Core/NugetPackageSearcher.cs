using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;


namespace Try.Core;

public class NuGetPackageSearcher
{
    private readonly HttpClient _httpClient;

    // Adjusted base URL for NuGet API search service
    private const string NuGetSearchBaseUrl = "https://azuresearch-usnc.nuget.org/query";

    public NuGetPackageSearcher(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    // Method that searches for packages using the HttpClient provided
    public async Task<NugetResponse> SearchForPackagesAsync(string searchString, int limit = 20, int skip = 0)
    {
        if (string.IsNullOrEmpty(searchString))
            throw new ArgumentException("Search string cannot be null or empty.", nameof(searchString));

        try
        {
            // Construct the search query with a default of 20 results
            string searchQuery = $"{NuGetSearchBaseUrl}?q={Uri.EscapeDataString(searchString)}&take={limit}&skip={skip}";

            // Send a GET request to the search URL
            HttpResponseMessage response = await _httpClient.GetAsync(searchQuery);
            response.EnsureSuccessStatusCode(); // Throw an exception if the HTTP call failed

            // Read the response content as a string
            return await response.Content.ReadFromJsonAsync<NugetResponse>();
        }
        catch (HttpRequestException httpEx)
        {
            Console.WriteLine($"HTTP Request Exception: {httpEx.Message}");
            throw; // Rethrow the exception to allow caller to handle it
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");
            throw; // Rethrow the exception to allow caller to handle it
        }
    }
}
