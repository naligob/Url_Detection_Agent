using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Specialized;
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
    private readonly HtmlHelperService _htmlHelperService;
    private static SemaphoreSlim _semaphore;
    public ProxyServerService(ILogger<ProxyServerService> logger, IUrlMemoryCache urlCache, IAPIService aPIService)
    {
        _logger = logger;
        _urlCache = urlCache;
        _aPIService = aPIService;
        _semaphore = new SemaphoreSlim(1);
        _htmlHelperService = new HtmlHelperService();
    }
    public void RunProxy()
    {

        var proxyServer = new ProxyServer();

        // locally trust root certificate used by this proxy
        // proxyServer.CertificateManager.TrustRootCertificate(true);

        // proxyServer.CertificateManager.CertificateEngine = Titanium.Web.Proxy.Network.CertificateEngine.DefultWindows;
        // proxyServer.CertificateManager.EnsureRootCertificate();
        // optionally set the Certificate Engine
        // Under Mono only BouncyCastle will be supported
        proxyServer.CertificateManager.CertificateEngine = Titanium.Web.Proxy.Network.CertificateEngine.BouncyCastle;

        proxyServer.BeforeRequest += OnRequest;
        proxyServer.BeforeResponse += OnResponse;
        proxyServer.ServerCertificateValidationCallback += OnCertificateValidation;
        proxyServer.ClientCertificateSelectionCallback += OnCertificateSelection;

        var explicitEndPoint = new ExplicitProxyEndPoint(IPAddress.Any, 8002, true)
        {
            // Use self-issued generic certificate on all https requests
            // Optimizes preformance by not creating a certificate for each https-enabled domain
            // Useful when certificate trust is not required by proxy clients
            //GenericCetificate = new X509Certificate2(Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "genericcert.pfx"),"password")
        };

        // Fired when a CONNECT request is received
        explicitEndPoint.BeforeTunnelConnectRequest += OnBeforeTunnelConnectedRequest;

        // An explicit endpoint is where the client jnows about the existence of a proxy
        // So client sends request in a proxy friendly manner
        proxyServer.AddEndPoint(explicitEndPoint);
        proxyServer.Start();

        // Transparent endpoint is useful for reverse proxy (client is now aware of the existence of proxy)
        // A transparent endpoint usually requires a network router port forwarding HTTP(S) packets or DNS
        // to send data to this endPoint
        var transparentEndPoint = new TransparentProxyEndPoint(IPAddress.Any, 8001, true)
        {
            // Generic Certificate hostname to use
            // when SNI is disabled by client
            GenericCertificateName = "google.com"
        };

        proxyServer.AddEndPoint(transparentEndPoint);

        //proxyServer.UpStreamHttpProxy = new ExternalProxy() { HostName = "localhost", Port = 8888 };
        //proxyServer.UpStreamHttpsProxy = new ExternalProxy() { HostName = "localhost", Port = 8888 };

        foreach (var endPoint in proxyServer.ProxyEndPoints)
            Console.WriteLine($"Listening on '{endPoint.GetType().Name}' endpoint at Ip {endPoint.IpAddress} and port: {endPoint.Port} ");

        // Only explicit proxies can be set as system proxy!
        proxyServer.SetAsSystemHttpProxy(explicitEndPoint);
        proxyServer.SetAsSystemHttpsProxy(explicitEndPoint);

        // wait here (You can use somthing else as a wait function, I am using this as a demo)
        Console.Read();

        // Unsubscribe & Quit
        explicitEndPoint.BeforeTunnelConnectRequest -= OnBeforeTunnelConnectedRequest;
        proxyServer.BeforeRequest -= OnRequest;
        proxyServer.BeforeResponse -= OnResponse;
        proxyServer.ServerCertificateValidationCallback -= OnCertificateValidation;
        proxyServer.ClientCertificateSelectionCallback -= OnCertificateSelection;

        proxyServer.Stop();
    }



    // filter suffix .js/.css/.png etc. ignore
    // filter header referer ignore
    // method : GET accept
    // header : {ConentType : text/html} accept?
    public async Task OnRequest(object sender, SessionEventArgs e)
    {
        //var t_url = e.HttpClient.Request.Url;

        //if (t_url.Contains("/agentdetectoreurl?"))
        //{
        //    await _semaphore.WaitAsync();
        //    try
        //    {
        //        var start = t_url.IndexOf("/agentdetectoreurl?") + 19;
        //        var concateString = t_url.Remove(0, start);
        //        var hashbyte = Convert.FromBase64String(concateString);
        //        var urlResult = _urlCache.TryGetUrlByHash(hashbyte);
        //        if (urlResult != null)
        //        {
        //            e.HttpClient.Response.Headers.Clear();
        //            e.Redirect(urlResult);
        //        }
        //    }
        //    finally
        //    {
        //        _semaphore.Release();
        //    }
        //    return;
        //}

        var host = e.HttpClient.Request.RequestUri.Host;

        var method = e.HttpClient.Request.Method.ToUpper();
        if (method == "GET")
        {
            if (e.HttpClient.Request.RequestUri.Segments.LastOrDefault() == "url" && host.Contains("google"))
                e.UserData = "GetFromBody";
            else if (e.HttpClient.Request.RequestUri.Segments.LastOrDefault() == "uviewer" && host.Contains("google"))
                e.UserData = "Skip";

            //// Get/Set request body bytes
            //byte[] bodyBytes = await e.GetRequestBody();
            //e.SetRequestBody(bodyBytes);

            //// Get/Set request body as string
            //string bodyString = await e.GetRequestBodyAsString();
            //e.SetRequestBodyString(bodyString);


            // store request 
            // so that you can find it from response handler
        }

        // Redirect exmple
        //if (e.HttpClient.Request.RequestUri.AbsoluteUri.Contains("wikipedia.org"))
        //{
        //    e.Redirect("https://www.paypal.com");
        //}
    }

    // Modify response
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
                //var hashbyte = Convert.FromBase64String(hashUrlString);
                var hashResult = _urlCache.TryGetUrl(hashUrlString, out bool isInCache);
                if (isInCache && hashResult != null)
                {
                    e.HttpClient.Response.Headers.Clear();
                    //e.Redirect(hashResult.Value.Url);
                    //e.SetResponseBodyString(hashResult.Value.Body);
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

        //if (e.UserData != null)
        //{
        //    // access request from UserData propery where we stored it in RequestHandler
        //    var request = (Request)e.UserData;
        //}
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
            // check if url legit
            var validUrlInCache = cacheModelResponse.IsLegit.HasValue && cacheModelResponse.IsLegit.Value;
            if (!validUrlInCache)
            {
                // make redirect 
                //Console.WriteLine($"From cache url:{cacheModelResponse.UrlName} is redirected to safe page \n the reason is {cacheModelResponse.ReasonsForUnsafty}");
                e.Ok(_htmlHelperService.GetHtmlSafePageContent(new HtmlModelContant { Url = "/agentdetectoreurl?" + cacheModelResponse.UrlHashCodeString, ReasonsList = cacheModelResponse.ReasonsForUnsafty }));
            }
            else
            {
                //Console.WriteLine($"From cache url:{cacheModelResponse.UrlName} is OK!");
            }
        }
        else // add to cache
        {
            // request to server
            var response = _aPIService.ServerDetectorAPICall(url);
            
            var newCacheItem = new UrlCacheModel { UrlName = url, UrlHashCodeString = urlHashCodeString };
            if (!response.Is_malicious)
            {
                //Console.WriteLine($"Server response {url} is legit");
                // need to add as hash url
                newCacheItem.IsLegit = true;
                _urlCache.AddUrl(newCacheItem);
            }
            else if (response.Is_malicious)
            {
                //var hashedUrl = HashSHA256Bytes(parameter: url, salt: cacheSize);
                //Console.WriteLine($"Server response {url} is DETECTED!");
                newCacheItem.Headers = e.HttpClient.Response.Headers.GetAllHeaders();
                newCacheItem.Body = body;
                newCacheItem.IsLegit = false;
                var topReasons = GetTopKReasons(dataObject: response.Reason?.ToString() ?? string.Empty, numberOfReasons: 3);
                newCacheItem.ReasonsForUnsafty = topReasons;
                _urlCache.AddUrl(newCacheItem);
                e.Ok(_htmlHelperService.GetHtmlSafePageContent(new HtmlModelContant { Url = "/agentdetectoreurl?" + urlHashCodeString, ReasonsList = topReasons }));
            }
        }
    }

    private List<string>? GetTopKReasons(string dataObject, int numberOfReasons)
    {
        var deserializeReasons = JsonConvert.DeserializeObject<Dictionary<string, double>>(dataObject)?.ToList() ?? new() { new(key: "Sorry no reasons was found", value: 0) };
        deserializeReasons.Sort((pair1, pair2) => pair1.Value.CompareTo(pair2.Value));
        if (!deserializeReasons.Any())
            return new() { "Sorry no reasons was found" };
        return deserializeReasons.TakeLast(numberOfReasons).Select(x => x.Key.Replace("_"," ").ToUpper()).Reverse().ToList();
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
            //var dtNow = DateTime.Now;
            if (e.UserData != null)
            {
                var request = (string)e.UserData;
                if (request == "GetFromBody")
                {
                    string urlFromGoogle = GetFromBodyUrl(body);
                    result = urlFromGoogle;
                    //Console.WriteLine($"{dtNow} OnResponse :\n\tURL:{urlFromGoogle}");
                }
            }
            else if (UrlValidationWithContentType(e))
            {
                var host = e.HttpClient.Request.RequestUri.Host ?? "";
                var url = e.HttpClient.Request.Url;

                var refererHeader = GetHeaderIfExists(e.HttpClient.Request.Headers, "Referer", out bool refererExists);

                if (!refererExists)
                {
                    //Console.WriteLine($"{dtNow} OnResponse :\n\tURL:{url}");
                    result = url;
                }
                else if (refererHeader.Contains(host))
                {
                    //Console.WriteLine($"{dtNow} OnResponse :\n\tURL:{url}\n\n\tReferer:{refererHeader}\n\n\tHost:{host}");
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


    // Allows overriding default client certificate selection logic durng mutual authentication
    public Task OnCertificateSelection(object sender, CertificateSelectionEventArgs e)
    {
        // set e.clientCertificate to override
        return Task.CompletedTask;
    }
    // Allows overriding default certificate validation logic
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
}
