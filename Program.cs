using UrlTcpListenerLibrary.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Url_Detection_Agent;

using IHost host = CreateHostBuilder(args).Build();
using var scope = host.Services.CreateScope();

var services = scope.ServiceProvider;

// general run of the app
try
{
    services.GetRequiredService<AgentApp>().Run(args);
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
	throw;
}

static IHostBuilder CreateHostBuilder(string[] args)
{
    return Host.CreateDefaultBuilder(args)
        .ConfigureServices((_, services) =>
        {
            services.AddSingleton<ITcpService, TcpService>();
            services.AddSingleton<AgentApp>();
            services.AddSingleton<IProxyServerService,ProxyServerService>();
        });
}