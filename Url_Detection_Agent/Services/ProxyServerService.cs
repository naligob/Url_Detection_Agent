using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Http;
using Titanium.Web.Proxy.Models;
using Url_Detection_Agent.Interfaces;
using Url_Detection_Agent.Models;

namespace Url_Detection_Agent.Services;

public class ProxyServerService : IProxyServerService
{
    private readonly ILogger<ProxyServerService> _logger;
    private IUrlMemoryCache _urlCache;
    private IAPIService _aPIService;
    private readonly IHtmlHelperService _htmlHelperService;
    private static SemaphoreSlim _semaphore = new SemaphoreSlim(1);
    private readonly IConfiguration _configuration;
    private readonly ProxyServer _proxyServer = new ProxyServer();
    private ExplicitProxyEndPoint _explicitEndPoint;
    public ProxyServerService(ILogger<ProxyServerService> logger, IUrlMemoryCache urlCache, IAPIService aPIService, IConfiguration configuration, IHtmlHelperService htmlHelperService)
    {
        _logger = logger;
        _urlCache = urlCache;
        _aPIService = aPIService;
        _htmlHelperService = htmlHelperService;
        _configuration = configuration;
    }
    public void RunProxy()
    {

        //_proxyServer = new ProxyServer();
        // Assign the exception handler delegate
        //_proxyServer.ExceptionFunc = HandleProxyException;


        _proxyServer.CertificateManager.CertificateEngine = Titanium.Web.Proxy.Network.CertificateEngine.BouncyCastle;

        _proxyServer.BeforeRequest += OnRequest;
        _proxyServer.BeforeResponse += OnResponse;
        _proxyServer.ServerCertificateValidationCallback += OnCertificateValidation;
        _proxyServer.ClientCertificateSelectionCallback += OnCertificateSelection;

        _explicitEndPoint = new ExplicitProxyEndPoint(IPAddress.Any, 8010, true) {};

        // Fired when a CONNECT request is received
        _explicitEndPoint.BeforeTunnelConnectRequest += OnBeforeTunnelConnectedRequest;

        // An explicit endpoint is where the client jnows about the existence of a proxy
        // So client sends request in a proxy friendly manner
        _proxyServer.AddEndPoint(_explicitEndPoint);
        _proxyServer.Start();

        // Transparent endpoint is useful for reverse proxy (client is now aware of the existence of proxy)
        // A transparent endpoint usually requires a network router port forwarding HTTP(S) packets or DNS
        // to send data to this endPoint
        var transparentEndPoint = new TransparentProxyEndPoint(IPAddress.Any, 8009, true)
        {
            // Generic Certificate hostname to use
            // when SNI is disabled by client
            GenericCertificateName = "google.com"
        };

        _proxyServer.AddEndPoint(transparentEndPoint);

        foreach (var endPoint in _proxyServer.ProxyEndPoints)
            _logger.LogInformation($"Listening on '{endPoint.GetType().Name}' endpoint at Ip {endPoint.IpAddress} and port: {endPoint.Port} ");

        // Only explicit proxies can be set as system proxy!
        _proxyServer.SetAsSystemHttpProxy(_explicitEndPoint);
        _proxyServer.SetAsSystemHttpsProxy(_explicitEndPoint);

        // wait here (You can use somthing else as a wait function, I am using this as a demo)
        Console.Read();

        StopProxy();
        // Unsubscribe & Quit
        //explicitEndPoint.BeforeTunnelConnectRequest -= OnBeforeTunnelConnectedRequest;
        //proxyServer.BeforeRequest -= OnRequest;
        //proxyServer.BeforeResponse -= OnResponse;
        //proxyServer.ServerCertificateValidationCallback -= OnCertificateValidation;
        //proxyServer.ClientCertificateSelectionCallback -= OnCertificateSelection;

        //proxyServer.Stop();

    }
    private void StopProxy()
    {
        // Unsubscribe & Quit
        _explicitEndPoint.BeforeTunnelConnectRequest -= OnBeforeTunnelConnectedRequest;
        _proxyServer.BeforeRequest -= OnRequest;
        _proxyServer.BeforeResponse -= OnResponseActionCatch;
        _proxyServer.ServerCertificateValidationCallback -= OnCertificateValidation;
        _proxyServer.ClientCertificateSelectionCallback -= OnCertificateSelection;
        if(_proxyServer.ProxyRunning)
            _proxyServer.Stop();
    }

