namespace Url_Detection_Agent.Models;

public class UrlCacheModel
{
    public string UrlName { get; set; } = string.Empty;
    public bool? IsLegit { get; set; }
    public string ReasonUrlUnsafe { get; set; } = string.Empty;
}
