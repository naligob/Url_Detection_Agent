using Titanium.Web.Proxy.EventArguments;

namespace Url_Detection_Agent.Interfaces;

public interface IProxyServerService
{
    Task OnCertificateSelection(object sender, CertificateSelectionEventArgs e);
    Task OnCertificateValidation(object sender, CertificateValidationEventArgs e);
    Task OnRequest(object sender, SessionEventArgs e);
    Task OnResponse(object sender, SessionEventArgs e);
    void RunProxy();
}
