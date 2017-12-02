using System;
using System.Collections.Generic;
using System.Linq;

namespace ReverseProxy.Test.Utils
{
    internal static class EnumerableUtils
    {
        public static int FindIndex<T>(this IEnumerable<T> collection, Func<T, bool> predicate)
        {
            var index = 0;
            foreach(var item in collection)
            {
                if(predicate(item))
                {
                    return index;
                }

                ++index;
            }

            return -1;
        }

        public static IEnumerable<(T, T)> JoinByIndex<T>(this IEnumerable<T> collection, T[] other)
        {
            return collection.Select((serverPacket, index) => (serverPacket, other[index]));
        }
    }
}