using Url_Detection_Agent.Models.API.ServerDetector;
using Url_Detection_Agent.Models.API.ServerLicenseAuth;

namespace Url_Detection_Agent.Services
{
    public interface IAPIService
    {
        ServerDetectorResponse ServerDetectorAPICall(string url, string host = "localhost");
        ServerLicenseAuthResponse CheckLicense(string license);
    }
}