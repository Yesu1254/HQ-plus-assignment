using ApiCore.CoreModels;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiCore.Managers
{
    public class RouteRequest
    {
        public string fullUrl { get; set; }

        public string contentStr { get; set; }

        public string function { get; set; }

        public string hostName { get; set; }

        public string parameters { get; set; }

        public string service { get; set; }
        public string token { get; set; }

        public string user { get; set; }

        public int userId { get; }

        [JsonIgnore]
        public HttpRequest httpRequest { get; set; }

        public string responseContentType { get; set; } = "";

        [JsonIgnore]
        public ActiveAction action { get; set; } = new ActiveAction();
    }

    public class Route
    {
        public Func<RouteRequest, Task<object>> action { get; set; }

    }

    public static class RouteManager
    {

        private static Object fileDebugLock = new object();

        private static double debugManifestVersion { get; set; }

        private static ConcurrentDictionary<string, Route> routes { get; set; } = new ConcurrentDictionary<string, Route>(StringComparer.InvariantCultureIgnoreCase);

        public static Dictionary<string, ICommandBase> commands { get; set; }

        public static void RegisterRoute(string function, string name, Route route)
        {
            if (routes.ContainsKey(function + "_" + name))
            {
                routes[function + "_" + name] = route;
            }
            else
            {
                routes.TryAdd(function + "_" + name, route);
            }
        }

        public static async Task<object> Process(string function, string routeName, RouteRequest request)
        {
            var functionAndName = function + "_" + routeName;
            if (!routes.ContainsKey(functionAndName))
            {
                //var delContainer = DelegateManager.GetDelegate(routeName);
                //if (delContainer)

                return await routes[function + "_" + "*"].action(request);
            }

            return await routes[functionAndName].action(request);
        }


        private static ConcurrentDictionary<string, ICommandBase> serviceInstances { get; set; } = new ConcurrentDictionary<string, ICommandBase>();
        //private static ConcurrentDictionary<Type, CommandParamMap> commandParamMaps = new ConcurrentDictionary<Type, CommandParamMap>();


        public static void RegisterDefaultRoutes()
        {
            #region Queues and Commands


            //delegate resolver
            var delegateResolverRoute = new Route();

            //delegateResolverRoute.action = ProcessCommand;

            RegisterRoute("q", "*", delegateResolverRoute);
            RegisterRoute("c", "*", delegateResolverRoute);
            RegisterRoute("s", "*", delegateResolverRoute);
            RegisterRoute("s", "*", delegateResolverRoute);

            #endregion

            #region Legacy Commands
            if (UnityManager.Commands == null) throw new Exception("No Commands Loaded");
            var services = UnityManager.Commands.ToList();

            var serviceInsts = new Dictionary<string, ICommandBase>();

            foreach (var service in services)
            {
                if (serviceInsts.ContainsKey(service.CommandName.ToLower()))
                {
                    continue;
                    //if (!service.GetType().ToString().Contains(".Core.")) serviceInsts[service.CommandName.ToLower()] = service;
                }
                else
                {
                    serviceInsts.Add(service.CommandName.ToLower(), service);
                }


            }

            commands = serviceInsts;

            foreach (var command in serviceInsts.Select(x => x.Value))
            {
                //var value = service.CreateExport().Value;

                serviceInstances.TryAdd(command.CommandName.ToLower(), command);

                var serviceRoute = new Route();

                serviceRoute.action = async (RouteRequest request) =>
                {
                    request.action.AddEvent("Action Started");

                    //check if we should use the new command engine
                    //if (DelegateManager.GetDelegate(request.service) != null) return await 
                    //await ProcessCommand(request);

                    var commandMap = CommandManager.GetCommandParamMap(command.GetType());

                    request.action.AddEvent("Paramaters Mapped");

                   // if (request.user == null && command.BypassAuthentication == false) throw new Exception($"Anonymous access it prohibited for the {command.CommandName.InsertSpaces()} service");

                    if (request.user != null && command.BypassAuthentication == false)
                    {
                        //if (!SecurityManager.IsAllowed(request.user, command.CommandName, "Command Access")) throw new Exception($"Access denied for command: '{command.CommandName.InsertSpaces()}'");
                    }

                    //if (request.user != null && command.BypassAuthentication == false && request.user.isAdmin != true)
                    //{
                    //    var userSettings = EntitySetting.EntitySettingManager.GetEntitySetting<List<NameEntity>>(typeof(IUserGroup), request.user.userGroupId.GetValueOrDefault(0), "ALLOWEDCOMMANDS");
                    //    if (!userSettings.Any(us => us.id == command.UniqueId))
                    //    {
                    //        throw new Exception($"Access denied for command: '{command.CommandName}'");
                    //    }
                    //}


                    object result = null;

                    var userId = "2";
                    double elapsedMilliseconds = 0;

                    try
                    {

                        //lock (fileDebugLock)
                        //{
                        //    try
                        //    {
                        //        System.IO.File.AppendAllText(GlobalsManager.GetCurrentDirectory + "Temp_Service_Execution_Log.txt", $"{System.Threading.Thread.CurrentThread.ManagedThreadId},{commandMap.CommandName},{userId},{DateTime.Now.ToString()}\r\n ");
                        //    }
                        //    catch (Exception ex)
                        //    {
                        //    }
                        //}

                        //if (command.MaxSimultaneousExecutions > 0 && command.MaxSimultaneousExecutions < commandMap.CurrentlyRunning)
                        //{
                        //    //throw new Exception($"Max Simultaneous Executions Exceeded: {commandMap.CurrentlyRunning}");
                        //}

                        var startTime = DateTime.Now;

                        //commandMap.CurrentlyRunning++;

                        result = await commandMap.CallRunMethod(request.parameters, request.contentStr, request);
                        //commandMap.CurrentlyRunning--;

                        //var endTime = DateTime.Now;
                        var runTime = (DateTime.Now - startTime);
                        elapsedMilliseconds = runTime.TotalMilliseconds;

                        try
                        {
                            //commandMap.Executions += 1;
                            //commandMap.TotalExecutionTime = commandMap.TotalExecutionTime.Add(runTime);
                            //if (commandMap.SlowestExecutionTime < runTime)
                            //{
                            //    commandMap.SlowestExecutionTime = runTime;
                            //    commandMap.SlowestExecutionTimeDate = startTime;
                            //}
                        }
                        catch (Exception ex)
                        {
                            //do nothing as this was a thread lock exception
                        }

                        //lock (fileDebugLock)
                        //{
                        //    try
                        //    {
                        //        System.IO.File.AppendAllText(GlobalsManager.GetCurrentDirectory + "Temp_Service_Execution_Log.txt", $"{System.Threading.Thread.CurrentThread.ManagedThreadId},{commandMap.CommandName},{userId},{DateTime.Now.ToString()}, {elapsedMilliseconds}\r\n ");
                        //    }
                        //    catch (Exception ex)
                        //    {
                        //    }
                        //}


                        try
                        {
                            object res = result;
                            if (result is CommandResponse)
                            {
                                res = "COMMAND RESPONSE";
                            }

                            //if(!request.fullUrl.Contains("debug")) CommandLogManager.LogServiceRequest(commandMap.CommandName, request.fullUrl, userId, request.contentStr, res, false, elapsedMilliseconds);
                        }
                        catch (Exception exLog)
                        {
                            //SystemEventManager.WriteError(request, "Error writing command log: " + exLog.Message);
                            //throw;
                        }
                    }
                    catch (Exception ex)
                    {
                        //commandMap.CurrentlyRunning--;
                        if (ex.InnerException != null) ex = ex.InnerException;
                        //SystemEventManager.WriteError(request, "Command Exception: " + ex.Message);
                        try
                        {

                            // CommandLogManager.LogServiceRequest(commandMap.CommandName, request.fullUrl, request.user, request.contentStr, ex.ToString(), true, elapsedMilliseconds, request.token);

                            request.action.AddEvent("Service Request Logged");
                        }
                        catch (Exception exLog)
                        {
                            // SystemEventManager.WriteError(request, "Error writing command log: " + exLog.Message);
                        }

                        return new CommandResponse($"{{\"ex\": \"{ex.Message.Replace("\r", "").Replace("\n", "").Replace("\"", "'")}\"}}", "application/json");
                    }

                    if (command.LogRequests)
                    {
                        var resultStr = "null";
                        if (result != null)
                        {
                            try
                            {
                                resultStr = JsonManager.Serialize(result);
                            }
                            catch (Exception ex)
                            {
                                resultStr = result.ToString();
                                throw;
                            }
                        }
                        // CommandLogManager.LogServiceRequest(commandMap.CommandName, request.fullUrl, request.user, request.contentStr, resultStr, false, elapsedMilliseconds, request.token);

                    }

                    return result;
                };

                RegisterRoute("s", command.CommandName.ToLower(), serviceRoute);

            }
            #endregion

            #region Files

            var streamResolverRoute = new Route();
            //var streamResolverService = UnityManager.GetCommand<StreamResolver>(); //serviceInstances["streamresolver"] as IStreamResolverCommand;

            //streamResolverRoute.action = async (RouteRequest request) =>
            //{
            //    var fileName = request.service.Split('&')[0];

            //    var result = streamResolverService.Run("");

            //    if (fileName.EndsWith(".manifest"))
            //    {
            //        //result.ContentType = "text/cache-manifest";

            //        byte[] bytes = null;

            //        bytes = System.Text.Encoding.UTF8.GetBytes("\r#" + GlobalsManager.StartupTime + "\r");

            //      //  var ms = result.value as MemoryStream;

            //       // var msBytes = ms.ToArray();
            //        //byte[] newArray = new byte[msBytes.Length + bytes.Length];
            //        //Array.Copy(msBytes, newArray, msBytes.Length);
            //        //Array.Copy(bytes, 0, newArray, msBytes.Length, bytes.Length);

            //       // result.value = new MemoryStream(newArray);
            //      //  ms.Position = 0;
            //    }
            //    //else if (fileName.EndsWith(".css")) result.ContentType = "text/css";
            //    //else if (fileName.EndsWith(".js")) result.ContentType = "text/javascript";
            //    //else if (fileName.EndsWith(".png")) result.ContentType = "text/png";
            //    //else if (fileName.EndsWith(".jpg")) result.ContentType = "text/jpg";
            //    //else if (fileName.EndsWith(".gif")) result.ContentType = "text/gif";
            //    //else if (fileName.EndsWith(".html")) result.ContentType = "text/html";

            //    return result;
            //};

            RegisterRoute("f", "*", streamResolverRoute);


            #endregion


        }

        //public static async Task<object> ProcessCommand(RouteRequest request)
        //{
        //   // var delContainer = DelegateManager.GetDelegate(request.service);

        //    //if (delContainer == null)
        //    //{
        //    //    CommandLogManager.LogServiceRequest("not found: " + request.service, request.fullUrl, request.user, request.contentStr, "Command not found", true, 0, request.token);
        //    //    throw new Exception("Command not found: " + request.service);
        //    //}

        //  //  var commandAttribute = delContainer.additionalInfo as Command;
        //  //  var commandMap = CommandManager.CommandManager.GetCommandParamMap(delContainer);


        //    //var maxSimultaneousExecutions = commandAttribute == null || commandAttribute.maxSimultaneousExecutions == 0 ? ConfigManager.current.maxSimultaneousExecutions : commandAttribute.maxSimultaneousExecutions;

        //    //if (maxSimultaneousExecutions < commandMap.CurrentlyRunning)
        //    //{
        //    //    //throw new Exception($"Max Simultaneous Executions Exceeded: {commandMap.CurrentlyRunning}");
        //    //}

        //    //if (commandAttribute != null)
        //    //{
        //    //    if (commandAttribute.bypassAuthentication == false && request.user == null)
        //    //    {
        //    //        var commandName = commandMap == null ? $"unknown:{request.service}" : commandMap.CommandName;

        //    //        #region Construct Query

        //    //        var rawRequest = new StringBuilder();

        //    //        if (request.httpRequest.QueryString != null) rawRequest.AppendLine("Query:" + request.httpRequest.QueryString.Value);

        //    //        rawRequest.AppendLine();

        //    //        foreach (var header in request.httpRequest.Headers)
        //    //        {
        //    //            rawRequest.AppendLine($"{header.Key}: {header.Value}");
        //    //        }

        //    //        if (request.httpRequest.Cookies != null && request.httpRequest.Cookies.Count > 0)
        //    //        {
        //    //            rawRequest.AppendLine();
        //    //            rawRequest.AppendLine("Cookies");
        //    //            foreach (var cookie in request.httpRequest.Cookies)
        //    //            {
        //    //                rawRequest.AppendLine($"{cookie.Key}: {cookie.Value}");
        //    //            }
        //    //        }

        //    //        rawRequest.AppendLine();
        //    //        rawRequest.AppendLine(request.contentStr);

        //    //        #endregion

        //    //        //CommandLogManager.LogServiceRequest(commandName, request.fullUrl, request.user, request.contentStr, "Anonymous access prohibited: Raw: " + rawRequest.ToString(), true, 0, request.token);
        //    //        throw new Exception("Anonymous access prohibited");
        //    //    }
        //    //}





        //    //var startTime = DateTime.Now;
        //    //try
        //    //{
        //    //    commandMap.CurrentlyRunning++;

        //    //    var result = await commandMap.CallRunMethod(request.parameters, request.contentStr, request);

        //    //    commandMap.CurrentlyRunning--;

        //    //    if (commandAttribute != null && commandAttribute.logRequests)
        //    //    {
        //    //        var runTime = (DateTime.Now - startTime);
        //    //        var elapsedMilliseconds = runTime.TotalMilliseconds;
        //    //        //CommandLogManager.LogServiceRequest(commandMap.CommandName, request.fullUrl, request.user, request.contentStr, JsonManager.Serialize(result), false, elapsedMilliseconds, request.token);
        //    //    }

        //    //    return result;
        //    //}
        //    //catch (Exception ex)
        //    //{
        //    //    commandMap.CurrentlyRunning--;
        //    //    var runTime = (DateTime.Now - startTime);
        //    //    var elapsedMilliseconds = runTime.TotalMilliseconds;
        //    //    while (ex.InnerException != null) ex = ex.InnerException;
        //    //    var message = ex.Message;
        //    //    //CommandLogManager.LogServiceRequest(commandMap.CommandName, request.fullUrl, request.user, request.contentStr, message, true, elapsedMilliseconds, request.token);
        //    //    return new { ex = message }; //$"{{\"ex\":\"{message}\"}}";
        //    //}


        //}

        //public static CommandParamMap GetCommandParamMap(Type commandType)
        //{
        //    return commandParamMaps.GetOrAdd(commandType, (t) => new CommandParamMap(t));
        //}

        //public static CommandParamMap GetCommandParamMap(DelegateContainer delContainer)
        //{
        //    return commandParamMaps.GetOrAdd(delContainer.delType, (t) => new CommandParamMap(delContainer));
        //}


    }
}
