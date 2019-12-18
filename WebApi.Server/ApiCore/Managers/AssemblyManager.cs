using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ApiCore.Managers
{
    public static class AssemblyManager
    {

        private static Dictionary<Type, object> cache { get; set; } = new Dictionary<Type, object>();


        public static Dictionary<Type, T> GetClassesWithAttribute<T>()
        {
            //test caching first
            cache.TryGetValue(typeof(T), out object cachedResult);
            if (cachedResult != null)
            {
                return (Dictionary<Type, T>)cachedResult;
            }


            var allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            var currentAssemblies = new List<Assembly>();

            var assemblyNames = new List<string>() { "ApiCore"};

            foreach (var asm in allAssemblies)
            {
                foreach (var asmName in assemblyNames)
                {
                    if (asm.FullName.StartsWith(asmName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        currentAssemblies.Add(asm);
                        break;
                    }
                }
            }

            var classTypes =
                            from a in currentAssemblies
                            from t in a.GetTypes()
                            let attributes = t.GetCustomAttributes(typeof(T), true)
                            where attributes != null && attributes.Length > 0
                            select new { type = t, attribute = attributes.Cast<T>().FirstOrDefault() };

            var dic = new Dictionary<Type, T>();

            foreach (var emt in classTypes)
            {
                dic.Add(emt.type, emt.attribute);
            }

            return dic;
        }

    }
}
