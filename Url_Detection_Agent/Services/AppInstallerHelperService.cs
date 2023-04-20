using Microsoft.Extensions.Configuration;

namespace Url_Detection_Agent.Services
{
    public class AppInstallerHelperService : IAppInstallerHelperService
    {
        private readonly IAPIService _aPIService;
        private readonly IConfiguration _configuration;

        public AppInstallerHelperService(IAPIService aPIService, IConfiguration configuration)
        {
            _aPIService = aPIService;
            _configuration = configuration;
        }
        public bool CheckLicenseKey(string key)
        {
            var result = false;
            var licenseCheckResult = _aPIService.CheckLicense(key);
            if (licenseCheckResult.Is_valid)
            {
                _configuration["ClientLicence"] = key;
                result = true;
            }
            return result;
        }

    }
}
