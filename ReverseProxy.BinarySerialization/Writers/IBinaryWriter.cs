using System.IO;
using System.Threading.Tasks;

namespace ReverseProxy.BinarySerialization.Writers
{
    public interface IBinaryWriter
    {
        Task WriteObject(object obj, Stream stream);
    }
}
