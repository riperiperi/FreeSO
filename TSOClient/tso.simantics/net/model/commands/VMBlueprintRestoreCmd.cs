using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using tso.world.model;
using TSO.Simantics.utils;

namespace TSO.Simantics.net.model.commands
{
    public class VMBlueprintRestoreCmd : VMNetCommandBodyAbstract
    {
        public byte[] XMLData;

        public override bool Execute(VM vm)
        {
            XmlHouseData lotInfo;
            using (var stream = new MemoryStream(XMLData))
            {
                lotInfo = XmlHouseData.Parse(stream);
            }

            var activator = new VMWorldActivator(vm, vm.Context.World);
            var blueprint = activator.LoadFromXML(lotInfo);

            vm.Context.World.InitBlueprint(blueprint);
            vm.Context.Blueprint = blueprint;

            return true;
        }

        #region VMSerializable Members

        public override void SerializeInto(BinaryWriter writer)
        {
            if (XMLData == null) writer.Write(0);
            else
            {
                writer.Write(XMLData.Length);
                writer.Write(XMLData);
            }
        }

        public override void Deserialize(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            XMLData = reader.ReadBytes(length);
        }

        #endregion
    }
}
