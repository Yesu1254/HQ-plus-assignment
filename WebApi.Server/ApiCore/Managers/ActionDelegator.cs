using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using System.Globalization;
using System.Threading;
using ApiCore.SignalR;
using System.IO;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using ApiCore.CoreModels;

namespace ApiCore.Managers
{
    public class ActiveAction
    {
        public string guid { get; set; }
        public DateTime startDate { get; set; }
        public string pathAndQuery { get; set; }
        public int? userId { get; set; }

        private DateTime lastEvent = DateTime.Now;
        public List<string> events { get; set; } = new List<string>();

        public void AddEvent(string message)
        {
            events.Add($"{System.Math.Round((DateTime.Now - lastEvent).TotalMilliseconds)}ms -> {message}");
            lastEvent = DateTime.Now;
        }
    }

    public class ActionRoute
    {
        public string replacement { get; set; }
        public bool anonymous { get; set; }
    }

    public class ActionDelegator
    {
        private const string Origin = "Origin";
        private const string OptionsMethod = "Options";
        private const string AccessControlAllowMethods = "Access-Control-Allow-Methods";
        private const string AccessControlAllowHeaders = "Access-Control-Allow-Headers";
        private const string AccessControlAllowCredentials = "Access-Control-Allow-Credentials";
        private const string AccessControlRequestMethod = "Access-Control-Request-Method";
        private const string AccessControlRequestHeaders = "Access-Control-Request-Headers";
        private const string AccessControlAllowOrigin = "Access-Control-Allow-Origin";

        private static Dictionary<string, ActionRoute> routes = new Dictionary<string, ActionRoute>();

        public static double TotalBytesSent = 0;
        public static double TotalWriteMs = 0;
        public static long TotalRequests = 0;

