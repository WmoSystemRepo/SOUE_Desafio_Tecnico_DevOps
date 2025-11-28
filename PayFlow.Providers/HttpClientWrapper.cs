using System.Net.Http.Json;

namespace PayFlow.Providers;

public class HttpClientWrapper
{
    private readonly HttpClient _httpClient;

    public HttpClientWrapper(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(url, request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TResponse>();
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (TaskCanceledException)
        {
            throw new HttpRequestException("Request timeout - provider unavailable");
        }
        catch (Exception ex)
        {
            throw new HttpRequestException($"Error communicating with provider: {ex.Message}", ex);
        }
    }
}

