using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ReverseProxy.BinarySerialization.Utils;

namespace ReverseProxy.BinarySerialization.Writers
{
    public sealed class UnsafeBinaryWriter : IBinaryWriter
    {
        private byte[] _temporaryBuffer;

        public byte[] TemporaryBuffer => _temporaryBuffer ?? (_temporaryBuffer = new byte[sizeof(decimal)]);

        public async Task WriteObject(object obj, Stream stream)
        {
            Debug.Assert(obj != null);
            Debug.Assert(stream != null);

            TraceUtils.WriteLineFormatted("*** BEGIN WRITING ***");

            await WriteObjectInternal(obj, stream).ConfigureAwait(false);

            TraceUtils.WriteLineFormatted("*** END WRITING ***");
        }

        private async Task WriteObjectInternal(object obj, Stream stream)
        {
            Debug.Assert(obj != null);

            var type = obj.GetType();
            if(type != typeof(object))
            {
                var objectType = TypeUtils.DetermineObjectType(type);
                if(objectType == ObjectType.Class || objectType == ObjectType.Struct)
                {
                    await WriteCompositeObject(obj, type, stream).ConfigureAwait(false);
                }
                else
                {
                    TraceUtils.WriteLineFormatted("Writing plain object of type: {0}", type.FullName);
                    await WritePlainObject(obj, type, stream).ConfigureAwait(false);
                }
            }
            else
            {
                TraceUtils.WriteLineFormatted("Unable to write object of type \"{0}\": Unsupported", type.FullName);
            }
        }

        private async Task WriteCompositeObject(object obj, Type type, Stream stream)
        {
            TraceUtils.WriteLineFormatted("Writing composite object of type: {0}", type.FullName);

            var props = obj.GetProperties();

            if(props.Any())
            {
                foreach(var prop in props)
                {
                    var value = prop.GetValue(obj);

                    TraceUtils.WriteLineFormatted("Writing sub-object of property {0} with type: {1}", prop.Name, prop.PropertyType.FullName);
                    await WritePlainObject(value, prop.PropertyType, stream).ConfigureAwait(false);
                }
            }
            else
            {
                TraceUtils.WriteLineFormatted("Composite object hasn't any read\\write properties");
            }
        }

        private async Task WritePlainObject(object value, Type type, Stream stream)
        {
            Debug.Assert(type != null);
            Debug.Assert(stream != null);

            if(value != null)
            {
                var objectType = TypeUtils.DetermineObjectType(type);

                switch(objectType)
                {
                    case ObjectType.Primitive:
                        await WritePrimitive(value, stream).ConfigureAwait(false);
                        break;
                    case ObjectType.Nullable:
                        await WriteNullableValueType(value, stream).ConfigureAwait(false);
                        break;
                    case ObjectType.String:
                        await WriteString((string)value, stream).ConfigureAwait(false);
                        break;
                    case ObjectType.DateTime:
                        await WriteDateTime((DateTime)value, stream).ConfigureAwait(false);
                        break;
                    case ObjectType.Class:
                        await WriteClass(value, stream).ConfigureAwait(false);
                        break;
                    case ObjectType.Struct:
                        await WriteStruct(value, stream).ConfigureAwait(false);
                        break;
                    case ObjectType.Enumerable:
                        if(value is byte[] bytes)
                        {
                            await WriteByteArray(bytes, stream);
                        }
                        else
                        {
                            await WriteEnumerable(value, type, stream).ConfigureAwait(false);
                        }
                        break;
                    case ObjectType.Enum:
                        await WriteEnum(value, stream).ConfigureAwait(false);
                        break;
                    case ObjectType.Unsupported:
                        TraceUtils.WriteLineFormatted("Unsupported value of type: \"{0}\"", type.FullName);
                        break;
                }
            }
            else
            {
                await WriteNullFlag(true, stream).ConfigureAwait(false);
            }
        }

        private async Task WriteByteArray(byte[] data, Stream stream)
        {
            await WriteNullFlag(false, stream);
            await WritePrimitive(data.Length, stream);
            await WriteBytes(data, data.Length, stream).ConfigureAwait(false);
        }

