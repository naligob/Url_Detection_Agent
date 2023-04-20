using Microsoft.Extensions.Logging;
using Url_Detection_Agent.Interfaces;
using System.Windows.Forms;
using Url_Detection_Agent.Services;
using Url_Detection_Agent.Utils;
using Microsoft.Extensions.Configuration;

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
        var programIsValidToRun = true;
        if (!_userVerification.IsLocalLicenseValid())
        {
            programIsValidToRun = false;
        }
        if (!programIsValidToRun)
        {
		    var result = _userVerification.ShowDialog("Please enter license key ","License");
            programIsValidToRun = result != null;
        }

        if (programIsValidToRun)
        {
		    _log.LogInformation("Agent Start");
		    _proxyServerService.RunProxy();
        }
	}
}
