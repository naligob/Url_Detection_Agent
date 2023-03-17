using Microsoft.Extensions.Logging;
using System.Collections;
using System.Runtime.Caching;
using Url_Detection_Agent.Interfaces;
using Url_Detection_Agent.Models;

namespace Url_Detection_Agent.Services;

public class UrlMemoryCache : IDisposable, IUrlMemoryCache
{
    private MemoryCache _memoryCache;
    private readonly CacheItemPolicy _itemPolicy;
    private readonly ILogger<MemoryCache> _logger;
    public UrlMemoryCache(ILogger<MemoryCache> logger)
    {
        _memoryCache = new MemoryCache("urlsCache");
        _itemPolicy = new CacheItemPolicy
        {
            AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(120.0)
        };
        _logger = logger;
    }
    public bool AddUrl(UrlCacheModel urlCacheModel)
    {
        var result = false;

        if (!string.IsNullOrEmpty(urlCacheModel.UrlName))
        {
            if (!_memoryCache.Contains(urlCacheModel.UrlName))
            {
                _memoryCache.Add(
                    key: urlCacheModel.UrlName,
                    value: urlCacheModel,
                    policy: _itemPolicy
                );
                result = true;
            }
        }
        return result;
    }

    public void Dispose()
    {
        _memoryCache.Dispose();
    }
    // return true if key exists & isLegitUrl is the value of the saved answer from server
    public UrlCacheModel TryGetUrl(string urlKey, out bool isInCache)
    {
        var result = new UrlCacheModel();
        isInCache = false;
        if (_memoryCache.Contains(urlKey))
        {
            result = (UrlCacheModel)_memoryCache.GetCacheItem(urlKey).Value;
            isInCache = true;
        }
        //else
        //{
        //    _logger.LogInformation($"url tried to get {urlKey}");
        //    _logger.LogInformation($"InCache List \n\t {string.Join(" , ", _memoryCache.Select(x => x.Key).ToList())}");
        //}
        return result;
    }
}

