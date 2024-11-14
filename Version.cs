using System.Net.Http;
using System.Text.Json;
namespace VRCHub;
public class Version : IComparable<Version>
{
    public byte major
    {
        get; private set;
    }
    public byte minor
    {
        get; private set;
    }
    public byte patch
    {
        get; private set;
    }
    public string edition
    {
        get; private set;
    }
    public Uri? updateURL
    {
        get; private set;
    }

    public Version(byte major, byte minor, byte patch, string edition = "")
    {
        this.major = major;
        this.minor = minor;
        this.patch = patch;
        this.edition = edition;
        updateURL = null;
    }
    public Version(string versionString)
    {
        var versionParts = versionString.Split('-');
        var versionNumbers = versionParts[0].Split('.');
        major = byte.Parse(versionNumbers[0]);
        minor = byte.Parse(versionNumbers[1]);
        patch = byte.Parse(versionNumbers[2]);
        edition = versionParts.Length > 1 ? versionParts[1] : "";
        updateURL = null;
    }


    public void SetUpdateURL(string url)
    {
        updateURL = new Uri(url);
    }
    public void SetUpdateURL(Uri url)
    {
        updateURL = url;
    }

    public Version? GetLatestVersion()
    {
        if (updateURL == null)
        {
            Console.WriteLine("Update URL is not set.");
            return null;
        }

        try
        {
#if !NET6_0_OR_GREATER
            using WebClient client = new WebClient();

            string json = client.DownloadString(updateURL);
#else
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(8);
            var json = httpClient.GetStringAsync(updateURL).GetAwaiter().GetResult();
#endif
            var updateInfo = JsonDocument.Parse(json);

            return new(updateInfo.RootElement.GetProperty("version").GetString()!)
            {
                updateURL = new Uri(updateInfo.RootElement.GetProperty("downloadURL").GetString()!)
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while checking for updates: {ex.Message}");
            return null;
        }
    }




    public override string ToString() => edition != "" ? $"{major}.{minor}.{patch}-{edition}" : $"{major}.{minor}.{patch}";
    public static Version Parse(string version)
    {
        return new Version(version);
    }
    public int CompareTo(Version? other)
    {
#pragma warning disable IDE0011
        if (((object?)other) == null) return 1;

        if (major != other.major) return major.CompareTo(other.major);
        if (minor != other.minor) return minor.CompareTo(other.minor);
        if (patch != other.patch) return patch.CompareTo(other.patch);

        if (edition == "" && other.edition != "") return 1;
        else if (edition != "" && other.edition == "") return -1;
        else if (edition != "" && other.edition != "") return string.Compare(edition, other.edition, StringComparison.Ordinal);

        return 0;
#pragma warning restore IDE0011
    }
    public static bool operator ==(Version? v1, Version? v2)
    {
#pragma warning disable IDE0011
        if (((object?)v1) == null && ((object?)v2) == null)
            return true;
        if (((object?)v1) == null || ((object?)v2) == null)
            return false;
        return v1.CompareTo(v2) == 0;
#pragma warning restore IDE0011
    }
    public static bool operator !=(Version v1, Version? v2)
    {
        return v1.CompareTo(v2) != 0;
    }
    public static bool operator <(Version v1, Version? v2)
    {
        return v1.CompareTo(v2) < 0;
    }
    public static bool operator >(Version v1, Version? v2)
    {
        return v1.CompareTo(v2) > 0;
    }
    public static bool operator <=(Version v1, Version? v2)
    {
        return v1.CompareTo(v2) <= 0;
    }
    public static bool operator >=(Version v1, Version? v2)
    {
        return v1.CompareTo(v2) >= 0;
    }
    public override bool Equals(object? obj)
    {
#pragma warning disable IDE0011
        if (obj == null || GetType() != obj.GetType())
            return false;

        var other = (Version)obj;
        return major == other.major && minor == other.minor && patch == other.patch && edition == other.edition;
#pragma warning restore IDE0011
    }
    public override int GetHashCode()
    {
        return (major << 16) ^ (minor << 8) ^ patch ^ edition.GetHashCode();
    }
}
