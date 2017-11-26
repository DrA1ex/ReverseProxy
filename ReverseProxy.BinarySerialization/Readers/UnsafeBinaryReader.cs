using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using ReverseProxy.BinarySerialization.Utils;

namespace ReverseProxy.BinarySerialization.Readers
{
    public class UnsafeBinaryReader : IBinaryReader
    {
        private byte[] _temporaryBuffer;

        public byte[] TemporaryBuffer => _temporaryBuffer ?? (_temporaryBuffer = new byte[sizeof(decimal)]);

        public async Task<object> ReadObject(Type type, Stream stream)
        {
            Debug.Assert(stream != null);

            TraceUtils.WriteLineFormatted("*** BEGIN READING ***");
            try
            {
                return await ReadObjectInternal(type, stream).ConfigureAwait(false);
            }
            finally
            {
                TraceUtils.WriteLineFormatted("*** END READING ***");
            }
        }

        private async Task<object> ReadObjectInternal(Type type, Stream stream)
        {
            if(type != typeof(object))
            {
                object resultObject;

                var objectType = TypeUtils.DetermineObjectType(type);
                if(objectType == ObjectType.Class || objectType == ObjectType.Struct)
                {
                    resultObject = await ReadCompositeObject(type, stream);
                }
                else
                {
                    TraceUtils.WriteLineFormatted("Reading plain object of type: {0}", type.FullName);
                    resultObject = await ReadPlainObject(type, stream);
                }

                return resultObject;
            }

            TraceUtils.WriteLineFormatted("Unable to read object of type \"{0}\": Unsupported", type.FullName);
            return null;
        }

        private async Task<object> ReadCompositeObject(Type type, Stream stream)
        {
            TraceUtils.WriteLineFormatted("Reading composite object of type: {0}", type.FullName);
            var resultingObject = Activator.CreateInstance(type);

            var props = resultingObject.GetProperties();

            if(props.Any())
            {
                foreach(var prop in props)
                {
                    TraceUtils.WriteLineFormatted("Reading sub-object of property {0} with type: {1}", prop.Name, prop.PropertyType.FullName);
                    var value = await ReadPlainObject(prop.PropertyType, stream).ConfigureAwait(false);
                    prop.SetValue(resultingObject, value);
                }
            }
            else
            {
                TraceUtils.WriteLineFormatted("Composite object hasn't any read\\write properties");
            }

            return resultingObject;
        }

        private async Task<object> ReadPlainObject(Type type, Stream stream)
        {
            Debug.Assert(type != null);
            Debug.Assert(stream != null);

            object result = null;

            var objectType = TypeUtils.DetermineObjectType(type);
            switch(objectType)
            {
                case ObjectType.Primitive:
                    result = await ReadPrimitive(type, stream);
                    break;
                case ObjectType.Nullable:
                    result = await ReadNullable(type, stream);
                    break;
                case ObjectType.String:
                    result = await ReadString(stream);
                    break;
                case ObjectType.DateTime:
                    result = await ReadDateTime(stream);
                    break;
                case ObjectType.Class:
                    result = await ReadClass(type, stream);
                    break;
                case ObjectType.Struct:
                    result = await ReadStruct(type, stream);
                    break;
                case ObjectType.Enumerable:
                    if(type.IsEquivalentTo(typeof(byte[])))
                    {
                        result = await ReadByteArray(stream);
                    }
                    else
                    {
                        result = await ReadEnumerable(type, stream);
                    }
                    break;
                case ObjectType.Enum:
                    result = await ReadEnum(type, stream);
                    break;
                case ObjectType.Unsupported:
                    TraceUtils.WriteLineFormatted("Unsupported value of type: \"{0}\"", type.FullName);
                    result = Task.FromResult<object>(null);
                    break;
            }

            return result;
        }

        protected async Task<byte[]> ReadByteArray(Stream stream)
        {
            var isNull = await ReadNullFlag(stream);
            if(!isNull)
            {
                var length = (int)await ReadPrimitive(typeof(int), stream);
                if(length > 0)
                {
                    var data = new byte[length];
                    await ReadStream(stream, data, length);

                    return data;
                }

                return new byte[0];
            }

            return null;
        }

