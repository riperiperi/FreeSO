using FSO.Server.Common;

namespace FSO.Server.Framework.Aries
{
    public interface IAriesSession : ISocketSession
    {
        bool IsAuthenticated { get; }
        uint LastRecv { get; set; }
        bool Connected { get; }

        void Write(params object[] messages);
        void Close();

        object GetAttribute(string key);
        void SetAttribute(string key, object value);
    }
}