        [EnableCors(policyName: "CorsPolicy")]
        public static async Task<HttpContext> ActionHandler(HttpContext context, Func<Task> next)
        {
            CultureInfo newCulture = (CultureInfo)Thread.CurrentThread.CurrentCulture.Clone();
            newCulture.DateTimeFormat.ShortDatePattern = "dd-MMM-yyyy";
            newCulture.NumberFormat.CurrencyDecimalSeparator = ".";
            newCulture.NumberFormat.NumberDecimalSeparator = ".";
            newCulture.DateTimeFormat.LongDatePattern = "dd-MMM-yyyy";
            newCulture.DateTimeFormat.DateSeparator = "-";
            Thread.CurrentThread.CurrentCulture = newCulture;
            Thread.CurrentThread.CurrentUICulture = newCulture;

            var origin = context.Request.Headers.ContainsKey(Origin) ? context.Request.Headers[Origin].First() : "";

            //var origin = context.Request.Headers.ContainsKey(Origin) ? context.Request.Headers[Origin] : "";

            var request = context.Request;
            var response = context.Response;
            var path = request.Path.Value.ToLower();
            var queryString = request.QueryString.Value;
            var pathAndQuery = path + queryString;


            var activeAction = new ActiveAction()
            {
                startDate = DateTime.Now,
                pathAndQuery = pathAndQuery,
                guid = Guid.NewGuid().ToString()
            };
            // HangPreventorManager.AddAction(activeAction);

            response.Headers.Append("Cache-Control", "No-Cache");

            //System.Threading.Thread.Sleep(1000);

            var requestContentStr = "";

            using (var reader = new StreamReader(request.Body))
            {
                requestContentStr = await reader.ReadToEndAsync();
            }

            activeAction.AddEvent("Request body read");



            try
            {

                #region Cors Support

                if (request.Headers != null && 1 == 2)
                {
                    var isCorsRequest = request.Headers.ContainsKey(Origin);
                    var isPreflightRequest = String.Equals(request.Method, OptionsMethod, StringComparison.InvariantCultureIgnoreCase);
                    if (isCorsRequest)
                    {
                        if (isPreflightRequest)
                        {

                            response.StatusCode = 200;

                            if (!String.IsNullOrEmpty(origin) && !response.Headers.ContainsKey(AccessControlAllowOrigin))
                            {
                                response.Headers.Append(AccessControlAllowOrigin, origin);
                            };

                            //var accessControlRequestMethod = request.Headers.ContainsKey(AccessControlRequestMethod) ? request.Headers[AccessControlRequestMethod] : ""; //request.Headers[AccessControlRequestMethod].FirstOrDefault();

                            var accessControlRequestMethod = request.Headers.ContainsKey(AccessControlRequestMethod) ? request.Headers[AccessControlRequestMethod].FirstOrDefault() : "";
                            if (accessControlRequestMethod != null)
                            {
                                response.Headers.Append(AccessControlAllowMethods, accessControlRequestMethod);
                            }

                            var requestedHeaders = string.Join(", ", request.Headers[AccessControlRequestHeaders]);
                            if (!string.IsNullOrEmpty(requestedHeaders))
                            {
                                response.Headers.Append(AccessControlAllowHeaders, requestedHeaders);
                            }
                            response.Headers.Append(AccessControlAllowCredentials, "true");

                            return context;
                        }

                        if (!String.IsNullOrEmpty(origin) && !response.Headers.ContainsKey(AccessControlAllowOrigin)) response.Headers.Append(AccessControlAllowOrigin, origin);
                        response.Headers.Append(AccessControlAllowHeaders, "*");
                        response.Headers.Append(AccessControlAllowCredentials, "true");

                    }

                }

                #endregion

                var primaryAccept = "*/*";

                if (request.Headers.ContainsKey("Accept"))
                {
                    //var acceptedHeaders = request.Headers.ContainsKey("Accept") ? new List<string>().ToArray() : request.Headers["Accept"].ToList(.Split(',');
                    var acceptedHeaders = request.Headers["Accept"].First().Split(','); //request.Headers.Accept.ToList().Select(a => a.ToString());

                    primaryAccept = acceptedHeaders.FirstOrDefault();

                    foreach (var accept in acceptedHeaders)
                    {
                        if (accept.ToLower().Contains("json"))
                        {
                            primaryAccept = accept;
                            break;
                        }
                    }
                }

                activeAction.AddEvent("Request body read");

                pathAndQuery = WebUtility.UrlDecode(pathAndQuery);

                //var isMvcRequest = pathAndQuery == "/" || pathAndQuery.StartsWith("/ui/") || pathAndQuery.StartsWith("ui/");


                //var requestContentStr = StreamToString(request.Body);

                try
                {
                    var routeRestult = await RouteRequest(response, pathAndQuery, request.Method, requestContentStr, primaryAccept, null, null, request, activeAction);

                    activeAction.AddEvent("Route Request Success End");

                    if (routeRestult == false)
                    {
                        await next();

                        activeAction.AddEvent("Successful");
                        return context;
                    }
                }
                catch (Exception routeException)
                {

                    activeAction.AddEvent("Fail: " + routeException.Message);

                    response.StatusCode = 200;

                    if (!String.IsNullOrEmpty(origin)) response.Headers.Append(AccessControlAllowOrigin, origin);

                    response.Headers.Append("Content-Type", "application/json");

                    await response.WriteAsync($"{{\"ex\":\"Route Exception : {routeException.Message} \"}}");

                    return context;

                }

                return context;

            }
            catch (Exception ex)
            {
                var errorBody = requestContentStr;

                context.Response.StatusCode = 200;

                if (!String.IsNullOrEmpty(origin)) context.Response.Headers.Append(AccessControlAllowOrigin, origin);

                //response.Headers.Append("Content-Type", "application/json");

                await context.Response.WriteAsync($"{{\"ex\":\"ActionDelegator Exception: {ex.Message} \"}}" + ": Body: " + errorBody);
                return context;
            }


        }

        public static async Task<bool> RouteRequest(HttpResponse httpResponse, string requestPath, string requestMethod, string requestContentStr, string accepts, string user, string token, HttpRequest request = null, ActiveAction action = null)
        {
            if (accepts == null) accepts = "*/*";

            if (requestPath.StartsWith("/")) requestPath = requestPath.Substring(1);

            var path = requestPath;
            var query = "";

            if (path.Contains("?"))
            {
                path = requestPath.Substring(0, requestPath.IndexOf("?"));
                query = requestPath.Substring(requestPath.IndexOf("?"));
            };

            //check for any pre-defined routes
            ActionRoute route = null;
            if (routes.ContainsKey(path))
            {
                route = routes[path];
                requestPath = route.replacement + query;
            }
            else
            {
                //default to files if no function path
                if (!requestPath.StartsWith("s/") && !requestPath.StartsWith("q/") && !requestPath.StartsWith("ui/") && !requestPath.StartsWith("f/") && !requestPath.StartsWith("c/"))
                {
                    if (requestPath.Contains("."))
                    {
                        requestPath = "f/" + requestPath;
                    }
                    else
                    {
                        requestPath = "f/index.html";
                    }

                }
            }

            var isFileRequest = requestPath.StartsWith("f/");

            var function = requestPath.Substring(0, requestPath.IndexOf("/"));
            var serviceName = requestPath.Substring(function.Length + 1);
            var parameters = "";

            if (serviceName.Contains("?"))
            {
                var questionIndex = serviceName.IndexOf("?");
                parameters = serviceName.Substring(questionIndex + 1);
                serviceName = serviceName.Substring(0, questionIndex);
            }

            if (isFileRequest == false && user == null && route?.anonymous == false)
            {
                var reqPath = request.Path.ToUriComponent();
                if (reqPath.Contains("?")) reqPath = reqPath.Substring(0, reqPath.IndexOf("?"));

                var redirectResponse = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    StatusCode = HttpStatusCode.Moved
                };

                var redirPath = "http://" + request.Host.Value + "/login?r=" + reqPath;

                httpResponse.Redirect(redirPath);
                return true;
            }