    public async Task OnRequest(object sender, SessionEventArgs e)
    {
        var host = e.HttpClient.Request.RequestUri.Host;
        
        //TODO add skip validetion for web host and server host

        var method = e.HttpClient.Request.Method.ToUpper();
        if (method == "GET")
        {
            if (e.HttpClient.Request.RequestUri.Segments.LastOrDefault() == "url" && host.Contains("google"))
                e.UserData = "GetFromBody";
            else if (e.HttpClient.Request.RequestUri.Segments.LastOrDefault() == "uviewer" && host.Contains("google"))
                e.UserData = "Skip";

        }
    }

    public async Task OnResponse(object sender, SessionEventArgs e)
    {
        var t_url = e.HttpClient.Request.Url;

        if (t_url.Contains("/agentdetectoreurl?"))
        {
            await _semaphore.WaitAsync();
            try
            {
                var start = t_url.IndexOf("/agentdetectoreurl?") + 19;
                var hashUrlString = t_url.Remove(0, start);
                var hashResult = _urlCache.TryGetUrl(hashUrlString, out bool isInCache);
                if (isInCache && hashResult != null)
                {
                    e.HttpClient.Response.Headers.Clear();
                    e.Ok(hashResult.Body, hashResult.Headers);
                }
            }
            finally
            {
                _semaphore.Release();
            }
            return;
        }
        else
        {
            if (e.HttpClient.Request.Method == "GET" && e.HttpClient.Response.StatusCode == 200)
            {
                byte[] bodyBytes = await e.GetResponseBody();
                e.SetResponseBody(bodyBytes);

                string body = await e.GetResponseBodyAsString();
                string url = GetServerUrlToDetect(e, body);
                if (!string.IsNullOrEmpty(url))
                {
                    await _semaphore.WaitAsync();
                    try
                    {
                        AgentURLHandler(url, e, body);
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }
                e.SetRequestBodyString(body);
            }
        }
    }

    private void AgentURLHandler(string url, SessionEventArgs e, string body)
    {
        var urlHashCodeString = Convert.ToBase64String(
                                        HashSHA256Bytes(
                                            parameter: url,
                                            salt: 0
                                        ));
        var cacheModelResponse = _urlCache.TryGetUrl(urlHashCodeString, out bool isInCache);
        if (isInCache)
        {
            var validUrlInCache = cacheModelResponse.IsLegit.HasValue && cacheModelResponse.IsLegit.Value;
            if (!validUrlInCache)
            {
                SetRedirectResponse(e, cacheModelResponse);
            }
        }
        else
        {
            var serverResponse = _aPIService.ServerDetectorAPICall(url);

            var newCacheItem = new UrlCacheModel
            {
                UrlName = url,
                UrlHashCodeString = urlHashCodeString,
                Headers = e.HttpClient.Response.Headers.GetAllHeaders(),
                Body = body
            };

            if (serverResponse.Is_malicious)
            {
                newCacheItem.IsLegit = false;
                var topReasons = GetTopKReasons(dataObject: serverResponse.Reason?.ToString() ?? string.Empty, numberOfReasons: 3);
                newCacheItem.ReasonsForUnsafty = topReasons;
                _urlCache.AddUrl(newCacheItem);
                SetRedirectResponse(e, newCacheItem);
            }
            else
            {
                newCacheItem.IsLegit = true;
                _urlCache.AddUrl(newCacheItem);
            }
        }
    }

    private void SetRedirectResponse(SessionEventArgs e, UrlCacheModel cacheModelResponse)
        => e.Ok(_htmlHelperService.GetHtmlSafePageContent(
                    new HtmlModelContant
                    {
                        Url = "/agentdetectoreurl?" + cacheModelResponse.UrlHashCodeString,
                        ReasonsList = cacheModelResponse.ReasonsForUnsafty
                    }));

    private List<string> GetTopKReasons(string dataObject, int numberOfReasons)
    {
        var deserializeReasons = JsonConvert.DeserializeObject<Dictionary<string, double>>(dataObject)?.ToList() ?? new() { new(key: "Sorry no reasons was found", value: 0) };
        deserializeReasons.Sort((pair1, pair2) => pair1.Value.CompareTo(pair2.Value));
        if (!deserializeReasons.Any())
            return new() { "Sorry no reasons was found" };
        return deserializeReasons.TakeLast(numberOfReasons).Select(x => x.Key.Replace("_", " ").ToUpper()).Reverse().ToList();
    }

    private byte[] HashSHA256Bytes(string parameter, int salt)
    {
        using (HashAlgorithm algorithm = SHA256.Create())
            return algorithm.ComputeHash(Encoding.UTF8.GetBytes(parameter));
    }

    private string GetServerUrlToDetect(SessionEventArgs e, string body)
    {
        var result = "";
        if (UrlValidationWithSeffix(e.HttpClient.Request.RequestUri.Segments.LastOrDefault() ?? "") && body.Contains("<html"))
        {
            if (e.UserData != null)
            {
                var request = (string)e.UserData;
                if (request == "GetFromBody")
                {
                    string urlFromGoogle = GetFromBodyUrl(body);
                    result = urlFromGoogle;
                }
            }
            else if (UrlValidationWithContentType(e))
            {
                var host = e.HttpClient.Request.RequestUri.Host ?? "";
                var url = e.HttpClient.Request.Url;

                var refererHeader = GetHeaderIfExists(e.HttpClient.Request.Headers, "Referer", out bool refererExists);

                if (!refererExists)
                {
                    result = url;
                }
                else if (refererHeader.Contains(host))
                {
                    result = url;
                }
            }
        }
        return result;
    }

    private string GetHeaderIfExists(HeaderCollection headers, string requierdHeader, out bool refererExists)
    {
        var result = "";
        refererExists = false;
        if (headers.HeaderExists(requierdHeader))
        {
            refererExists = true;
            result = headers.Headers[requierdHeader].Value;
        }
        return result;
    }

    /// <summary>
    /// Get Url From string body, in case google redirecting to url.
    /// </summary>
    /// <param name="body"></param>
    /// <returns></returns>
    private string GetFromBodyUrl(string body)
    {
        var start = body.IndexOf("redirectUrl='") + 13;
        var concateString = body.Remove(0, start);
        return concateString.Substring(0, concateString.IndexOf("'"));
    }

    /// <summary>
    /// Method that check if the suffix of the url is not js / css
    /// </summary>
    /// <param name="lastUrlSegment"></param>
    /// <returns></returns>
    private bool UrlValidationWithSeffix(string lastUrlSegment)
    => string.IsNullOrEmpty(lastUrlSegment) ?
        true :
        !lastUrlSegment.ToLower().EndsWith(".js") && !lastUrlSegment.ToLower().EndsWith(".css");

    /// <summary>
    /// Method that check if content-type 
    /// header exsits must be with currect value
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    private bool UrlValidationWithContentType(SessionEventArgs e)
    {
        var result = true;
        if (e.HttpClient.Response.Headers.HeaderExists("Content-Type"))
        {
            if (!e.HttpClient.Response.Headers.Headers["Content-Type"].Value.Contains("text/html"))
                result = false;
        }
        return result;
    }

    public Task OnCertificateSelection(object sender, CertificateSelectionEventArgs e)
    {
        // set e.clientCertificate to override
        return Task.CompletedTask;
    }
    public Task OnCertificateValidation(object sender, CertificateValidationEventArgs e)
    {
        // set IsValid to true/false based on Certificate Errors
        if (e.SslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
            e.IsValid = true;

        return Task.CompletedTask;
    }

    private async Task OnBeforeTunnelConnectedRequest(object sender, TunnelConnectSessionEventArgs e)
    {
        string hostname = e.HttpClient.Request.RequestUri.Host;

        if (hostname.Contains("dropbox.com"))
        {
            // Exclude Https addresses you don't want to proxy
            // Useful for client that use certificate pinning
            // for exmple dropbox.com
            e.DecryptSsl = false;
        }
    }

    public async Task OnResponseActionCatch(object sender, SessionEventArgs e)
    {
        try
        {
            await OnRequest(sender,e);
        }
        catch (Exception ex)
        {
            _logger.LogError($"An exception occurred: {ex.Message}");
            if (_proxyServer.ProxyRunning)
            {
                StopProxy();
                _logger.LogInformation("The application closed and unsubscribe!");
            }
            else
            {
                _logger.LogInformation("The application closed but the proxy never run");
            }
        }
    }
}
