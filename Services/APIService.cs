using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using System.Net;
using Url_Detection_Agent.Models.API.ServerDetector;

namespace Url_Detection_Agent.Services;

public class APIService : IAPIService
{
    private readonly ILogger<APIService> _logger;
    private readonly RestClient _client;
    // License will be given as an input to the class
    private readonly string _licenseTokenCookie = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpZCI6MSwiZW1haWwiOiJvcmlAYWJjLmNvbSIsInNjb3BlIjpbIioiLCJzaGFyaW5nIl0sImV4cCI6MTcxMDM1MzExNiwiaWF0IjoxNjc4ODE3MTE2LCJzdWIiOiJqd3QtY29va2llcy10ZXN0In0.JoSogsHWwtx196RfAajqgQUdNjfWLO2WBmp746pa1GU";
    public APIService(ILogger<APIService> logger)
    {
        _logger = logger;
        _client = new RestClient("http://localhost:8000/");
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
