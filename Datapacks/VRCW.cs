using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text;
using System.Text.Json.Serialization;

namespace VRCHub.Models;
public class VRCW
{
    public string authorId { get; set; } = "";
    public string authorName { get; set; } = "";
    public string description { get; set; } = "";
    public string id { get; set; } = "";
    public string name { get; set; } = "";
    public string thumbnailImageUrl { get; set; } = "";
    public int version { get; set; } = 0;
    public string updated_at { get; set; } = "";
    public int visits { get; set; } = 0;

}
