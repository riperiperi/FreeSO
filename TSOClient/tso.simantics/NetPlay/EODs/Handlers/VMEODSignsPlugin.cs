using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.NetPlay.EODs.Handlers
{
    public class VMEODSignsPlugin : VMEODHandler
    {
        //temp 0 contains VMEODSignsMode
        //temp 1 contains max chars

        public VMEODSignsPlugin(VMEODServer server) : base(server)
        {

        }
    }

    public enum VMEODSignsMode
    {
        Erase = 0,
        Write = 1,
        Read = 2,
        OwnerPermissions = 3,
        OwnerWrite = 4
    }

    public enum VMEODSignsEvent
    {
        TurnOnWritingSign = 1, //bool in temp 0 if writing 
        Connect = -2
    }
}
