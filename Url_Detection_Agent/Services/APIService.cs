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
    //private readonly string _licenseTokenCookie;
    public APIService(IConfiguration configuration)
    {
        _configuration = configuration;
        _client = new RestClient(_configuration.GetSection("ServerConfiguration:ServerHostName").Value);
        //_licenseTokenCookie = _configuration.GetSection("ClientLicence").Value;
    }

    public ServerDetectorResponse ServerDetectorAPICall(string url, string host= "localhost")
    {
        var response = new ServerDetectorResponse();

        var request = new RestRequest("detect/", Method.Post) { RequestFormat = DataFormat.Json };
        request.AddBody(new { Url_link = url });
        var token = _configuration.GetSection("ClientLicence").Value;
        request.AddCookie("jwt-token", token , "/", "192.168.68.103");

        var res = _client.Execute<ServerDetectorResponse>(request);
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
        if (res.StatusCode != HttpStatusCode.OK)
            return response;
        else
            return res.Data ?? response;
    }

}
