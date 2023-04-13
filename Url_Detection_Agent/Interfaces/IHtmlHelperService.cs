using Url_Detection_Agent.Models;

namespace Url_Detection_Agent.Services
{
    public interface IHtmlHelperService
    {
        string GetHtmlSafePageContent(HtmlModelContant input);
    }
}