        private Task WritePrimitive(object value, Stream stream)
        {
            var affected = ConvertionUtils.Convert(value, TemporaryBuffer);
            return WriteBytes(TemporaryBuffer, affected, stream);
        }

        private async Task WriteNullableValueType(object value, Stream stream)
        {
            var valueType = value.GetType();

            await WriteNullFlag(false, stream).ConfigureAwait(false);
            await WritePlainObject(value, valueType, stream).ConfigureAwait(false);
        }

        private async Task WriteString(string value, Stream stream)
        {
            var stringLength = value.Length;
            await WriteNullFlag(false, stream).ConfigureAwait(false);


            if(stringLength > 0)
            {
                var stringBytes = ConvertionUtils.GetStringBytes(value);
                await WritePrimitive(stringBytes.Length, stream).ConfigureAwait(false);
                await WriteBytes(stringBytes, stringBytes.Length, stream).ConfigureAwait(false);
            }
            else
            {
                await WritePrimitive(0, stream).ConfigureAwait(false);
            }
        }

        private Task WriteDateTime(DateTime value, Stream stream)
        {
            return WritePrimitive(value.Ticks, stream);
        }

        private async Task WriteEnumerable(object value, Type type, Stream stream)
        {
            var elementType = TypeUtils.GetEnumerableItemType(type);

            var supportedElementType = true;
            if(elementType != null)
            {
                supportedElementType = TypeUtils.IsSupportedElementType(elementType);
            }

            if(elementType != null && supportedElementType)
            {
                var enumerable = (IEnumerable)value;

                // ReSharper disable PossibleMultipleEnumeration
                var supportedEnumerable = enumerable.Cast<object>().All(item => item == null || item.GetType() == elementType);

                if(supportedEnumerable)
                {
                    TraceUtils.WriteLineFormatted("Begin writing enumerable");
                    await WriteNullFlag(false, stream).ConfigureAwait(false);

                    using(var internalStream = new MemoryStream())
                    {
                        var count = 0;
                        foreach(var item in enumerable)
                        {
                            await WritePlainObject(item, elementType, internalStream).ConfigureAwait(false);
                            ++count;
                        }

                        await WriteBytes(TemporaryBuffer, ConvertionUtils.Convert(count, TemporaryBuffer), stream).ConfigureAwait(false);

                        if(count > 0)
                        {
                            internalStream.Seek(0, SeekOrigin.Begin);
                            await internalStream.CopyToAsync(stream).ConfigureAwait(false);
                        }
                    }
                    // ReSharper restore PossibleMultipleEnumeration

                    TraceUtils.WriteLineFormatted("End writing enumerable");
                }
                else
                {
                    await WriteNullFlag(true, stream).ConfigureAwait(false);
                    TraceUtils.WriteLineFormatted("Unable to write Enumerable of type \"{0}\": All elements must be of one type", type);
                }
            }
            else if(elementType != null)
            {
                TraceUtils.WriteLineFormatted("Unable to write Enumerable of type \"{0}\": Unsupported element type \"{1}\"", type, elementType);
            }
            else
            {
                TraceUtils.WriteLineFormatted("Unable to write Enumerable of type \"{0}\": Unsupported", type);
            }
        }

        private Task WriteEnum(object value, Stream stream)
        {
            var targetType = Enum.GetUnderlyingType(value.GetType());
            return WritePrimitive(Convert.ChangeType(value, targetType), stream);
        }

        private async Task WriteClass(object value, Stream stream)
        {
            await WriteNullFlag(false, stream).ConfigureAwait(false);
            await WriteObjectInternal(value, stream).ConfigureAwait(false);
        }

        private Task WriteStruct(object value, Stream stream)
        {
            return WriteObjectInternal(value, stream);
        }

        private Task WriteNullFlag(bool isNull, Stream stream)
        {
            TemporaryBuffer[0] = (byte)(isNull ? 0 : 1);
            return stream.WriteAsync(TemporaryBuffer, 0, 1);
        }

        private Task WriteBytes(byte[] bytes, int length, Stream stream)
        {
            Debug.Assert(length <= bytes.Length);
            TraceUtils.WriteLineFormatted("Written {0} bytes", length);

            return stream.WriteAsync(bytes, 0, length);
        }
    }
}