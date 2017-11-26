using System;
using System.Linq;
using System.Reflection;

namespace ReverseProxy.BinarySerialization.Utils
{
    public static class ReflectionUtils
    {
        public static bool IsAssignableTo<T>(this Type type)
        {
            return type.IsAssignableTo(typeof(T));
        }

        public static bool IsAssignableTo(this Type type, Type targetType)
        {
            return targetType.IsAssignableFrom(type);
        }

        public static PropertyInfo[] GetProperties(this object obj)
        {
            var type = obj.GetType();
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(c => c.CanWrite && c.CanRead).ToArray();
        }
    }
}