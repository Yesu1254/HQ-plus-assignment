using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;

namespace ApiCore.Managers
{
    public sealed class CustomJsonSerializer : JsonSerializer
    {
        public CustomJsonSerializer()
        {
            //var i = IUnityContainer;
            //ContractResolver = new UnityContractResolver(i);
        }
    }

    public class UnityContractResolver : DefaultContractResolver
    {
        private readonly IUnityContainer _container;

        public UnityContractResolver(IUnityContainer container)
        {
            _container = container;
        }

        protected override JsonObjectContract CreateObjectContract(Type objectType)
        {
            var contract = base.CreateObjectContract(objectType);

            if (objectType.IsInterface && _container.IsRegistered(objectType))
            {
                contract.DefaultCreator = () => _container.Resolve(objectType);
            }

            return contract;
        }
    }


    public static class JsonManager
    {
        private static JsonSerializerSettings deserializeSettings;

        #region Deserialize
        public static T DeserializeFromFile<T>(string path)
        {
            return Deserialize<T>(System.IO.File.ReadAllText(path));
        }

        public static object Deserialize(Type t, string json)
        {
            var jss = GetSerializerSettings();

            var result = JsonConvert.DeserializeObject(json, t, jss);

            foreach (var interf in result.GetType().GetInterfaces())
            {
                //if (interf == t) continue; //needed as unity creates new class
                //if (interf == typeof(IEntity)) continue;
                //if (!typeof(IEntity).IsAssignableFrom(interf)) continue;
                //if (!EntityTypeManager.ContainsType(interf)) continue; //check if the type is registered
                //if (!interf.IsAssignableFrom(typeof(IEntity))) continue;
                var subInterface = JsonConvert.DeserializeObject(json, interf, jss);
                var baseType = result.GetType();
                foreach (var prop in subInterface.GetType().GetProperties())
                {
                    var val = prop.GetValue(subInterface);
                    if (val == null) continue;
                    var baseTypeProp = baseType.GetProperty(prop.Name);
                    baseTypeProp.SetValue(result, val);
                }
            }

            return result;
        }



        public static JsonSerializerSettings GetSerializerSettings()
        {
            if (deserializeSettings == null) deserializeSettings = new JsonSerializerSettings();
            return deserializeSettings;
        }

        public static Object Deserialize(string json)
        {
            var jss = GetSerializerSettings(); //new JsonSerializerSettings { ContractResolver = new UnityContractResolver(UnityManager.container) };
            var result = JsonConvert.DeserializeObject(json, jss);
            return result;
        }

        public static T Deserialize<T>(string json)
        {
            return (T)Deserialize(typeof(T), json);
        }

        #endregion


        #region Serialize
        public static void SerializeToFile(object obj, string path)
        {
            var json = Serialize(obj);
            System.IO.File.WriteAllText(path, json);
        }

        public static string Serialize(object obj, PreserveReferencesHandling preserveReferencesHandling = PreserveReferencesHandling.Objects)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            var jss = new JsonSerializerSettings { PreserveReferencesHandling = preserveReferencesHandling, DateFormatString = "yyyy-MM-ddTHH:mm:ss" };
            return JsonConvert.SerializeObject(obj, jss);
        }

        public static string TrySerialize(object obj, PreserveReferencesHandling preserveReferencesHandling = PreserveReferencesHandling.Objects, bool includeNullFields = true)
        {
            try
            {
                if (obj == null) return "";
                var jss = new JsonSerializerSettings { PreserveReferencesHandling = preserveReferencesHandling, DateFormatString = "yyyy-MM-ddTHH:mm:ss", NullValueHandling = includeNullFields ? NullValueHandling.Include : NullValueHandling.Ignore };
                return JsonConvert.SerializeObject(obj, jss);
            }
            catch (Exception)
            {
                return "";
            }
        }
        #endregion

    }
}
