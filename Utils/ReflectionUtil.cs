using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace CommonLib.Utils
{
    public static class ReflectionUtil
    {
        public static Type[] GetTypesWithAttribute(Type attributeType)
        {
            var types = new List<Type>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                try
                {
                    var assemblyTypes = assembly.GetExportedTypes();
                    foreach (var type in assemblyTypes)
                    {
                        if (Attribute.IsDefined(type, attributeType))
                        {
                            types.Add(type);
                        }
                    }
                }
                catch (FileNotFoundException) { }
            }
            return types.ToArray();
        }

        public static Type[] GetTypesWithAttribute<T>()
        {
            return GetTypesWithAttribute(typeof(T));
        }

        public static T? GetAttribute<T>(this Type type) where T : Attribute
        {
            return (T)type.GetCustomAttribute(typeof(T), true);
        }

        public static T? GetAttribute<T>(this PropertyInfo prop) where T : Attribute
        {
            return (T)Attribute.GetCustomAttribute(prop, typeof(T));
        }
    }
}
