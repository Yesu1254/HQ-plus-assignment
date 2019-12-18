using ApiCore.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiCore.CoreModels
{
    public interface ICommandBase
    {
        string CommandName { get; }

        bool BypassAuthentication { get; set; }

        int? CacheMinutes { get; set; }

        Func<Object, bool> CacheBypass { get; set; }

        bool Queued { get; set; }

        bool LogRequests { get; set; }

        int MaxSimultaneousExecutions { get; set; }

        //bool IsRunning { get; set; }

        //RouteRequest Request { get; set; }

        Result AutomationRun(DateTime now, RouteRequest request);

        //void RegisterSecuritySettings();

        //void RegisterSecuritySetting(string settingName, string name, int id = 0);

        //bool IsAllowed(string settingName);

        //Task CurrentTask { get; set; }


    }
}
