using ApiCore.CoreModels;
using ApiCore.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using HtmlAgilityPack;

namespace ApiCore.Commands
{
    public class ReadHtmlContent : CommandBase
    {
        private const string InputFileName = "task 1 - Kempinski Hotel Bristol Berlin, Germany - Booking.com.html";
        private const string Charset = "windows-1251";

        public bool Run(RouteRequest routeRequest)
        {
            HtmlDocument document = new HtmlDocument();
            var htmlContent = System.IO.File.ReadAllText(InputFileName);
            document.LoadHtml(htmlContent);
            foreach (HtmlNode link in document.DocumentNode.SelectNodes("//meta[@content]"))
            {
                HtmlAttribute attribute = link.Attributes["content"];
                if (attribute.Value.Contains("WordPress"))
                {
                    var contentValue = attribute.Value.Replace("WordPress", "").Trim();
                }
            }
            return true;
        }
    }
}
