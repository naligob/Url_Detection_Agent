using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RestSharp;
using System.Net;
using Url_Detection_Agent.Models.API.ServerDetector;

namespace Url_Detection_Agent.Services;

public class APIService : IAPIService
{
    private readonly ILogger<APIService> _logger;
    private readonly RestClient _client;
    private readonly IConfiguration _configuration;
    private readonly string _licenseTokenCookie;
    public APIService(ILogger<APIService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _client = new RestClient(_configuration.GetSection("ServerConfiguration:ServerHostName").Value);
        _licenseTokenCookie = _configuration.GetSection("ClientLicence").Value;
    }

    public ServerDetectorResponse ServerDetectorAPICall(string url, string host= "localhost")
    {
        var response = new ServerDetectorResponse();

        var request = new RestRequest("detect/", Method.Post) { RequestFormat = DataFormat.Json };
        request.AddBody(new { Url_link = url });

        request.AddCookie("jwt-token", _licenseTokenCookie, "/", host);

        var res = _client.Execute<ServerDetectorResponse>(request);
        if (res.StatusCode != HttpStatusCode.OK)
            return response;
        else
            return res.Data ?? response;
    }

}
