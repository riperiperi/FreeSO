using FSO.Server.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Framework.Aries
{
    public interface IAriesSession : ISocketSession
    {
        bool IsAuthenticated { get; }
        
        void Write(params object[] messages);
        void Close();
    }
}
