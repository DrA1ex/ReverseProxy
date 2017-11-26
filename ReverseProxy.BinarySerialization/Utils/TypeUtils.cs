using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ReverseProxy.BinarySerialization.Utils
{
    public static class TypeUtils
    {
        public static ObjectType DetermineObjectType(Type type)
        {
            if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return ObjectType.Nullable;
            }
            if(type.IsPrimitive || type == typeof(decimal) || type == typeof(TimeSpan))
            {
                return ObjectType.Primitive;
            }
            if(type == typeof(string))
            {
                return ObjectType.String;
            }
            if(type == typeof(DateTime))
            {
                return ObjectType.DateTime;
            }
            if(type.IsAssignableTo<IEnumerable>())
            {
                return ObjectType.Enumerable;
            }
            if(type.IsClass)
            {
                return ObjectType.Class;
            }
            if(type.IsEnum)
            {
                return ObjectType.Enum;
            }
            if(type.IsValueType)
            {
                return ObjectType.Struct;
            }

            return ObjectType.Unsupported;
        }

        public static Type GetEnumerableItemType(Type type)
        {
            var elementType = type.GetElementType();

            if(elementType == null) //non-array
            {
                var genericType = type.IsGenericType ? type.GetGenericTypeDefinition() : null;
                if(genericType == typeof(IList<>) || genericType == typeof(List<>))
                {
                    elementType = type.GetGenericArguments().First();
                }
            }

            return elementType;
        }

        public static bool IsSupportedElementType(Type elementType)
        {
            return elementType != typeof(object)
                                       && !elementType.IsAbstract
                                       && !elementType.IsInterface;
        }

        public static IList CreateList(Type itemType)
        {
            var collectionType = typeof(List<>).MakeGenericType(itemType);
            return (IList)Activator.CreateInstance(collectionType);
        }

        public static Array CreateArray(Type itemType, int length)
        {
            return Array.CreateInstance(itemType, length);
        }
    }
}