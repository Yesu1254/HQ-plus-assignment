using ApiCore.CoreModels;
using ApiCore.Managers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace ApiCore.Commands
{
    public class Root
    {
        public Hotel hotel { get; set; }
    }

    public class HotelRate
    {
        public int adults { get; set; }
        public int los { get; set; }
        public Price price { get; set; }
        public string rateDescription { get; set; }
        public int rateID { get; set; }
        public string rateName { get; set; }
        public List<RateTag> rateTags { get; set; }
        public DateTime targetDay { get; set; }
    }

    public class Price
    {
        public string currency { get; set; }
        public decimal numericFloat { get; set; }
        public int numericInteger { get; set; }
    }

    public class RateTag
    {
        public string name { get; set; }
        public bool shape { get; set; }
    }

    public class Hotel
    {
        public int hotelID { get; set; }
        public int classification { get; set; }
        public string name { get; set; }
        public double reviewscore { get; set; }
        public List<HotelRate> hotelRates { get; set; }
    }

    //it will take hotleId and date as a parameters and filter the json file
    public class GetHotelRatesByIdAndDate : CommandBase
    {
        public CommandResponse Run(RouteRequest request, int hotelId, string date)
        {
            try
            {
                var currentDir = Environment.CurrentDirectory;
                var path = currentDir + $"/task 2 - hotelrates.json";
                var jsonStr = (request.contentStr != null || request.contentStr != "") ? System.IO.File.ReadAllText(path) : request.contentStr;
                var sr = JObject.Parse(jsonStr);
                var root = JsonConvert.DeserializeObject<Root>(jsonStr);
                var hoteRates = new List<HotelRate>();
                if (root.hotel.hotelID == hotelId)
                {
                    var rates = JsonConvert.DeserializeObject<Hotel>(jsonStr);
                    hoteRates = rates.hotelRates.Where(a => a.targetDay.ToString("MM/dd/yyyy") == Convert.ToDateTime(date).ToString("MM/dd/yyyy")).ToList();
                }

                var rootJson = new JavaScriptSerializer().Serialize(root);
                var hoteRatesJson = new JavaScriptSerializer().Serialize(hoteRates);
                var json = JsonConvert.SerializeObject(new { hotel = rootJson, hoteRatesJson });
                var cr = new CommandResponse(json, "application/json");
                return cr;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }
    }
}