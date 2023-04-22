using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RestSharp;
using System.Net;
using Url_Detection_Agent.Models.API.ServerDetector;
using Url_Detection_Agent.Models.API.ServerLicenseAuth;

namespace Url_Detection_Agent.Services;

public class APIService : IAPIService
{
    private readonly RestClient _client;
    private readonly IConfiguration _configuration;
    private readonly ILogger<APIService> _logger;
    private string _host;
    public APIService(IConfiguration configuration, ILogger<APIService> logger)
    {
        _configuration = configuration;
        var serverUri = _configuration.GetSection("ServerConfiguration:ServerHostName").Value;
        _client = new RestClient(serverUri);
        var uri = new Uri(serverUri);
        _host = uri.Host;
        _logger = logger;
    }

    public ServerDetectorResponse ServerDetectorAPICall(string url, string host= "localhost")
    {
        var response = new ServerDetectorResponse();

        var request = new RestRequest("detect/", Method.Post) { RequestFormat = DataFormat.Json };
        request.AddBody(new { Url_link = url });
        var token = _configuration.GetSection("ClientLicence").Value;


        request.AddCookie("jwt-token", token, "/", _host);
        var res = new RestResponse<ServerDetectorResponse>(request);
        try
        {
            res = _client.Execute<ServerDetectorResponse>(request);
        }
        catch (Exception ex)
        {
           _logger.LogError(ex.Message + ex.InnerException != null ? "\n" + ex.InnerException.Message : string.Empty);
        }
        
        if (res.StatusCode != HttpStatusCode.OK)
            return response;
        else
            return res.Data ?? response;
    }
    public ServerLicenseAuthResponse CheckLicense(string license)
    {
        var response = new ServerLicenseAuthResponse();

        var request = new RestRequest("license/auth/", Method.Post) { RequestFormat = DataFormat.Json };
        request.AddBody(new { License = license });

        var res = _client.Execute<ServerLicenseAuthResponse>(request);
        response.statusCode = res.StatusCode;
        if (res.StatusCode != HttpStatusCode.OK)
            return response;
        else
            return res.Data ?? response;
    }

}
