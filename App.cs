using Microsoft.Extensions.Logging;
using UrlTcpListenerLibrary.Services;

namespace Url_Detection_Agent;

public class AgentApp
{
	private readonly ITcpService _tcpService;
	private readonly IProxyServerService _proxyServerService;
	private readonly ILogger<AgentApp> _log;

	public AgentApp(ITcpService tcpService,ILogger<AgentApp> log,IProxyServerService proxyServerService)
	{
		_tcpService = tcpService;
		_log = log;
		_proxyServerService = proxyServerService;
	}


	public void Run(string[] args)
	{
		_log.LogInformation("Agent Start");
		//_tcpService.RunTcpListeners();
		_proxyServerService.RunProxy();
		
	}
}
