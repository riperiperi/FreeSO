using System.IO;

namespace FSO.SimAntics.NetPlay.Model
{
    public abstract class VMNetCommandBodyAbstract : VMSerializable
    {
        public uint ActorUID;
        public bool FromNet = false;

        public virtual bool AcceptFromClient
        {
            get { return true; }
        }

        public virtual bool Execute(VM vm) { return true; }

        public virtual bool Execute(VM vm, VMAvatar caller) { return Execute(vm); }

        public virtual void Deserialize(BinaryReader reader) {
            FromNet = true;
            ActorUID = reader.ReadUInt32();
        }
        public virtual void SerializeInto(BinaryWriter writer) {
            writer.Write(ActorUID);
        }

        //verifies commands sent by clients before running and forwarding them.
        //if "Verify" returns true, the server runs the command and it is sent to clients
        //this prevents forwarding bogus requests - though some verifications are performed as the command is sequenced.
        //certain commands like "StateSyncCommand" cannot be forwarded from clients.

        //note - that returning false from here will only prevent the command from being forwarded IMMEDIATELY.
        //Architecture and Buy Object commands perform asynchronous transactions and then resend their command on success later.

        //verify is not run on clients.
        public virtual bool Verify(VM vm, VMAvatar caller)
        {
            return true;
        }
    }
}
