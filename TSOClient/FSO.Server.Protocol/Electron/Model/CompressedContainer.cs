using FSO.Common.Serialization;
using Mina.Core.Buffer;
using System.IO.Compression;
using System.Text;

namespace FSO.Server.Protocol.Electron.Model
{
    public interface ICompressedContainerItem : IoBufferSerializable, IoBufferDeserializable
    {
    }

    /// <summary>
    /// Container that compresses the data when serializing into an IoBuffer.
    /// The compressed data is cached, so it can be reused across multiple serializations without compressing each time.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class CompressedContainer<T> where T : ICompressedContainerItem
    {
        private byte[] _compressedData;
        private T _item;

        public T Item
        {
            get
            {
                if (_item == null)
                {
                    Decompress();
                }

                return _item;
            }

            set
            {
                _compressedData = null;
                _item = value;
            }
        }

        private void Compress()
        {
            if (_item == null)
            {
                _compressedData = null;
                return;
            }

            var buffer = IoBufferUtils.SerializableToIoBuffer(_item, null);

            var data = buffer.GetBytes();

            using (var dstStream = new MemoryStream())
            {
                using (var cStream = new GZipStream(dstStream, CompressionMode.Compress))
                using (var srcStream = new MemoryStream(data))
                {
                    srcStream.CopyTo(cStream);
                };

                _compressedData = dstStream.ToArray();
            }
        }

        private void Decompress()
        {
            if (_compressedData == null)
            {
                _item = default;
                return;
            }

            using (var compressed = new MemoryStream(_compressedData))
            using (var cStream = new GZipStream(compressed, CompressionMode.Decompress))
            using (var dstStream = new MemoryStream())
            {
                cStream.CopyTo(dstStream);

                var data = dstStream.ToArray();

                _item = IoBufferUtils.Deserialize<T>(data, null);
            }
        }

        public void Deserialize(IoBuffer input, ISerializationContext context)
        {
            int compressedDataSize = input.GetInt32();

            _compressedData = input.GetSlice(compressedDataSize).GetBytes();
        }

        public void Serialize(IoBuffer output, ISerializationContext context)
        {
            Compress();

            output.PutInt32(_compressedData?.Length ?? 0);
            if (_compressedData != null)
            {
                output.Put(_compressedData);
            }
        }
    }
}
