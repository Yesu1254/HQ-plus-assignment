using ApiCore.CoreModels;
using ApiCore.Managers;
using Microsoft.Office.Interop.Excel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Drawing;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net.Mail;

namespace ApiCore.Commands
{

    public class GetHotelRatesReport : CommandBase
    {
        public CommandResponse Run(RouteRequest request, bool isCallFromAutomation = false)
        {
            var currentDir = Environment.CurrentDirectory;
            var jsonStr = (request.contentStr != null || request.contentStr != "") ? System.IO.File.ReadAllText(currentDir +$"/task 2 - hotelrates.json") : request.contentStr;
            var jsonObj = JObject.Parse(jsonStr);
            var values = (JArray)jsonObj["hotelRates"];
            var fileName = "HotelRates.xlsx";
            var path = currentDir + $"/Imports/";

            //Creae an Excel application instance
            Application excelApp = new Application();
            excelApp.DisplayAlerts = false;

            if (!Directory.Exists(currentDir + $"/Imports/"))
            {
                System.IO.Directory.CreateDirectory(currentDir + $"/Imports/");
            }

            //create a file if does not exist
            if (!File.Exists(fileName))
            {
                var wb = excelApp.Workbooks.Add();
                wb.SaveAs(path + fileName);
                wb.Close(0);
            }

            //Create an Excel workbook instance and open it from the predefined location
            Workbook excelWorkBook = excelApp.Workbooks.Open(path + fileName);
            Worksheet excelWorkSheet = excelWorkBook.Sheets.Add();

            try
            {
                if (!Directory.Exists(path + "Imported"))
                {
                    System.IO.Directory.CreateDirectory(path + "Imported");
                }
                if (File.Exists(path + fileName))
                {
                    var newPath = Path.Combine(path + fileName, path + "Imported");
                    var processedFileName = Path.Combine(path + "Imported", $"_Imported{fileName}{Path.GetExtension(path)}");
                    if (File.Exists(processedFileName))
                    {
                        File.Delete(processedFileName);
                    }
                }

                var hotelRates = new List<ExpandoObject>();
                foreach (JObject content in values.Children<JObject>())
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
                            if (prop.Value.Type == JTokenType.Date)
                            {
                                x.Add(prop.Name, Convert.ToDateTime(prop.Value).ToString("dd'.'MM'.'yyyy", CultureInfo.InvariantCulture));
                            }
                            else x.Add(prop.Name, prop.Value);
                        }
                    }
                    x.TryGetValue("targetDay", out object value);
                    var testt = value;
                    x.Add("ARRIVAL_DATE", testt);
                    hotelRates.Add(x as ExpandoObject);

                }

                var dt = ListConvertorManager.ToDataTable<dynamic>(hotelRates);

                dt.Columns.Remove("numericFloat");
                dt.Columns.Remove("rateId");
                dt.Columns.Remove("rateDescription");
                dt.Columns.Remove("name");
                dt.Columns.Remove("los");

                dt.Columns["targetDay"].SetOrdinal(0);
                dt.Columns["ARRIVAL_DATE"].SetOrdinal(1);
                dt.Columns["numericInteger"].SetOrdinal(2);
                dt.Columns["currency"].SetOrdinal(3);
                dt.Columns["ratename"].SetOrdinal(4);
                dt.Columns["adults"].SetOrdinal(5);
                dt.Columns["shape"].SetOrdinal(6);

                dt.Columns["targetDay"].ColumnName = "DEPARTURE_DATE";
                dt.Columns["numericInteger"].ColumnName = "PRICE";
                dt.Columns["currency"].ColumnName.ToUpper();
                dt.Columns["ratename"].ColumnName.ToUpper();
                dt.Columns["adults"].ColumnName.ToUpper();
                dt.Columns["shape"].ColumnName = "BREAKFAST_INCLUDED";

                Range SourceRange = (Range)excelWorkSheet.get_Range("A1:G" + dt.Rows.Count + 1); // or whatever range you want here
                FormatAsTable(SourceRange, "Table1", "TableStyleLight20");


                for (int i = 1; i < dt.Columns.Count + 1; i++)
                {
                    excelWorkSheet.Cells[1, i] = dt.Columns[i - 1].ColumnName.ToUpper();
                    excelWorkSheet.Cells[1, i].Font.Color = Color.Blue;
                }

                for (int j = 0; j < dt.Rows.Count; j++)
                {
                    for (int k = 0; k < dt.Columns.Count; k++)
                    {
                        excelWorkSheet.Cells[j + 2, k + 1] = dt.Rows[j].ItemArray[k].ToString();
                    }
                }

                if (isCallFromAutomation)
                {
                    MemoryStream obj_stream = new MemoryStream();
                    var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid() + ".xls");
                    excelWorkBook.SaveAs(tempFile);
                    excelWorkBook.Close(0);
                    excelApp.Quit();
                    obj_stream = new MemoryStream(File.ReadAllBytes(tempFile));
                    var attachment = new Attachment(obj_stream, DateTime.Now.ToString("dd-MMM-yyyy") + fileName);
                    UnityManager.GetCommand<SendEmail>().Run(request, "smtp.gmail.com", 465, "", "", "", "", "HotelRatesReport", "", "Please see attached", new List<Attachment>() { attachment }, false);
                }
                else
                {
                    excelWorkBook.Save();
                    excelWorkBook.Close(0);
                    excelApp.Quit();
                }

                var response = new CommandResponse();
                response.Headers.Add("location", "report saved at " + path + fileName);

                // var cr = new CommandResponse("File Saved at " + path + fileName, "application/json");
                return response;

                //return true;
            }
            catch (Exception e)
            {
                excelWorkBook.Close(0);
                throw new Exception(e.Message);
            }
        }

        public void FormatAsTable(Microsoft.Office.Interop.Excel.Range SourceRange, string TableName, string TableStyleName)
        {
            SourceRange.Worksheet.ListObjects.Add(XlListObjectSourceType.xlSrcRange,
            SourceRange, System.Type.Missing, XlYesNoGuess.xlYes, System.Type.Missing).Name =
                TableName;
            SourceRange.Select();
            SourceRange.Worksheet.ListObjects[TableName].TableStyle = TableStyleName;
        }
    }
}