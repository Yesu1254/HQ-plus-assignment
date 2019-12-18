using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace ApiCore.Managers
{
    public class RequestLog
    {
        public DateTime Date;
        public string Path;
        public string QueryString;
        //public HttpContext Context;
        public byte[] Body;
    }

    public class RequestPath
    {
        public int MaxCount = 15;
        //public int Count;
        public int Executions;
        //public string Url;

        public string RequestLog;
        public ConcurrentDictionary<string, RequestLog> requests = new ConcurrentDictionary<string, RequestLog>();

    }

    public class RequestCount
    {
        public int MaxCount = 15;
        public int Count;
    }

    public static class ActiveRequestMiddleware
    {
        private static ConcurrentDictionary<string, RequestPath> requestPaths = new ConcurrentDictionary<string, RequestPath>();

        private static DateTime? excessiveLoadStartDate;
        private static bool excessiveLoadNotificationSent;
        private static DateTime startTime;

        //private static int CurrentRequestCount;

        static ActiveRequestMiddleware()
        {
            GC.KeepAlive(requestPaths);
            GC.SuppressFinalize(requestPaths);
            startTime = DateTime.Now;
            //PollForGCEvent();

            //Allocate huge memory to apply pressure on GC
            //AllocateMemory();

        }

        public static List<char[]> lst = new List<char[]>();
        private static void AllocateMemory()
        {
            while (true)
            {

                char[] bbb = new char[1000]; // creates a block of 1000 characters
                lst.Add(bbb);                // Adding to list ensures that the object doesnt gets out of scope   
                int counter = GC.CollectionCount(2);
                Console.WriteLine("GC Collected {0} objects", counter);

            }
        }
        public static async Task<HttpContext> ActionHandler(HttpContext context, Func<Task> next)
        {
            var requestId = Guid.NewGuid().ToString();
            var queryString = context.Request.QueryString.Value;
            var path = context.Request.Path.Value.ToLower();
            const string maxError = "{\"ex\":\"Maximum requests reached for this command\"}";

            var currentRequestCount = requestPaths.Values.Sum(x => x.requests.Count);

            if (path == "/requests")
            {
                await context.Response.WriteAsync(GetRequestsHtml(false, currentRequestCount));
                return context;
            }

            if (path == "/detailedrequests")
            {
                await context.Response.WriteAsync(GetRequestsHtml(true, currentRequestCount));
                return context;
            }

            //if currently under load and we havent started the clock, then start it
            if (currentRequestCount > 50 && excessiveLoadStartDate == null)
            {
                excessiveLoadStartDate = DateTime.Now;
            }
            //if currently under load and the clock is ticking, then wait for 30 seconds before sending the notification
            else if (currentRequestCount > 50 && excessiveLoadStartDate != null)
            {
                //check if we have already sent the notification
                if (!excessiveLoadNotificationSent)
                {
                    //send notification it has been slow for 10 seconds
                    if ((DateTime.Now - excessiveLoadStartDate.Value).TotalSeconds > 60)
                    {
                        //SendNotification("System Under Load", currentRequestCount);
                    }
                }
            }
            //not under load then reset the clock
            else if (currentRequestCount <= 50)
            {
                //no load currently
                excessiveLoadStartDate = null;
                excessiveLoadNotificationSent = false;
            }

            try
            {
                requestPaths.TryAdd(path, new RequestPath());
            }
            catch (Exception)
            {
                throw;
            }
            requestPaths.TryGetValue(path, out RequestPath requestPath);

            var pathRequestCount = requestPath.requests.Count;

            //check if max requests per url have been reached
            if (pathRequestCount + 1 > requestPath.MaxCount)
            {
                //return error
                context.Response.StatusCode = 503; //capacity
                await context.Response.WriteAsync(maxError);
                return context;
            }

            //requestPath.Count++;
            requestPath.Executions++;

            try
            {
                //replace Body stream with memory stream so that it can be accessed
                var stream = context.Request.Body;
                byte[] body;
                var buffer = new MemoryStream();

                // Copy the request stream to the memory stream.
                await stream.CopyToAsync(buffer);

                // Rewind the memory stream.
                buffer.Position = 0L;

                // Replace the request stream by the memory stream.

                body = buffer.ToArray();

                buffer.Position = 0;

                context.Request.Body = buffer;

                stream.Dispose();

                requestPath.requests.TryAdd(requestId, new RequestLog() { Date = DateTime.Now, Path = path, QueryString = queryString, Body = body });

            }
            catch (Exception)
            {

                throw;
            }

            try
            {
                await next();
            }
            catch (Exception ex)
            {
                //var errorJson = $"{{\"ex\":\"{ex.Message.Replace("\"","\\\"")}\"}}";
                //await context.Response.WriteAsync(errorJson);
                context.Abort();
                //throw;
            }
            finally
            {
                //requestPath.Count--;
                requestPath.requests.TryRemove(requestId, out RequestLog val);
            }

            //requestPath.Count--;

            //try
            //{
            //    requestPath.requests.TryRemove(requestId, out RequestLog val);
            //}
            //catch (Exception)
            //{

            //    throw;
            //}

            return context;

        }

        private static string GetRequestsHtml(bool fullDetails, int currentRequestCount)
        {
            var currentRequests = requestPaths.Values.SelectMany(x => x.requests).OrderBy(v => v.Value.Date).ToList();

            var sb = new StringBuilder();

            var process = Process.GetCurrentProcess();
            PerformanceCounter ramCounter = new PerformanceCounter("Process", "Working Set", process.ProcessName);
            PerformanceCounter cpuCounter = new PerformanceCounter("Process", "% Processor Time", process.ProcessName);

            //stats
            sb.AppendLine("<b>Start Time:</b> " + startTime.ToString() + "<br/>");
            sb.AppendLine("<b>Active Requests:</b> " + currentRequestCount + "<br/>");
            sb.AppendLine("<b>Memory Consumption:</b> " + ((GC.GetTotalMemory(false) / 1024) / 1024) + "MB<br/>");
            sb.AppendLine("<b>RAM:</b> " + Math.Round((ramCounter.NextValue() / 1024 / 1024), 2) + " MB<br/>");
            sb.AppendLine("<b>CPU:</b> " + (cpuCounter.NextValue()) + " %<br/>");
            sb.AppendLine("<b>GC Latency Mode:</b> " + GCSettings.LatencyMode.ToString() + "<br/>");
            sb.AppendLine("<b>GC Mode:</b> " + (GCSettings.IsServerGC ? "server" : "workstation") + "<br/>");
            sb.AppendLine("<br/>");
            sb.AppendLine("<br/>");
            sb.AppendLine("<b>Grouped Requests</b><br/>");

            //grouped data
            var groupedCurrentRequests = requestPaths.OrderByDescending(x => x.Value.requests.Count);
            var index = 0;
            foreach (var requestGroup in groupedCurrentRequests)
            {
                index++;
                var request = requestGroup.Value.requests.Values.OrderBy(x => x.Date).FirstOrDefault();
                if (request == null) continue;
                var runningSec = (DateTime.Now - request.Date).TotalSeconds;

                sb.AppendLine($"<b>{requestGroup.Key}</b>");
                sb.AppendLine($"Count: {requestGroup.Value.requests.Count()}, Max: {requestGroup.Value.MaxCount}, Total: {requestGroup.Value.Executions}, Longest: {Math.Round(runningSec)}s");
                sb.AppendLine("<br/>");
            }

            sb.AppendLine("<br/>");
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("<b>Individual Requests</b><br/>");

            //individual
            index = 0;
            foreach (var request in currentRequests)
            {
                index++;
                var runningSec = (DateTime.Now - request.Value.Date).TotalSeconds;

                if (fullDetails == false)
                {
                    sb.AppendLine($"{index}, {Math.Round(runningSec)}s, {request.Value.Path}, {request.Value.QueryString}<br/>");
                }
                else
                {
                    var body = Encoding.UTF8.GetString(request.Value.Body);

                    sb.AppendLine($"<b>{index}, {request.Value.Date.ToShortTimeString()}, {Math.Round(runningSec)}s, {request.Value.Path}, {request.Value.QueryString}</b><br/>");
                    sb.AppendLine($"<p style=\"color: #006699\">{body}</p><br/>");
                }
            }

            return sb.ToString();
        }
    }
}
