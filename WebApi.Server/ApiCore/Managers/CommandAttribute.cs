using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiCore.Managers
{
    public class Command : Attribute
    {
        public bool bypassAuthentication = false;
        public int cacheMinutes = 0;
        public bool logRequests = false;
        public int maxSimultaneousExecutions = 20;
        public bool handleServiceRequest = false;
        public string alias = "";

        public Command()
        {
        }

        //public CommandAttribute(bool bypassAuthentication)
        //{
        //}

        //public CommandAttribute(int cacheMinutes)
        //{
        //}


        public Command(bool bypassAuthentication = false, int cacheMinutes = 0, bool logRequests = false, int maxSimultaneousExecutions = 20, bool handleServiceRequest = false, string alias = "")
        {
            this.bypassAuthentication = bypassAuthentication;
            this.cacheMinutes = cacheMinutes;
            this.logRequests = logRequests;
            this.maxSimultaneousExecutions = maxSimultaneousExecutions;
            this.handleServiceRequest = handleServiceRequest;
            this.alias = alias;
        }
    }
}