        private async Task<object> ReadPrimitive(Type type, Stream stream)
        {
            var bytesToRead = Marshal.SizeOf(type);
            await ReadStream(stream, TemporaryBuffer, bytesToRead).ConfigureAwait(false);

            return ConvertionUtils.GetValue(type, TemporaryBuffer);
        }

        private async Task<object> ReadNullable(Type type, Stream stream)
        {
            var isNull = await ReadNullFlag(stream).ConfigureAwait(false);
            if(!isNull)
            {
                return await ReadPlainObject(Nullable.GetUnderlyingType(type), stream).ConfigureAwait(false);
            }

            return null;
        }

        private async Task<object> ReadString(Stream stream)
        {
            var isNull = await ReadNullFlag(stream).ConfigureAwait(false);
            if(!isNull)
            {
                var length = (int)await ReadPrimitive(typeof(int), stream).ConfigureAwait(false);
                if(length > 0)
                {
                    var bytes = new byte[length];
                    await ReadStream(stream, bytes, length).ConfigureAwait(false);
                    return ConvertionUtils.GetString(bytes);
                }

                return string.Empty;
            }

            return null;
        }

        private async Task<object> ReadDateTime(Stream stream)
        {
            var ticks = (long)await ReadPrimitive(typeof(long), stream).ConfigureAwait(false);
            return new DateTime(ticks);
        }

        private async Task<object> ReadEnumerable(Type type, Stream stream)
        {
            var elementType = TypeUtils.GetEnumerableItemType(type);

            var supportedElementType = true;
            if(elementType != null)
            {
                supportedElementType = TypeUtils.IsSupportedElementType(elementType);
            }

            if(elementType != null && supportedElementType)
            {
                var isNull = await ReadNullFlag(stream).ConfigureAwait(false);

                if(!isNull)
                {
                    var result = TypeUtils.CreateList(elementType);

                    var count = (int)await ReadPrimitive(typeof(int), stream).ConfigureAwait(false);

                    if(count > 0)
                    {
                        TraceUtils.WriteLineFormatted("Begin reading enumerable");

                        for(var i = 0; i < count; i++)
                        {
                            var item = await ReadPlainObject(elementType, stream).ConfigureAwait(false);
                            result.Add(item);
                            TraceUtils.WriteLineFormatted("Read #{0}/{1} of enumerable", i + 1, count);
                        }

                        TraceUtils.WriteLineFormatted("End reading enumerable");
                    }
                    else
                    {
                        TraceUtils.WriteLineFormatted("Enumerable is empty");
                    }

                    if(!type.IsArray)
                    {
                        return result;
                    }

                    return ConvertionUtils.ConvertListToArray(result);
                }
            }
            else if(elementType != null)
            {
                TraceUtils.WriteLineFormatted("Unable to read Enumerable of type \"{0}\": Unsupported element type \"{1}\"", type, elementType);
            }
            else
            {
                TraceUtils.WriteLineFormatted("Unable to read Enumerable of type \"{0}\": Unsupported", type);
            }


            return null;
        }

        private async Task<object> ReadClass(Type type, Stream stream)
        {
            var isNull = await ReadNullFlag(stream).ConfigureAwait(false);
            if(!isNull)
            {
                return ReadObjectInternal(type, stream);
            }

            return null;
        }

        private Task<object> ReadStruct(Type type, Stream stream)
        {
            return ReadObjectInternal(type, stream);
        }

        private Task<object> ReadEnum(Type type, Stream stream)
        {
            return ReadPrimitive(Enum.GetUnderlyingType(type), stream);
        }

        private async Task<bool> ReadNullFlag(Stream stream)
        {
            await ReadStream(stream, TemporaryBuffer, 1).ConfigureAwait(false);
            return TemporaryBuffer[0] == 0;
        }

        private async Task ReadStream(Stream stream, byte[] dst, int length)
        {
            TraceUtils.WriteLineFormatted("Read from stream {0}", length);

            int readBytes = 0, lastRead;

            do
            {
                lastRead = await stream.ReadAsync(dst, readBytes, length - readBytes);
                readBytes += lastRead;
            } while(readBytes < length && lastRead != 0);

            if(readBytes != length)
            {
                throw new SerializationException($"Unable to read data from stream. Except {length} but found only {readBytes} bytes");
            }
        }
    }
}