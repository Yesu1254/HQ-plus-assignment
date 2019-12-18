using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ApiCore.Managers
{
    public class UserUnreadLogs
    {
        public int userId { get; set; }
        public int noOfUnreadLogs { get; set; }

        public string userToken { get; set; }
    }


    public static class ServiceRequestManager
    {
        private static List<string> serviceRequestCommands { get; set; } = new List<string>();

        public static void Setup()
        {
            var commandClasses = AssemblyManager.GetClassesWithAttribute<Command>();

            foreach (var command in commandClasses)
            {
                if (command.Value.handleServiceRequest)
                {
                    var fieldInfo = command.Key.GetField("Run", BindingFlags.Public | BindingFlags.Static);
                    var delegateName = fieldInfo.FieldType.FullName;

                    AddCommand(delegateName);
                }
            }

        }

        public static bool HasCommand(string commandName) => serviceRequestCommands.Any(x => x.ContainsIgnoreCase(commandName));

        private static void AddCommand(string commandName)
        {
            if (HasCommand(commandName)) return;
            serviceRequestCommands.Add(commandName);
        }

        public static List<string> GetAllCommands() => serviceRequestCommands;

    }
}