            var routeRequest = new RouteRequest();

            routeRequest.contentStr = requestContentStr;
            routeRequest.fullUrl = requestPath;
            routeRequest.parameters = parameters;
            routeRequest.hostName = "";
            routeRequest.user = user;
            routeRequest.function = function;
            routeRequest.service = serviceName;
            routeRequest.httpRequest = request;
            routeRequest.action = action;
            routeRequest.token = token;

            var result = await RouteManager.Process(function, serviceName, routeRequest);

            action.AddEvent("Route Processed");

            if (result == null) result = "";

            //if (result != null)
            //if (result.GetType() != typeof(RequestError))
            //{
            var resultStr = "";

            if (routeRequest.responseContentType != "") accepts = routeRequest.responseContentType;

            if (requestPath.Contains("plugins/cordova")) accepts = "application/javascript";

            var returnType = "";

            if (result is CommandResponse)
            {
                var commandResponse = ((CommandResponse)result);

                httpResponse.StatusCode = commandResponse.Status;

                if (!String.IsNullOrEmpty(request?.ContentType)) accepts = request.ContentType;

                if (request.Headers.ContainsKey("Accept"))
                {
                    var firstHeader = request.Headers["Accept"].FirstOrDefault();
                    if (firstHeader == null) firstHeader = "";
                    if (firstHeader.Split(',')[0] != "*/*") returnType = request.Headers["Accept"].First().Split(',')[0];
                    if (accepts != "*/*") returnType = accepts;
                }

                if (commandResponse.ContentType != "") returnType = commandResponse.ContentType;

                if (returnType == "") returnType = "application/json";

                httpResponse.Headers.Append("Content-Type", returnType);

                foreach (var header in commandResponse.Headers)
                {
                    httpResponse.Headers.Append(header.Key, header.Value);
                }

                if (commandResponse.Bytes != null)
                {
                    var bytes = commandResponse.Bytes.ToArray();
                    await httpResponse.Body.WriteAsync(bytes, 0, bytes.Length);
                }
                else if (!String.IsNullOrWhiteSpace(commandResponse.Content))
                {
                    await httpResponse.WriteAsync(commandResponse.Content);
                }
                else if (commandResponse.value is MemoryStream)
                {
                    var bytes = ((MemoryStream)commandResponse.value).ToArray();
                    await httpResponse.Body.WriteAsync(bytes, 0, bytes.Length);
                }
                else
                {
                    await httpResponse.WriteAsync(commandResponse.value == null ? "" : commandResponse.value.ToString());
                }
                return true;

            }
            //else if (result is IEntity) resultStr = ((IEntity)result).ToJson();
            else
            {
                try
                {
                    resultStr = JsonManager.Serialize(result);
                }
                catch (Exception ex)
                {
                    var resultType = result == null ? "null" : result.GetType().ToString();
                    resultStr = $"Error with serialization of type {resultType}. {ex.Message} ";
                }
            }

            if (request.Headers.ContainsKey("Accept"))
            {
                var firstHeader = request.Headers["Accept"].FirstOrDefault();
                if (firstHeader == null) firstHeader = "";
                if (firstHeader.Split(',')[0] != "*/*") returnType = request.Headers["Accept"].First().Split(',')[0];
                if (accepts != "*/*") returnType = accepts;
            }

            if (returnType == "") returnType = "application/json";


            var writeStart = DateTime.Now;

            resultStr = resultStr == null ? "" : resultStr;

            httpResponse.StatusCode = 200;
            httpResponse.Headers.Append("Content-Type", returnType);
            await httpResponse.WriteAsync(resultStr);

            //log the write time
            TotalWriteMs += (DateTime.Now - writeStart).TotalMilliseconds;
            TotalBytesSent += resultStr.Length * 2;
            TotalRequests++;

            //
            if (TotalRequests >= 1000)
            {
                TotalWriteMs = 0;
                TotalBytesSent = 0;
                TotalRequests = 0;
            }

            //response.Content.Headers.ContentType = new MediaTypeHeaderValue(returnType);

            return true;
            //}

            //return false;
        }

    }
}
