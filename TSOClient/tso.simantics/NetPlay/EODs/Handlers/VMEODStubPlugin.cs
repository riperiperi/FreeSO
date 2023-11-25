namespace FSO.SimAntics.NetPlay.EODs.Handlers
{
    public class VMEODStubPlugin : VMEODHandler
    {
        public VMEODStubPlugin(VMEODServer server) : base(server)
        {
            
        }

        public override void OnConnection(VMEODClient client)
        {
            //immediately disconnect
            Server.Disconnect(client);
        }
    }
}
