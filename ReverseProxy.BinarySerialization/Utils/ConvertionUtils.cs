using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ReverseProxy.BinarySerialization.Utils
{
    public static class ConvertionUtils
    {
        private static readonly Encoding StringEncoding = Encoding.UTF8;

        public static int Convert(object value, byte[] dst)
        {
            Debug.Assert(value != null);
            Debug.Assert(value.GetType().IsValueType);
            Debug.Assert(dst != null);
            Debug.Assert(dst.Length >= Marshal.SizeOf(value));

            unsafe
            {
                fixed(byte* bytes = dst)
                {
                    Marshal.StructureToPtr(value, (IntPtr)bytes, false);
                }

                return Marshal.SizeOf(value);
            }
        }

        public static byte[] GetStringBytes(string value)
        {
            Debug.Assert(value != null);

            return StringEncoding.GetBytes(value);
        }

        public static object GetValue(Type type, byte[] bytes)
        {
            Debug.Assert(type != null);
            Debug.Assert(type.IsValueType);
            Debug.Assert(bytes != null);
            Debug.Assert(bytes.Length >= Marshal.SizeOf(type));


            unsafe
            {
                fixed(byte* data = bytes)
                {
                    return Marshal.PtrToStructure((IntPtr)data, type);
                }
            }
        }

        public static string GetString(byte[] bytes)
        {
            Debug.Assert(bytes != null && bytes.Length > 0);

            return StringEncoding.GetString(bytes);
        }

        public static Array ConvertListToArray(IList list)
        {
            var itemType = TypeUtils.GetEnumerableItemType(list.GetType());
            var array = TypeUtils.CreateArray(itemType, list.Count);
            list.CopyTo(array, 0);

            return array;
        }
    }
}