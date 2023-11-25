using System.IO;

namespace FSO.Content.Framework
{
    public abstract class IContentCodec <T> : IGenericContentCodec
    {
        public T Decode(Stream stream)
        {
            return (T)GenDecode(stream);
        }

        public abstract object GenDecode(Stream stream);
    }

    public interface IGenericContentCodec
    {
        object GenDecode(Stream stream);
    }
}
