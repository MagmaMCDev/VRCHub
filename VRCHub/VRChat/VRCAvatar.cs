using System.Net.Http;
using System.Net.Http.Json;
using VRCHub;

namespace VRCHub;
public class VRCAvatar
{
    public string authorId
    {
        get; set;
    } = "";
    public string authorName
    {
        get; set;
    } = "";
    public string description
    {
        get; set;
    } = "";
    public string name
    {
        get; set;
    } = "";
    public string id
    {
        get; set;
    } = "";
    public string imageUrl
    {
        get; set;
    } = "";
    public string thumbnailImageUrl
    {
        get; set;
    } = "";
    public string updated_at
    {
        get; set;
    } = "";
    public uint version
    {
        get; set;
    } = 0;

    public VRCAvatar()
    {
    }

    public VRCAvatar(string ID)
    {
        id = ID;
    }

}
