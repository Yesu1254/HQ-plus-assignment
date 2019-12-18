using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiCore.Managers
{
    public class ConvertJsonToList
    {
        public static List<ExpandoObject> GetExpandoObject(JArray jArray)
        {
            try
            {
                var hotelRates = new List<ExpandoObject>();
                foreach (JObject content in jArray.Children<JObject>())
                {
                    var x = new ExpandoObject() as IDictionary<string, Object>;
                    foreach (JProperty prop in content.Properties())
                    {

                        if (prop.Value.Type == JTokenType.Object)
                        {
                            var childObject = (JObject)prop.Value;
                            foreach (JProperty cprop in childObject.Properties())
                            {
                                x.Add(cprop.Name, cprop.Value);
                            }
                            Console.WriteLine(prop.Name);
                        }
                        if (prop.Value.Type == JTokenType.Array)
                        {
                            var childArray = (JArray)prop.Value;
                            foreach (var child in childArray)
                            {
                                var cprop = (JObject)child;
                                foreach (JProperty c in cprop.Properties())
                                {
                                    if (c.Value.Type == JTokenType.Boolean)
                                    {
                                        var val = (bool)c.Value == true ? "1" : "0";
                                        x.Add(c.Name, val);
                                    }
                                    else
                                    {
                                        x.Add(c.Name, c.Value);
                                    }
                                }
                            }

                        }
                        if (prop.Value.Type != JTokenType.Array && prop.Value.Type != JTokenType.Object)
                        {
                            x.Add(prop.Name, prop.Value);
                        }
                    }
                    x.TryGetValue("targetDay", out object value);
                    x.Add("ARRIVAL_DATE", Convert.ToDateTime(value).AddDays(-1));
                    hotelRates.Add(x as ExpandoObject);

                }
                return hotelRates;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }
    }
}
