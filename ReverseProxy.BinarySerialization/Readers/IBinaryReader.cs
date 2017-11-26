using System;
using System.IO;
using System.Threading.Tasks;

namespace ReverseProxy.BinarySerialization.Readers
{
    public interface IBinaryReader
    {
        Task<object> ReadObject(Type type, Stream stream);
    }
}
