using ApiCore.CoreModels;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ApiCore.Managers
{
    public class CommandExecutionLog
    {
        public string CommandName { get; set; }
        public int ExecutionCount { get; set; }
        public double TotalExecutionTime { get; set; }
    }

    public class DelegateTypeHookRegistration
    {
        public Type registrationType;
        public Func<Type, object> callback;
    }

    public class DelegateContainer
    {
        public Type delType;
        public MethodInfo methodInfo;
        public Delegate replacementMethod;
        public Delegate method;
        public ParameterInfo[] parameters;
        public Dictionary<Type, DelegateTypeHookRegistration> hooks = new Dictionary<Type, DelegateTypeHookRegistration>();
        public Object additionalInfo;

        public Type GetImplementationClass()
        {
            return method.Method.DeclaringType.DeclaringType;
        }

    }

    public class CommandParamMap
    {
        public string CommandName { get; set; }

        public Type CommandType { get; set; }

        public Delegate RunMethod { get; set; }

        public List<ParameterInfo> Parameters { get; set; }

        public bool IsValid { get; set; } = true;

        public int? CacheMinutes { get; set; }

        public Func<Object, bool> CacheBypass { get; set; }

        private static CustomJsonSerializer customSerializer { get; set; } = new CustomJsonSerializer();
        //private static JsonSerializerSettings customSerializer { get; set; } = JsonManager.GetSerializerSettings();

        //legacy
        public static Delegate CreateDelegate(object instance, MethodInfo method)
        {
            var parameters = method.GetParameters()
                       .Select(p => Expression.Parameter(p.ParameterType, p.Name))
                        .ToArray();

            var call = Expression.Call(Expression.Constant(instance), method, parameters);
            return Expression.Lambda(call, parameters).Compile();
        }

        public CommandParamMap(DelegateContainer delContainer)
        {
            CommandType = delContainer.delType;

            CommandName = delContainer.delType.Name;

            var commandAttribute = delContainer.additionalInfo as ApiCore.Managers.Command;

            if (commandAttribute != null)
            {
                CacheMinutes = commandAttribute.cacheMinutes;

                //CacheBypass = additionalInfo.cac;
            }

            RunMethod = delContainer.replacementMethod;

            Parameters = delContainer.parameters.ToList();
        }

        public CommandParamMap(Type commandType)
        {
            CommandType = commandType;

            CommandName = commandType.Name;

            var legacyCommand = Activator.CreateInstance(commandType) as ICommandBase;

            if (legacyCommand != null)
            {
                CacheMinutes = legacyCommand.CacheMinutes;
                CacheBypass = legacyCommand.CacheBypass;

                try
                {
                    var method = commandType.GetMethod("Run", BindingFlags.Public | BindingFlags.Instance);
                    RunMethod = CreateDelegate(legacyCommand, method);
                    Parameters = method.GetParameters()?.ToList();

                }
                catch (Exception x)
                {

                    throw;
                }
            }
            else
            {
                //required for new delegate code
            }
        }

        public async Task<Object> CallRunMethod(string url, string contentStr, RouteRequest request)
        {

            List<object> parameters = Parameters == null || Parameters.Count == 0 ? null : new List<object>();

            if (url == null) url = "";

            //consider doing a urldecode here. This was requried for here maps
            url = url.ToLower();// .Replace("%26", "&");
            var urlElements = url.Split('&');

            JObject contentArgs = new JObject();

            //temp hack
            var tempParams = Parameters.ToList();
            if (tempParams.Count > 0 && tempParams[0].ParameterType == typeof(RouteRequest))
            {
                parameters.Add(request);
                tempParams.RemoveAt(0);
            }

            try
            {
                if (contentStr != "") contentArgs = JObject.Parse(contentStr);
            }
            catch (Exception ex)
            {

            }

            #region append url tokens

            foreach (var param in tempParams)
            {
                //var paramFoundInUrl = false;
                foreach (var urlElement in urlElements)
                {
                    if (urlElement.StartsWith(param.Name.ToLower() + "="))
                    {
                        var strVal = urlElement.Substring(urlElement.IndexOf("=") + 1);

                        //only parse if the following types
                        if (param.ParameterType == typeof(int) || param.ParameterType == typeof(string) || param.ParameterType == typeof(double) || param.ParameterType == typeof(float) || param.ParameterType == typeof(bool))
                        {
                            contentArgs.AddFirst(new JProperty(param.Name, strVal));
                            continue;
                        }
                        //nullable types
                        else if (param.ParameterType == typeof(int?) || param.ParameterType == typeof(double?) || param.ParameterType == typeof(float?) || param.ParameterType == typeof(bool?))
                        {
                            contentArgs.AddFirst(new JProperty(param.Name, strVal));
                            continue;
                        }
                        else
                        {
                            if (strVal.StartsWith("{"))
                            {
                                var jProp = new JProperty(param.Name, JObject.Parse(strVal));
                                contentArgs.AddFirst(jProp);
                            }
                        }

                    }
                    //if (paramFoundInUrl) continue;
                }
            }
            #endregion
           // var paramCount = tempParams.Count(x => (x.ParameterType.IsDelegate() == false));
            var paramCount = tempParams.Count();


            if (paramCount == 1 && contentArgs.GetValue(tempParams[0].Name, StringComparison.InvariantCultureIgnoreCase) == null)
            {
                if (tempParams[0].ParameterType == typeof(int))
                {
                    var jObj = contentArgs.GetValue(Parameters[0].Name, StringComparison.InvariantCultureIgnoreCase);
                    if (jObj == null && Parameters[0].IsOptional)
                    {
                        var defaultVal = Parameters[0].DefaultValue; //GetDefaultValue(command.GetType(), Parameters[0].Name);

                        parameters.Add(defaultVal);
                    }
                    else
                    {
                        var intValue = (int)jObj;
                        parameters.Add(intValue);
                    }
                }
                else if (tempParams[0].ParameterType == typeof(string))
                {
                    var jObj = contentArgs.GetValue(tempParams[0].Name, StringComparison.InvariantCultureIgnoreCase);
                    if (jObj == null && tempParams[0].IsOptional)
                    {
                        parameters.Add(tempParams[0].DefaultValue);
                    }
                    else
                    {
                        var strValue = (string)jObj;
                        parameters.Add(strValue);
                    }
                }
                else if (tempParams[0].ParameterType == typeof(double))
                {
                    var jObj = contentArgs.GetValue(tempParams[0].Name, StringComparison.InvariantCultureIgnoreCase);
                    if (jObj == null && tempParams[0].IsOptional)
                    {
                        parameters.Add(tempParams[0].DefaultValue);
                    }
                    else
                    {
                        var dblValue = (double)jObj;
                        parameters.Add(dblValue);
                    }
                }
                else if (tempParams[0].ParameterType == typeof(bool))
                {
                    var jObj = contentArgs.GetValue(tempParams[0].Name, StringComparison.InvariantCultureIgnoreCase);
                    if (jObj == null && tempParams[0].IsOptional)
                    {
                        //parameters.Add(Parameters[0].DefaultValue);
                        parameters.Add(false);
                    }
                    else
                    {
                        var boolValue = (bool)jObj;
                        parameters.Add(boolValue);
                    }
                }
                else if (tempParams[0].ParameterType == typeof(DateTime))
                {
                    var jObj = contentArgs.GetValue(tempParams[0].Name, StringComparison.InvariantCultureIgnoreCase);
                    if (jObj == null && tempParams[0].IsOptional)
                    {
                        parameters.Add(tempParams[0].DefaultValue);
                    }
                    else
                    {
                        var dtValue = (DateTime)jObj;
                        parameters.Add(dtValue);
                    }
                }

            }
            else
            {
                foreach (var param in tempParams)
                {

                    if (contentArgs[param.Name] == null)
                    {
                        if (param.IsOptional)
                        {
                            var defaultVal = param.DefaultValue; //GetDefaultValue(command.GetType(), param.Name);

                            parameters.Add(defaultVal);
                            continue;
                        }
                        //else if (param.ParameterType.IsGenericType && param.ParameterType.IsClass == false)
                        //{
                        //    //if (param.ParameterType.IsNullable())
                        //    //{
                        //    //    parameters.Add(request);
                        //    //}
                        //    if (DelegateManager.HasDelegate(param.ParameterType))
                        //    {
                        //        parameters.Add(DelegateManager.GetDelegate(param.ParameterType).replacementMethod);
                        //    }
                        //    else
                        //    {
                        //        var genericType = param.ParameterType.GetGenericTypeDefinition();
                        //        var genericVersion = DelegateManager.GetDelegate(genericType);

                        //        var del = DelegateManager.CreateGenericDelegate(param.ParameterType, genericVersion.methodInfo);

                        //        parameters.Add(del);
                        //    }


                        //}
                        //else if (DelegateManager.HasDelegate(param.ParameterType))
                        //{
                        //    parameters.Add(DelegateManager.GetDelegate(param.ParameterType).replacementMethod);
                        //}
                        //else if (param.ParameterType == typeof(RouteRequest))
                        //{
                        //    parameters.Add(request);
                        //    continue;
                        //}
                        //else
                        //{
                        //    throw new Exception($"Paramater missing from request: {param.Name}");
                        //}
                    }

                    var jObj = contentArgs.GetValue(param.Name, StringComparison.InvariantCultureIgnoreCase);
                    if (jObj == null)
                    {
                        //do nothing
                    }
                    else if (param.ParameterType == typeof(int))
                    {
                        var intValue = (int)jObj;
                        parameters.Add(intValue);
                    }
                    else if (param.ParameterType == typeof(int?))
                    {
                        var intStr = jObj.ToString();
                        var intValue = intStr.EqualsIgnoreCase("null") ? (int?)null : (int?)jObj;
                        parameters.Add(intValue);
                    }
                    else if (param.ParameterType == typeof(string))
                    {
                        var strValue = (string)jObj;
                        parameters.Add(strValue);
                    }
                    else if (param.ParameterType == typeof(double))
                    {
                        var dblValue = (double)jObj;
                        parameters.Add(dblValue);
                    }
                    else if (param.ParameterType == typeof(bool))
                    {
                        var boolValue = (bool)jObj;
                        parameters.Add(boolValue);
                    }
                    else if (param.ParameterType == typeof(DateTime))
                    {
                        var dtValue = (DateTime)jObj;
                        parameters.Add(dtValue);
                    }
                    else
                    {
                        try
                        {
                            var obj = jObj.ToObject(param.ParameterType, customSerializer);
                            parameters.Add(obj);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Error serializaing object for param {param.Name}: {ex.Message}");
                        }
                    }
                }
            }


            object result = null;

            request.action.AddEvent("Paramaters Set");

            var isCacheCall = CacheMinutes != null && CacheMinutes > 0;

            if (isCacheCall)
            {
                Func<object> methodReturn = () =>
                {
                    var res = RunMethod.DynamicInvoke(parameters == null ? null : parameters.ToArray());
                    //newCommand.Dispose();

                    return res;
                };

                if (parameters == null) parameters = new List<object>();
                var args = parameters.ToList();
                //args.Remove(request);

                //remove any routerequests as they will always be different (hence no caching)
                args = args.Where(a => a == null || a.GetType() != typeof(RouteRequest)).ToList();

                //if value == false or null or empty list then run
                    if (CacheBypass(result))
                    {
                        result = RunMethod.DynamicInvoke(parameters == null ? null : parameters.ToArray());
                        //newCommand.Dispose();
                    }

            }
            else
            {
                result = RunMethod.DynamicInvoke(parameters == null ? null : parameters.ToArray());
                //newCommand.Dispose();

            }

            //handle async functions
            if (result is Task)
            {
                dynamic task = result;

                return await task;
                //var task = result as Task;
                //Task<object> castedTask = task.ContinueWith(t => (object)t.Result);
                //var res = await Task.FromResult<object>(task);
                //return await res;
            }

            if (isCacheCall) request.action.AddEvent("Cache call completed");
            else request.action.AddEvent("Non-cache call completed");




            //var result = RunMethod.Invoke(command, parameters == null ? null : parameters.ToArray());
            return result;
        }

        //This is to overcome and issue with Unity, whereby it does not pass back the DefaultValue for optional parameters
        private static ConcurrentDictionary<string, Object> defaultValues { get; set; } = new ConcurrentDictionary<string, object>();
        public int Executions { get; set; } = 0;
        public TimeSpan TotalExecutionTime { get; set; } = new TimeSpan();
        public TimeSpan SlowestExecutionTime { get; set; } = new TimeSpan();
        public DateTime SlowestExecutionTimeDate { get; set; } = new DateTime();
        public int CurrentlyRunning { get; set; } = 0;

        private static object GetDefaultValue(Type commandBase, string paramName)
        {

            if (defaultValues.ContainsKey($"{commandBase.ToString()}_{paramName}"))
            {
                return defaultValues[$"{commandBase.ToString()}_{paramName}"];
            }

            //var commandInterfaces = commandBase.GetInterfaces();
            //var commandInterface = commandBase.GetInterfaces()[1];

            var runMethod = commandBase.GetMethod("Run");

            var methodParams = runMethod.GetParameters();

            object result = null;
            foreach (var param in methodParams)
            {
                if (param.IsOptional && param.Name == paramName)
                {
                    result = param.DefaultValue;
                }
            }

            defaultValues.TryAdd($"{commandBase.ToString()}_{paramName}", result);

            return result;
        }

    }

    public class CommandManager
    {
        public string CommandName { get; set; }

        public Type CommandType { get; set; }

        public Delegate RunMethod { get; set; }

        public List<ParameterInfo> Parameters { get; set; }

        public bool IsValid { get; set; } = true;

        public int? CacheMinutes { get; set; }

        public Func<Object, bool> CacheBypass { get; set; }

        private static CustomJsonSerializer customSerializer { get; set; } = new CustomJsonSerializer();
        //private static JsonSerializerSettings customSerializer { get; set; } = JsonManager.GetSerializerSettings();

        //legacy
        public static Delegate CreateDelegate(object instance, MethodInfo method)
        {
            var parameters = method.GetParameters()
                       .Select(p => Expression.Parameter(p.ParameterType, p.Name))
                        .ToArray();

            var call = Expression.Call(Expression.Constant(instance), method, parameters);
            return Expression.Lambda(call, parameters).Compile();
        }

        public async Task<Object> CallRunMethod(string url, string contentStr, RouteRequest request)
        {

            List<object> parameters = Parameters == null || Parameters.Count == 0 ? null : new List<object>();

            if (url == null) url = "";

            //consider doing a urldecode here. This was requried for here maps
            url = url.ToLower();// .Replace("%26", "&");
            var urlElements = url.Split('&');

            JObject contentArgs = new JObject();

            //temp hack
            var tempParams = Parameters.ToList();
            if (tempParams.Count > 0 && tempParams[0].ParameterType == typeof(RouteRequest))
            {
                parameters.Add(request);
                tempParams.RemoveAt(0);
            }

            try
            {
                if (contentStr != "") contentArgs = JObject.Parse(contentStr);
            }
            catch (Exception ex)
            {

            }

            #region append url tokens

            foreach (var param in tempParams)
            {
                //var paramFoundInUrl = false;
                foreach (var urlElement in urlElements)
                {
                    if (urlElement.StartsWith(param.Name.ToLower() + "="))
                    {
                        var strVal = urlElement.Substring(urlElement.IndexOf("=") + 1);

                        //only parse if the following types
                        if (param.ParameterType == typeof(int) || param.ParameterType == typeof(string) || param.ParameterType == typeof(double) || param.ParameterType == typeof(float) || param.ParameterType == typeof(bool))
                        {
                            contentArgs.AddFirst(new JProperty(param.Name, strVal));
                            continue;
                        }
                        //nullable types
                        else if (param.ParameterType == typeof(int?) || param.ParameterType == typeof(double?) || param.ParameterType == typeof(float?) || param.ParameterType == typeof(bool?))
                        {
                            contentArgs.AddFirst(new JProperty(param.Name, strVal));
                            continue;
                        }
                        else
                        {
                            if (strVal.StartsWith("{"))
                            {
                                var jProp = new JProperty(param.Name, JObject.Parse(strVal));
                                contentArgs.AddFirst(jProp);
                            }
                        }

                    }
                    //if (paramFoundInUrl) continue;
                }
            }
            #endregion

            var paramCount = tempParams.Count();


            if (paramCount == 1 && contentArgs.GetValue(tempParams[0].Name, StringComparison.InvariantCultureIgnoreCase) == null)
            {
                if (tempParams[0].ParameterType == typeof(int))
                {
                    var jObj = contentArgs.GetValue(Parameters[0].Name, StringComparison.InvariantCultureIgnoreCase);
                    if (jObj == null && Parameters[0].IsOptional)
                    {
                        var defaultVal = Parameters[0].DefaultValue; //GetDefaultValue(command.GetType(), Parameters[0].Name);

                        parameters.Add(defaultVal);
                    }
                    else
                    {
                        var intValue = (int)jObj;
                        parameters.Add(intValue);
                    }
                }
                else if (tempParams[0].ParameterType == typeof(string))
                {
                    var jObj = contentArgs.GetValue(tempParams[0].Name, StringComparison.InvariantCultureIgnoreCase);
                    if (jObj == null && tempParams[0].IsOptional)
                    {
                        parameters.Add(tempParams[0].DefaultValue);
                    }
                    else
                    {
                        var strValue = (string)jObj;
                        parameters.Add(strValue);
                    }
                }
                else if (tempParams[0].ParameterType == typeof(double))
                {
                    var jObj = contentArgs.GetValue(tempParams[0].Name, StringComparison.InvariantCultureIgnoreCase);
                    if (jObj == null && tempParams[0].IsOptional)
                    {
                        parameters.Add(tempParams[0].DefaultValue);
                    }
                    else
                    {
                        var dblValue = (double)jObj;
                        parameters.Add(dblValue);
                    }
                }
                else if (tempParams[0].ParameterType == typeof(bool))
                {
                    var jObj = contentArgs.GetValue(tempParams[0].Name, StringComparison.InvariantCultureIgnoreCase);
                    if (jObj == null && tempParams[0].IsOptional)
                    {
                        //parameters.Add(Parameters[0].DefaultValue);
                        parameters.Add(false);
                    }
                    else
                    {
                        var boolValue = (bool)jObj;
                        parameters.Add(boolValue);
                    }
                }
                else if (tempParams[0].ParameterType == typeof(DateTime))
                {
                    var jObj = contentArgs.GetValue(tempParams[0].Name, StringComparison.InvariantCultureIgnoreCase);
                    if (jObj == null && tempParams[0].IsOptional)
                    {
                        parameters.Add(tempParams[0].DefaultValue);
                    }
                    else
                    {
                        var dtValue = (DateTime)jObj;
                        parameters.Add(dtValue);
                    }
                }

            }
            else
            {
                foreach (var param in tempParams)
                {

                    if (contentArgs[param.Name] == null)
                    {
                        if (param.IsOptional)
                        {
                            var defaultVal = param.DefaultValue; //GetDefaultValue(command.GetType(), param.Name);

                            parameters.Add(defaultVal);
                            continue;
                        }
                        else if (param.ParameterType.IsGenericType && param.ParameterType.IsClass == false)
                        {
                            //if (param.ParameterType.IsNullable())
                            //{
                            //    parameters.Add(request);
                            //}


                        }
                        else if (param.ParameterType == typeof(RouteRequest))
                        {
                            parameters.Add(request);
                            continue;
                        }
                        else
                        {
                            throw new Exception($"Paramater missing from request: {param.Name}");
                        }
                    }

                    var jObj = contentArgs.GetValue(param.Name, StringComparison.InvariantCultureIgnoreCase);
                    if (jObj == null)
                    {
                        //do nothing
                    }
                    else if (param.ParameterType == typeof(int))
                    {
                        var intValue = (int)jObj;
                        parameters.Add(intValue);
                    }
                    else if (param.ParameterType == typeof(int?))
                    {
                        var intStr = jObj.ToString();
                        var intValue = intStr.EqualsIgnoreCase("null") ? (int?)null : (int?)jObj;
                        parameters.Add(intValue);
                    }
                    else if (param.ParameterType == typeof(string))
                    {
                        var strValue = (string)jObj;
                        parameters.Add(strValue);
                    }
                    else if (param.ParameterType == typeof(double))
                    {
                        var dblValue = (double)jObj;
                        parameters.Add(dblValue);
                    }
                    else if (param.ParameterType == typeof(bool))
                    {
                        var boolValue = (bool)jObj;
                        parameters.Add(boolValue);
                    }
                    else if (param.ParameterType == typeof(DateTime))
                    {
                        var dtValue = (DateTime)jObj;
                        parameters.Add(dtValue);
                    }
                    else
                    {
                        try
                        {
                            var obj = jObj.ToObject(param.ParameterType, customSerializer);
                            parameters.Add(obj);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Error serializaing object for param {param.Name}: {ex.Message}");
                        }
                    }
                }
            }


            //var newCommand = command; //UnityManager.Create(command.GetType()) as ICommandBase;
            //newCommand.Request = request;

            //var target = newCommand; //input.Target as ICommandBase;

            object result = null;

            request.action.AddEvent("Paramaters Set");

            var isCacheCall = CacheMinutes != null && CacheMinutes > 0;

            if (isCacheCall)
            {
                Func<object> methodReturn = () =>
                {
                    var res = RunMethod.DynamicInvoke(parameters == null ? null : parameters.ToArray());
                    //newCommand.Dispose();

                    return res;
                };

                if (parameters == null) parameters = new List<object>();
                var args = parameters.ToList();
                //args.Remove(request);

                //remove any routerequests as they will always be different (hence no caching)
                args = args.Where(a => a == null || a.GetType() != typeof(RouteRequest)).ToList();

                if (CacheBypass(result))
                {
                    result = RunMethod.DynamicInvoke(parameters == null ? null : parameters.ToArray());
                    //newCommand.Dispose();
                }

            }
            else
            {
                result = RunMethod.DynamicInvoke(parameters == null ? null : parameters.ToArray());
                //newCommand.Dispose();

            }

            //handle async functions
            if (result is Task)
            {
                dynamic task = result;

                return await task;
            }

            if (isCacheCall) request.action.AddEvent("Cache call completed");
            else request.action.AddEvent("Non-cache call completed");




            //var result = RunMethod.Invoke(command, parameters == null ? null : parameters.ToArray());
            return result;
        }

        //This is to overcome and issue with Unity, whereby it does not pass back the DefaultValue for optional parameters
        private static ConcurrentDictionary<string, Object> defaultValues { get; set; } = new ConcurrentDictionary<string, object>();
        public int Executions { get; set; } = 0;
        public TimeSpan TotalExecutionTime { get; set; } = new TimeSpan();
        public TimeSpan SlowestExecutionTime { get; set; } = new TimeSpan();
        public DateTime SlowestExecutionTimeDate { get; set; } = new DateTime();
        public int CurrentlyRunning { get; set; } = 0;

        private static object GetDefaultValue(Type commandBase, string paramName)
        {

            if (defaultValues.ContainsKey($"{commandBase.ToString()}_{paramName}"))
            {
                return defaultValues[$"{commandBase.ToString()}_{paramName}"];
            }

            //var commandInterfaces = commandBase.GetInterfaces();
            //var commandInterface = commandBase.GetInterfaces()[1];

            var runMethod = commandBase.GetMethod("Run");

            var methodParams = runMethod.GetParameters();

            object result = null;
            foreach (var param in methodParams)
            {
                if (param.IsOptional && param.Name == paramName)
                {
                    result = param.DefaultValue;
                }
            }

            defaultValues.TryAdd($"{commandBase.ToString()}_{paramName}", result);

            return result;
        }

        private static ConcurrentDictionary<Type, CommandParamMap> commandParamMaps = new ConcurrentDictionary<Type, CommandParamMap>();

        public static CommandParamMap GetCommandParamMap(Type commandType)
        {
            return commandParamMaps.GetOrAdd(commandType, (t) => new CommandParamMap(t));
        }

        public static CommandParamMap GetCommandParamMap(DelegateContainer delContainer)
        {
            return commandParamMaps.GetOrAdd(delContainer.delType, (t) => new CommandParamMap(delContainer));
        }

    }

}

