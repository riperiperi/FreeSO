using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.NetPlay.EODs
{
    /*
        just text dumping here to make it obvious how this should work

        When Invoke Plugin is called, the calling thread blocks in the format of an animation.
        The plugin returns EVENTS (short code, short[] dataForTemps) on the false branch.
        These events obviously queue up, multiple per frame and have priority over the final "close".
         - these MUST be synchronised across clients (using commands)! all other comms (UI) are done async

        Plugins can either run on an object, with connections (Thread, ObjectID) (joinable, objects share same plugin)
        ...or run just on their thread alone. (Thread)
        AvatarID specifies who sees the UI and "connects" to the plugin. This can be 0. (Dance Floor Controller connects to itself to get events)

        ID for non-joinable EODs should be (ThreadOwnerID). For joinable, the ID should be (ObjectID).
    */
    public class VMEODServer
    {

    }
}
