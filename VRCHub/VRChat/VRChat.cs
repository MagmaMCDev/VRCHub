using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MagmaMC.SharedLibrary;
using OpenVRChatAPI;
using OpenVRChatAPI.Models;

namespace VRCHub;

#pragma warning disable IDE1006 // Naming Styles

public class VRCAuth
{
    public string? Auth
    {
        get; set;
    }
    public string? TwoFactorAuth
    {
        get; set;
    }
    public string? TwoFactorAuthType
    {
        get; set;
    }
    public bool? requiresEmailAuth
    {
        get; set;
    }

    public static implicit operator OpenVRChatAPI.Models.VRCAuth(VRCAuth v)
    {
        OpenVRChatAPI.Models.VRCAuth auth = new()
        {
            auth = v.Auth,
            twoFactorAuth = v.TwoFactorAuth,
        };
        return auth;
    }
}
public class VRCAccount: OpenVRChatAPI.Models.VRCAuth
{
    public string id
    {
        get; set;
    } = "";
    public string username
    {
        get; set;
    } = "";
    public string email
    {
        get; set;
    } = "";
    public string password
    {
        get; set;
    } = "";
    public bool main
    {
        get; set;
    } = false;
    public void Login()
    {
        VRChat.Auth = this;
    }
    public bool Validate()
    {
        SimpleLogger.Debug("Validating User: " + username);
        var backup = VRChat.Auth;
        VRChat.Auth = this;
        bool authed = VRChat.CheckAuth();
        if (backup != null)
            VRChat.Auth = backup;
        if (authed)
            SimpleLogger.Debug("Validated User: " + username);
        else
            SimpleLogger.Warn("Unvalidated User: " + username);
        return authed;
    }
}
public enum TrustRank
{
    Visitor,
    New,
    User,
    Known,
    Trusted,
    Administrator,
    Developer
}
public class VRCUser
{
    public string GetStatus() =>
        (string.IsNullOrEmpty(statusDescription) ? status : statusDescription) ?? "Online";
    public TrustRank GetRank()
    {
        if (Common.UList.Contains(id.Substring(4)))
            return TrustRank.Developer;
        else if(tags.Contains("admin_moderator") || tags.Contains("system_notamod"))
            return TrustRank.Administrator;
        else if(tags.Contains("system_trust_trusted"))
            return TrustRank.Trusted;
        else if(tags.Contains("system_trust_veteran"))
            return TrustRank.Known;
        else if(tags.Contains("system_trust_known"))
            return TrustRank.User;
        else if (tags.Contains("system_trust_basic"))
            return TrustRank.New;
        else
            return TrustRank.Visitor;
    }
    public string? bio
    {
        get; set;
    }
    public string? currentAvatarImageUrl
    {
        get; set;
    }
    public string? currentAvatarThumbnailImageUrl
    {
        get; set;
    }
    public string date_joined
    {
        get; set;
    } = "";
    public string displayName
    {
        get; set;
    } = "";
    public string id
    {
        get; set;
    } = "";
    [JsonIgnore]
    public bool Adult => isAdult && ageVerified;
    public bool isAdult
    {
        get; set;
    }
    public bool ageVerified
    {
        get; set;
    }
    public bool allowAvatarCopying
    {
        get; set;
    }
    public string? last_platform
    {
        get; set;
    }
    public string? profilePicOverride
    {
        get; set;
    }
    public string? profilePicOverrideThumbnail
    {
        get; set;
    }
    public string? pronouns
    {
        get; set;
    }
    public string? state
    {
        get; set;
    }
    public string? status
    {
        get; set;
    }
    public string? statusDescription
    {
        get; set;
    }
    public string[] tags
    {
        get; set;
    } = [];
    public string? userIcon
    {
        get; set;
    }
    public string username
    {
        get; set;
    } = "";
}
#pragma warning restore IDE1006 // Naming Styles

