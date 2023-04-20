using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Runtime.Caching;
using Url_Detection_Agent.Interfaces;
using Url_Detection_Agent.Models;

namespace Url_Detection_Agent.Services;

public class UrlMemoryCache : IDisposable, IUrlMemoryCache
{
    private MemoryCache _memoryCache;
    private readonly CacheItemPolicy _itemPolicy;
    private readonly ILogger<MemoryCache> _logger;
    private readonly IConfiguration _configuration;

    public UrlMemoryCache(ILogger<MemoryCache> logger, IConfiguration configuration)
    {
        _memoryCache = new MemoryCache("urlsCache");
        _configuration = configuration;
        _itemPolicy = new CacheItemPolicy
        {
            SlidingExpiration = TimeSpan.FromMinutes(int.Parse(configuration.GetSection("CacheItemPolicyTimeSpanInMinuts").Value)),
#if DEBUG
            RemovedCallback = (args) => { logger.LogDebug($"\nitem was removed: {args.CacheItem.Key}\n"); }
#endif
        };
        _logger = logger;
    }
    public bool AddUrl(UrlCacheModel urlCacheModel)
    {
        var result = false;

        if (!string.IsNullOrEmpty(urlCacheModel.UrlHashCodeString))
        {
            if (!_memoryCache.Contains(urlCacheModel.UrlHashCodeString))
            {
                result = _memoryCache.Add(
                    key: urlCacheModel.UrlHashCodeString,
                    value: urlCacheModel,
                    policy: _itemPolicy
                );
#if DEBUG
                if (!urlCacheModel.IsLegit.Value)
                    _logger.LogDebug($"\nitem added:{urlCacheModel.UrlHashCodeString}\n not legit! \n result of addition: {result} ");
#endif
            }
        }
        return result;
    }
    public void Dispose()
    {
        _memoryCache.Dispose();
    }
    public UrlCacheModel TryGetUrl(string urlKey, out bool isInCache)
    {
        var result = new UrlCacheModel();
        isInCache = false;
        if (_memoryCache.Contains(urlKey))
        {
            result = (UrlCacheModel)_memoryCache.GetCacheItem(urlKey).Value;
            isInCache = true;
        }
        return result;
    }
}

