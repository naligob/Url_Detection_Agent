using Microsoft.Extensions.Logging;
using UrlTcpListenerLibrary.Interfaces;

namespace Url_Detection_Agent;

public class AgentApp
{
	private readonly IProxyServerService _proxyServerService;
	private readonly ILogger<AgentApp> _log;

	public AgentApp(ILogger<AgentApp> log,IProxyServerService proxyServerService)
	{
		_log = log;
		_proxyServerService = proxyServerService;
	}


	public void Run(string[] args)
	{
		_log.LogInformation("Agent Start");
		_proxyServerService.RunProxy();
	}
}
