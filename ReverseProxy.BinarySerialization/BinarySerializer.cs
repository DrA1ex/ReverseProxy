using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ReverseProxy.BinarySerialization.Readers;
using ReverseProxy.BinarySerialization.Utils;
using ReverseProxy.BinarySerialization.Writers;

namespace ReverseProxy.BinarySerialization
{
    public enum BinarySerializationMethod
    {
        UnsafeSerialization
    }

    public class BinarySerializer
    {
        private readonly IBinaryReader _reader;
        private readonly IBinaryWriter _writer;

        public BinarySerializer(BinarySerializationMethod serializationMethod)
        {
            switch(serializationMethod)
            {
                case BinarySerializationMethod.UnsafeSerialization:
                    _writer = new UnsafeBinaryWriter();
                    _reader = new UnsafeBinaryReader();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(serializationMethod));
            }
        }

        public async Task<MemoryStream> Serialize(object obj)
        {
            var stream = new MemoryStream();
            await Serialize(obj, stream)
                .ConfigureAwait(false);

            return stream;
        }

        public async Task Serialize(object obj, Stream stream)
        {
            var typeFullName = obj.GetType().AssemblyQualifiedName;

            byte[] typeFullNameBytes;
            using(var md5 = MD5.Create())
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                typeFullNameBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(typeFullName));
            }

            TraceUtils.WriteLineFormatted("Write object type...");
            await _writer.WriteObject(typeFullNameBytes, stream).ConfigureAwait(false);
            TraceUtils.WriteLineFormatted("Write object...");
            await _writer.WriteObject(obj, stream).ConfigureAwait(false);
        }

        public async Task<T> Deserialize<T>(Stream stream)
        {
            var result = await Deserialize(typeof(T), stream).ConfigureAwait(false);
            return (T)result;
        }

        public async Task<object> Deserialize(Type objectType, Stream stream)
        {
            var targetTypeFullName = objectType.AssemblyQualifiedName;
            TraceUtils.WriteLineFormatted("Read object type...");
            var typeFullNameBytes = (byte[])await _reader.ReadObject(typeof(byte[]), stream).ConfigureAwait(false);

            byte[] targetTypeFullNameBytes;
            using(var md5 = MD5.Create())
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                targetTypeFullNameBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(targetTypeFullName));
            }

            if(targetTypeFullNameBytes.SequenceEqual(typeFullNameBytes))
            {
                TraceUtils.WriteLineFormatted("Read object...");
                return await _reader.ReadObject(objectType, stream);
            }

            var targetTypeBinaryString = string.Join("", targetTypeFullNameBytes.Select(x => Convert.ToString(x, 16).PadLeft(2, '0')));
            var typeBinaryString = string.Join("", typeFullNameBytes.Select(x => Convert.ToString(x, 16).PadLeft(2, '0')));

            throw new ArgumentException($"Unable to deserialize object: Wrong type hash \"{typeBinaryString}\" expected \"{targetTypeBinaryString}\"");
        }
    }
}