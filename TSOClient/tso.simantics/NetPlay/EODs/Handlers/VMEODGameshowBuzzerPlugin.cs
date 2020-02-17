using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.NetPlay.EODs.Handlers
{
    public class VMEODGameshowBuzzerPlugin : VMEODHandler
    {
        public VMEODGameshowBuzzerPlugin(VMEODServer server) : base(server)
        {
            
        }
        public override void OnConnection(VMEODClient client)
        {
            client.Send("Buzzer_Host_Show", new byte[] { 0 });
            base.OnConnection(client);
        }
    }
}
