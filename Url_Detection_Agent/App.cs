using Microsoft.Extensions.Logging;
using Url_Detection_Agent.Interfaces;
using System.Windows.Forms;
using Url_Detection_Agent.Services;
using Url_Detection_Agent.Utils;
using Microsoft.Extensions.Configuration;
using static Url_Detection_Agent.Enum.Enums;

namespace Url_Detection_Agent;

public class AgentApp
{
	private readonly IProxyServerService _proxyServerService;
    private readonly IUserVerification _userVerification;
    private readonly ILogger<AgentApp> _log;

    public AgentApp(ILogger<AgentApp> log, IProxyServerService proxyServerService,IUserVerification userVerification)
    {
        _log = log;
        _proxyServerService = proxyServerService;
        _userVerification = userVerification;
    }


    public void Run(string[] args)
	{
        
        if (LicenseValidetion())
        {
		    _log.LogInformation("Agent Start");
		    _proxyServerService.RunProxy();
        }
	}
    private bool LicenseValidetion()
    {
        var programIsValidToRun = true;
        if (!_userVerification.IsLocalLicenseValid())
        {
            programIsValidToRun = false;
        }
        if (!programIsValidToRun)
        {
            var statusResult = _userVerification.ShowDialog("Please enter license key ", "License");
            programIsValidToRun = statusResult == UserVerificationStatus.Success;
        }
        return programIsValidToRun;
    }
}
