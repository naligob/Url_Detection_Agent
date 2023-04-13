using Url_Detection_Agent.Models.API.ServerDetector;

namespace Url_Detection_Agent.Services
{
    public interface IAPIService
    {
        ServerDetectorResponse ServerDetectorAPICall(string url, string host = "localhost");
    }
}