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
    public UrlMemoryCache(ILogger<MemoryCache> logger)
    {
        _memoryCache = new MemoryCache("urlsCache");
        _itemPolicy = new CacheItemPolicy
        {
            SlidingExpiration = TimeSpan.FromMinutes(2),
            RemovedCallback = (args) => { Console.WriteLine($"\nitem was removed: {args.CacheItem.Key}\n"); }
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
                if (!urlCacheModel.IsLegit.Value)
                    Console.WriteLine($"\nitem added:{urlCacheModel.UrlHashCodeString}\n not legit! \n result of addition: {result} ");
            }
        }
        return result;
    }
    public int GetCacheSize()
    {
        return _memoryCache.Count();
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
    //public UrlCacheModel? TryGetUrlByHash(byte[] hash)
    //{

    //    var values = _memoryCache.Select(x => (UrlCacheModel)x.Value).ToList();
    //    if (values.Any())
    //    {
    //        var result = values.FirstOrDefault(x => x.UrlHashCode != null && x.UrlHashCode.SequenceEqual(hash));
    //        return result != null ? result : null;
    //    }
    //    return null;
    //}
}

