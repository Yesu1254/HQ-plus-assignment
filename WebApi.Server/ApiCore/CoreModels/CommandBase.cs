using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ApiCore.Managers;

namespace ApiCore.CoreModels
{
    public abstract class CommandBase : ICommandBase
    {
        private string commandName = "";
        public string CommandName
        {
            get
            {
                if (commandName == "")
                {
                    var typeName = this.GetType().Name;
                    if (typeName.EndsWith("Command")) typeName = typeName.Substring(0, typeName.Length - "Command".Length);
                    commandName = typeName;
                }
                return commandName;
            }
        }

        //public bool IsRunning { get; set; }

        public int? CacheMinutes { get; set; }

        public Func<Object, bool> CacheBypass { get; set; }

        //public RouteRequest Request { get; set; }
        public string RequestContentStr { get; set; }

        public bool BypassAuthentication { get; set; }

        //public Task CurrentTask { get; set; }

        public bool Queued { get; set; }

        public bool LogRequests { get; set; }

        public int MaxSimultaneousExecutions { get; set; }

        public virtual bool HasValue(string settingName, Object testValue = null)
        {
            return true;
        }

        public virtual Result AutomationRun(DateTime now, RouteRequest request)
        {
            return new Result(false);
        }

        protected Result AutomationNotRun()
        {
            //we need to handle success and failures, so null can represent a non-run
            return null;
        }
        protected Result GetResult(Func<RouteRequest, bool> func, RouteRequest request)
        {
            return GetResult(() => func(request));

        }

        protected Result GetResult(Func<bool> func)
        {
            try
            {
                var result = func();
                return result ? Result.Success() : Result.Fail("Run returned false");

            }
            catch (Exception e)
            {

                return Result.Fail(e);
            }


        }
    }
}
