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
using System.Xml.XPath;
using Newtonsoft.Json;
using System.Web.Script.Serialization;

namespace ApiCore.Commands
{
    public class NewHotel
    {
        public string name { get; set; }
        public string address { get; set; }
        public string clasification { get; set; }
        public string reviewPoints { get; set; }
        public string numberOfReviews { get; set; }
        public string description { get; set; }
    }

    public class Categories
    {
        public string max { get; set; }
        public string roomType { get; set; }
        public string price { get; set; }
    }
    public class Alternates
    {
        public string info { get; set; }
    }

    //it will read the data from html file and convert that as object or list
    public class ReadHtmlContent : CommandBase
    {
        private const string InputFileName = "task 1 - Kempinski Hotel Bristol Berlin, Germany - Booking.com.html";

        public CommandResponse Run(RouteRequest routeRequest)
        {
            HtmlDocument document = new HtmlDocument();
            var htmlContent = System.IO.File.ReadAllText(InputFileName);
            document.LoadHtml(htmlContent);

            var newHotel = new NewHotel();
            var categoriesLst = new List<Categories>();
            var alternateList = new List<Alternates>();
            var testt = document.DocumentNode.SelectNodes(".//div[@id='hotelTmpl']");
            foreach (HtmlNode node in document.DocumentNode.SelectNodes(".//div[@id='hotelTmpl']"))
            {
                newHotel.reviewPoints = node.SelectSingleNode(".//span[@class='average js--hp-scorecard-scoreval']").InnerText.Trim();
                newHotel.numberOfReviews = node.SelectSingleNode(".//span[@class='trackit score_from_number_of_reviews']").InnerText.Trim();
                newHotel.description = node.SelectSingleNode(".//div[@id='summary']").InnerText.Replace("\n\n\n\n", ":").Split(':')[2];
                newHotel.name = node.SelectSingleNode(".//span[@id='hp_hotel_name']").InnerText.Trim();
                newHotel.address = node.SelectSingleNode(".//span[@id='hp_address_subtitle']").InnerText.Trim();

                var alternateResults = node.SelectSingleNode(".//table[@id='althotelsTable']").Descendants("tr").Where(tr => tr.Elements("td").Count() > 1)
                                  .Select(tr => tr.Elements("td").Select(td => td.InnerText.Trim()).ToList()).ToList();

                var categoriesResult = node.SelectSingleNode(".//table[@id='maxotel_rooms']").Descendants("tr").Where(tr => tr.Elements("td").Count() > 1)
                                  .Select(tr => tr.Elements("td").Select(td => td.InnerText.Trim()).ToList()).ToList();

                var gtins = node.SelectSingleNode(".//table[@id='maxotel_rooms']/tbody/tr[1]");
                //.Select(td => td.InnerText.Replace("GTIN:", ""));

                //var span2 = gtins.SelectSingleNode("/html[1]/body[1]/div[1]/div[1]/div[4]/div[1]/div[1]/div[3]/div[2]/div[1]/div[8]/table[1]/tbody[1]/tr[2]/td[1]").InnerText;
                var firstColumn = node.SelectSingleNode(".//table[@id='maxotel_rooms']").Descendants("tr").Skip(1).Where(tr => tr.Elements("td").Count() > 1).ToList();

                //.Select(tr => tr.Elements("span").Select(td => td.InnerText.Trim()).ToList())).ToList();

                var numberOfPopleCanStayLst = new List<string>();
                foreach (HtmlNode row in firstColumn)
                {
                    var index = 0;
                    var numberOfPopleCanStay = "";
                    foreach (var td in row.SelectNodes("td"))
                    {
                        var span = td.SelectNodes("span");
                        var div = td.SelectNodes("div");
                        if (index == 0)
                        {
                            if (span != null || div != null)
                            {
                                var tag = span != null ? "span" : div != null ? "div" : "span";
                                foreach (HtmlNode cell in td.SelectNodes(tag))
                                {
                                    if (cell.InnerHtml.Contains("div") || cell.InnerHtml.Contains("span"))
                                    {
                                        var tag2 = cell.InnerHtml.Contains("div") ? "div" : cell.InnerHtml.Contains("span") ? "span" : "";
                                        if (tag2 != "")
                                        {
                                            var divExists = cell.SelectNodes(tag2);
                                            if (divExists != null)
                                            {
                                                numberOfPopleCanStay = cell.InnerText.Trim() + cell.Attributes[1].Value.Trim();
                                                //resulttypes[0].Add(text);
                                                if (cell.InnerHtml.Contains(tag2))
                                                {

                                                }
                                            }
                                        }
                                        else numberOfPopleCanStay = cell.InnerText.Trim();
                                    }
                                }
                            }
                            else
                            {
                                if (index == 0)
                                {
                                    //resulttypes[1].Add(text);
                                    var test = td.SelectNodes("i").ToList();
                                    numberOfPopleCanStay = test[0].Attributes[1].Value.Trim();
                                }
                            }

                        }
                        if(index == 0) numberOfPopleCanStayLst.Add(numberOfPopleCanStay);
                        index++;
                    }
                }

                var i = 0;
                foreach (var items in categoriesResult)
                {
                    var categories = new Categories();
                    categories.max = numberOfPopleCanStayLst[i].ToString();
                    categories.roomType = items[1].ToString();
                    categories.price = items[2].ToString();
                    categoriesLst.Add(categories);
                    i++;
                }
                foreach (var items in alternateResults)
                {
                    foreach (var item in items)
                    {
                        var alternates = new Alternates();
                        alternates.info = item;
                        alternateList.Add(alternates);
                    }

                }
            }

            var hotelJson = new JavaScriptSerializer().Serialize(newHotel);
            var categoriesLstJson = new JavaScriptSerializer().Serialize(categoriesLst);
            var alternateHotelsJson = new JavaScriptSerializer().Serialize(alternateList);
            var json = JsonConvert.SerializeObject(new { hotel = hotelJson, categories = categoriesLstJson, alternateHotels = alternateHotelsJson });
            var cr = new CommandResponse(json, "application/json");
            return cr;

        }
    }
}
