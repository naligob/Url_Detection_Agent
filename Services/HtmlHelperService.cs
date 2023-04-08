using System.Text;
using Url_Detection_Agent.Models;

namespace Url_Detection_Agent.Services
{
    public class HtmlHelperService
    {
        public HtmlHelperService()
        {

        }
        public string GetHtmlSafePageContent(HtmlModelContant input)
        {
            StringBuilder sb = new StringBuilder();
            #region html & JS
            sb.Append("<!DOCTYPE html><html lang=\"en\"><head>");
            sb.AppendLine("<script>function Redirect(){window.location.href = document.getElementById(\"targetUrl\").getAttribute(\"herf\")}</script>");
            sb.Append("<meta charset=\"UTF-8\"/><title>URL Detection</title></head>" +
                "<body>" +
                "<section class=\"img-back\">" +
                "<h1>URL Detection</h1>" +
                "<p class=\"nav-paragraph\">if you still want to continue to the site click <a onclick=\"Redirect()\" herf = \"");
            sb.Append($"{input.Url}\" ");
            sb.Append("id=\"targetUrl\" target=\"_blank\">here</a></p>");
            if (input.ReasonsList != null && input.ReasonsList.Any())
            {
                sb.Append("<p> top reason to block this URL </p>" +
                    "<div class=\"warrper\">");
                foreach (var reasonData in input.ReasonsList.Select((val, index) => new { Reason = val, Index = index }))
                {
                    sb.Append("<div class=\"info-box\"> " +
                        $"<h5>number {reasonData.Index}</h5>");
                    sb.Append($"<p>{reasonData.Reason}</p></div>");
                    if (reasonData.Index / 3 != 0) 
                    {
                        sb.Append("<div class=\"vline\"></div>");
                    }
                }
                sb.Append("</div>");
            }
            sb.Append("</section></body></html>");
            #endregion
            #region css
            sb.AppendLine("<style>.info-box{display: inline-block;vertical-align:top;width: 30%;}.vline{background: rgb(186,177,152);display: inline-block;vertical-align:top;min-height: 150px;margin: 0;width: 1px;}.warrper{text-align:center;width: 80%;margin: 0 auto;text-align: center;}body {margin: 20px;font-family: cursive;text-align: center;}h1 {margin-top: 50px;text-align: center;width: 569px;margin: 60px auto 0;padding-top: 100px;font-size: 3rem;}.nav-paragraph{margin-top: 30px;padding-top: 20px;padding-bottom: 40px;}a {border-style: none none solid;padding: 0;border-width: 1px;cursor: pointer;}.img-back {background-color: #cef9f7;width: 100%;height: 640px;}</style>");
            #endregion
            return sb.ToString();
        }

    }
}
