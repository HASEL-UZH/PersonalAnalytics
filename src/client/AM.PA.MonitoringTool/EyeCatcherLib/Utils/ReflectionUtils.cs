using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EyeCatcherLib.Utils
{
    internal static class ReflectionUtils
    {
        public static IList<T> GetAndActivateInstances<T>() where T : class
        {
            var list = new List<T>();
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var t in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (t.BaseType == typeof(T) && t.GetConstructor(Type.EmptyTypes) != null)
                {
                    list.Add((T)Activator.CreateInstance(t));
                }
            }
            return list;
        }

        public static List<TInterface> GetAndActivateInterfaceInstances<TInterface>() where TInterface : class
        {
            var list = new List<TInterface>();
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var t in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (t.GetInterfaces().Contains(typeof(TInterface)) && t.GetConstructor(Type.EmptyTypes) != null)
                {
                    list.Add((TInterface) Activator.CreateInstance(t));
                }
            }
            return list;
        }
    }
}
