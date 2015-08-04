using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using tso.world.model;
using TSO.Simantics.model;

namespace TSO.Simantics.net.model.commands
{
    public class VMNetDeleteObjectCmd : VMNetCommandBodyAbstract
    {
        public short ObjectID;
        public bool CleanupAll;

        public override bool Execute(VM vm)
        {
            VMEntity obj = vm.GetObjectById(ObjectID);
            if (obj == null || (obj is VMAvatar)) return false;
            obj.Delete(CleanupAll, vm.Context);
            return true;
        }

        #region VMSerializable Members

        public override void SerializeInto(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(CleanupAll);
        }

        public override void Deserialize(BinaryReader reader)
        {
            ObjectID = reader.ReadInt16();
            CleanupAll = reader.ReadBoolean();
        }

        #endregion
    }
}
