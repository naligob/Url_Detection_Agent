using Url_Detection_Agent.Models;

namespace Url_Detection_Agent.Interfaces;

public interface IUrlMemoryCache
{
    bool AddUrl(UrlCacheModel url);
    void Dispose();
    UrlCacheModel TryGetUrl(string urlKey, out bool isInCache);
    int GetCacheSize();
    //UrlCacheModel? TryGetUrlByHash(byte[] hash);
}
