using Titanium.Web.Proxy.Models;

namespace Url_Detection_Agent.Models;

public class UrlCacheModel
{
    public string UrlName { get; set; } = string.Empty;
    public bool? IsLegit { get; set; }
    public List<string> ReasonsForUnsafty { get; set; } = new List<string>();
    public string Body { get; set; } = string.Empty;
    public List<HttpHeader> Headers { get; set; } = new List<HttpHeader>();
    public string UrlHashCodeString { get; set; } = string.Empty;
}