public static class VRCAPI
{
    public static readonly Uri APIWebsite = new("https://api.vrchat.cloud");
    public static readonly Uri APIBase = new("https://api.vrchat.cloud/api/1/");
    public static VRCAuth? Login(string username, string password, string? code = null)
    {
        CookieContainer Cookies = new CookieContainer();
        HttpClientHandler handler = new HttpClientHandler();
        handler.CookieContainer = Cookies;
        handler.UseCookies = true;
        handler.AllowAutoRedirect = true;
        HttpClient HTTPClient = new HttpClient(handler);
        HTTPClient.BaseAddress = APIBase;
        HTTPClient.DefaultRequestHeaders.Add("User-Agent", VRChat.User_Agent);
        HTTPClient.DefaultRequestHeaders.Add("Authorization", new AuthHeader(username, password).Value);
        HttpResponseMessage content = HTTPClient.GetAsync("auth/user").GetAwaiter().GetResult();
        string Response = content.Content.ReadAsStringAsync().GetAwaiter().GetResult().ToLower();
        if (Response.Contains("missing credentials") || Response.Contains("invalid username/email"))
            return null;

        if (Response.Contains("it looks like you're logging in from somewhere new!"))
        {
            VRCAuth auth = new VRCAuth();
            auth.requiresEmailAuth = true;
            foreach (Cookie cookie in Cookies.GetCookies(APIWebsite))
            {
                switch (cookie.Name.ToLower())
                {
                    case "auth":
                        auth.Auth = cookie.Value;
                        break;
                }
            }
            return auth;
        }

        else if (Response.Contains("requirestwofactorauth"))
        {
            VRCAuth auth = new VRCAuth();
            TwoFactorAuth twoFactorAuth = JsonSerializer.Deserialize<TwoFactorAuth>(Response)!;
            auth.TwoFactorAuthType = twoFactorAuth.requirestwofactorauth.FirstOrDefault();
            if (code == null)
            {
                foreach (Cookie cookie in Cookies.GetCookies(APIWebsite))
                {
                    switch (cookie.Name.ToLower())
                    {
                        case "auth":
                            auth.Auth = cookie.Value;
                            break;
                    }
                }
                return auth;
            }
            else
            {
                TwoFactorAuth Verified = VerifyTwoFA(ref HTTPClient, auth.TwoFactorAuthType!, code);
                if (Verified.verified)
                {
                    auth.TwoFactorAuthType = null;
                    foreach (Cookie cookie in Cookies.GetCookies(APIWebsite))
                    {
                        switch (cookie.Name.ToLower())
                        {
                            case "auth":
                                auth.Auth = cookie.Value;
                                break;
                            case "twofactorauth":
                                auth.TwoFactorAuth = cookie.Value;
                                break;
                        }
                    }
                }
                HTTPClient.Dispose();
                handler.Dispose();
                return auth;
            }
        }



        VRCAuth _auth = new VRCAuth();
        foreach (Cookie cookie in Cookies.GetCookies(APIWebsite))
        {
            switch (cookie.Name.ToLower())
            {
                case "auth":
                    _auth.Auth = cookie.Value;
                    break;
            }
        }
        HTTPClient.Dispose();
        handler.Dispose();
        return _auth;
    }
    private static TwoFactorAuth VerifyTwoFA(ref HttpClient HTTPClient, string TwoFAType, string Code)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"auth/twofactorauth/{TwoFAType.ToLower()}/verify")
        {
            Content = new StringContent(JsonSerializer.Serialize(new _2FACode(Code.ToString())), Encoding.UTF8, "application/json")
        };

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
        request.Headers.Referrer = new Uri("https://vrchat.com/home/emailtwofactorauth");
        request.Headers.Add("Origin", "https://vrchat.com");
        request.Headers.Add("DNT", "1");
        request.Headers.Add("Sec-Fetch-Dest", "empty");
        request.Headers.Add("Sec-Fetch-Mode", "cors");
        request.Headers.Add("Sec-Fetch-Site", "same-origin");
        request.Headers.Add("Sec-GPC", "1");
        request.Headers.ConnectionClose = false;

        HttpResponseMessage response = HTTPClient.SendAsync(request).Result;
        string responseString = response.Content.ReadAsStringAsync().Result;
        request.Headers.Clear();
        SimpleLogger.Debug(responseString);
        return JsonSerializer.Deserialize<TwoFactorAuth>(responseString)!;
    }

    private static readonly ConcurrentBag<HttpClient> _httpClientBag = new();

    public static bool LoggedIn => CheckAuth();
    public static bool CheckAuth()
    {
        if (VRChat.Auth == null || 
            string.IsNullOrWhiteSpace(VRChat.Auth.auth) || 
            string.IsNullOrWhiteSpace(VRChat.Auth.twoFactorAuth))
            return false;
        HttpClient Client = GetHttpClient();
        HttpResponseMessage content = Client.GetAsync("auth/user").GetAwaiter().GetResult();
        ReturnHttpClient(Client);
        return content.IsSuccessStatusCode;
    }
    public static HttpClient GetHttpClient()
    {
        if (_httpClientBag.TryTake(out var httpClient))
            return httpClient;
        else
        {
            HttpClientHandler ClientHandler = new HttpClientHandler();
            ClientHandler.AllowAutoRedirect = true;
            ClientHandler.CookieContainer = new CookieContainer();
            ClientHandler.CookieContainer.Add(APIWebsite, new Cookie("auth", VRChat.Auth.auth));
            ClientHandler.CookieContainer.Add(APIWebsite, new Cookie("twoFactorAuth", VRChat.Auth.twoFactorAuth));
            HttpClient Client = new(ClientHandler);
            Client.BaseAddress = APIBase;
            Client.DefaultRequestHeaders.Add("User-Agent", VRChat.User_Agent);
            return Client;
        }
    }
    public static void ReturnHttpClient(HttpClient httpClient) => _httpClientBag.Add(httpClient);


    public static VRCUser? GetUser(string UserID)
    {
        HttpClient API = GetHttpClient();
        try
        {
            return API.GetFromJsonAsync<VRCUser>($"users/{UserID}").GetResult();
        }
        finally
        {
            ReturnHttpClient(API);
        }
    }
    public static VRCUser? GetUser()
    {
        HttpClient API = GetHttpClient();
        try
        {
            return API.GetFromJsonAsync<VRCUser>("auth/user").GetResult();
        }
        finally
        {
            ReturnHttpClient(API);
        }

    }

    public static VRCAvatar[]? GetFavoriteAvatars()
    {
        HttpClient API = GetHttpClient();
        try
        {
            return API.GetFromJsonAsync<VRCAvatar[]?>("avatars/favorites").GetResult();
        }
        finally
        {
            ReturnHttpClient(API);
        }

    }

    public static Bitmap? DownloadImage(ref HttpClient httpClient, string url)
    {
        Bitmap? image = null;
        try
        {
            byte[]? imagebytes = null;
#if INCLUDE_CLIENTCACHE
            if (!ClientDatabase.ImageCache.TryGetValue(url, out imagebytes))
#else
            if (imagebytes == null)
#endif
            {
                using var response = httpClient.GetAsync(url).GetResult();
                if (!response.IsSuccessStatusCode)
                    return null;

                imagebytes = response.Content.ReadAsByteArrayAsync().GetResult();
                using var stream = new MemoryStream(imagebytes);
                image = new(stream);
            }
#if INCLUDE_CLIENTCACHE
            ClientDatabase.ImageCache.TryAdd(url, imagebytes);
#endif
            if (imagebytes != null)
                image = new(new MemoryStream(imagebytes));
        }
        catch { }

        return image;
    }
}