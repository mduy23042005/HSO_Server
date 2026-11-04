using Azure;
using System;
using System.Net.Http;
using System.Threading.Tasks;

public class WebAPIManager
{
    private readonly HttpClient client = new HttpClient();
    private const string apiUrl = "http://localhost:55555";
    
    public static WebAPIManager Instance { get; } = new WebAPIManager();

    public string GetApiUrl()
    {
        return apiUrl;
    }
    public HttpClient GetHttpClient()
    {
        return client;
    }

    public async Task<bool> InitAPI()
    {
        try
        {
            HttpResponseMessage response = await client.GetAsync(apiUrl);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> PostAsync(string endpoint)
    {
        try
        {
            string url = endpoint.StartsWith("https") ? endpoint : $"{apiUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";

            HttpResponseMessage response = await client.PostAsync(url, null);

            string result = await response.Content.ReadAsStringAsync();
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine("POST API lỗi: " + ex.Message);
            return null;
        }
    }
    public async Task<string?> PostAsync(string endpoint, HttpContent content)
    {
        try
        {
            string url = endpoint.StartsWith("https")
                ? endpoint
                : $"{apiUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";

            HttpResponseMessage response = await client.PostAsync(url, content);

            string result = await response.Content.ReadAsStringAsync();
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine("POST API lỗi: " + ex.Message);
            return null;
        }
    }
}
