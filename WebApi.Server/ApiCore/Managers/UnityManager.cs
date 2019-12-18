using ApiCore.CoreModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Unity;
using Unity.Injection;

namespace ApiCore.Managers
{
    public delegate T Create<T>(Action<T> a = null);
    public delegate T CreateAndSave<T>(Action<T> a = null);
    public delegate T GetCommand<T>() where T : CommandBase;


    [DelegateContainer]
    public static class EntityT<T>
    {
        public static Create<T> Create = Entity.Create;
        public static CreateAndSave<T> CreateAndSave = Entity.CreateAndSave;
    }

    [DelegateContainer]
    public static class UnityManagerContainer<T> where T : CommandBase
    {
        public static GetCommand<T> GetCommand = UnityManager.GetCommand<T>;
    }

    //helper class for creating entities
    public static class Entity
    {
        public static object CreateFromType(Type type) => UnityManager.Create(type);
        public static T Create<T>(Action<T> a = null) => UnityManager.Create(a);
        public static T CreateAndSave<T>(Action<T> a = null)
        {
            var entity = Create<T>(a);
            return entity;
        }
    }
    public class UnityManager
    {
        public static List<string> AssemblyNames { get; set; } = new List<string>() { "ApiCore" };
        public static List<ICommandBase> Commands { get; set; } = new List<ICommandBase>();
        public static IUnityContainer container = new UnityContainer();

        public static T Create<T>(Action<T> a)
        {
            var t = Create<T>();
            a?.Invoke(t);
            return t;
        }

        public static T Create<T>()
        {
            if (!typeof(T).IsInterface || !container.IsRegistered<T>())
            {
                var instance = Activator.CreateInstance(typeof(T));
                return (T)instance;
            }

            var entity = container.Resolve<T>();
            return entity;
        }

        public static object Create(Type iType)
        {
            if (!iType.IsInterface || !container.IsRegistered(iType))
            {
                var instance = Activator.CreateInstance(iType);
                return instance;
            }
            var entity = container.Resolve(iType);
            return entity;
        }

        public static IEnumerable<Type> GetEnumerableOfType<T>()
        {
            var res = GetAllAssemblies().SelectMany(a => a.GetTypes()).Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(T)));
            return res;
        }

        public static Assembly[] GetAllAssemblies()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            var assembliesList = assemblies.ToList().Where(a => AssemblyNames.Any(an => a.FullName.Contains(an))).OrderBy(x => x.FullName.Contains("ApiCore")).ToList();

            var assembliesArray = assembliesList.OrderBy(a => a.FullName.Contains("ApiCore")).ToArray();

            return assembliesArray;
        }

        public static T GetCommand<T>() where T : CommandBase
        {
            foreach (var command in Commands)
            {
                if (command is T)
                {
                    return (T)command;
                }
            }
            return default(T);
        }

        public static void RegisterTypes()
        {
            try
            {
                var modelsAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName.StartsWith("ApiCore")).ToList();

                var modelTypes = modelsAssemblies
                    .SelectMany(a =>
                        a.GetExportedTypes()
                        .ToList()
                    );

                var interfaces = modelTypes.Where(t => t.IsInterface).ToList();

                var badEntities = new List<string>();

                var types = GetEnumerableOfType<CommandBase>();
                foreach (var type in types)
                {
                    try
                    {
                        var c = UnityManager.Create(type);
                        Commands.Add((ICommandBase)c);
                    }
                    catch (Exception x)
                    {

                        throw;
                    }
                }

            }
            catch (Exception ex)
            {
            }

        }
    }
}
