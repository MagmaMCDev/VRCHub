using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Policy;

namespace VRCHub;
public class ServerAPI: IDisposable
{
    public static readonly string[] Servers = [
        "https://vrchub.site", 
        "https://api.vrchub.site/API/2/Status",
        "https://datapacks.vrchub.site/List.php",
        "https://software.vrchub.site/Hash/"
        ];
    public static bool usingProxy = false;

    public static string GetServer(string url)
    {
        if (usingProxy)
        {
            if (url.Contains("api.vrchub.site"))
                url = url.Replace("api.vrchub.site", "magmamc.dev/ServerProxy/vrchub");
            else if (url.Contains("datapacks.vrchub.site"))
                url = url.Replace("datapacks.vrchub.site", "magmamc.dev/ServerProxy/vrchub/datapacks");
            else if (url.Contains("software.vrchub.site"))
                url = url.Replace("software.vrchub.site", "magmamc.dev/ServerProxy/vrchub/software");
            Console.WriteLine("[HTTP PROXY] " + url);
        }
        else
            Console.WriteLine("[HTTP] " + url);

        return url;
    }

    public HttpClient? HTTP;
    private static readonly object HTTPLock = new();
    public ServerAPI()
    {
        HTTP = new HttpClient();
        HTTP.DefaultRequestHeaders.Add("Connection", "keep-alive");
        HTTP.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
        HTTP.DefaultRequestHeaders.Add("Pragma", "no-cache");
        HTTP.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
        HTTP.DefaultRequestHeaders.Add("Accept", "application/json, application/zip, application/octet-stream, text/plain, image/*, */*");
        HTTP.DefaultRequestHeaders.Add("User-Agent", "HttpClient/9.0 EasyServiceAPI/1.2");
        HTTP.Timeout = TimeSpan.FromSeconds(10);
    }
    public static HttpClient CreateByteDownloader(bool cache = false)
    {
        HttpClient HTTP = new HttpClient();
        HTTP.DefaultRequestHeaders.Add("Connection", "keep-alive");
        if (!cache)
        {
            HTTP.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
            HTTP.DefaultRequestHeaders.Add("Pragma", "no-cache");
        } 
        else
            HTTP.DefaultRequestHeaders.Add("Cache-Control", "public, max-age=86400");
        HTTP.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
        HTTP.DefaultRequestHeaders.Add("Accept", "application/json, application/zip, application/octet-stream, image/*");
        HTTP.DefaultRequestHeaders.Add("User-Agent", "HttpClient/9.0 EasyServiceAPI/1.2");
        HTTP.Timeout = TimeSpan.FromMinutes(5);
        return HTTP;
    }
    public Task<string> GetStringAsync(string url) =>
        HTTP!.GetStringAsync(url);
    public Task<byte[]> GetByteArrayAsync(string url) =>
        HTTP!.GetByteArrayAsync(url);
    public Task<T?> GetFromJsonAsync<T>(string url) =>
        HTTP!.GetFromJsonAsync<T>(url);
    public bool CheckServer(string server)
    {
        bool Status = false;
        try
        {
            Console.Write("[INIT] ");
            using var request = new HttpRequestMessage(HttpMethod.Head, GetServer(server));
            using var response = HTTP!.SendAsync(request).GetAwaiter().GetResult();
            if (response.IsSuccessStatusCode)
                Status = true;
        }
        catch { }
        return Status;
    }

    public void Dispose()
    {
        lock (HTTPLock)
        {
            HTTP?.Dispose();
            HTTP = null;
        }
    }
}
