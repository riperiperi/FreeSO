using FSO.SimAntics.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMNetUpdateInventoryCmd : VMNetCommandBodyAbstract
    {
        public List<VMInventoryItem> Items;

        public override bool Execute(VM vm)
        {
            //sent direct to the target, so we should believe the inventory is ours.
            vm.MyInventory = Items;
            return true;
        }

        public override bool Verify(VM vm, VMAvatar caller)
        {
            return !FromNet;
        }

        #region VMSerializable Members

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(Items.Count);
            foreach (var item in Items)
            {
                item.SerializeInto(writer);
            }
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            var count = reader.ReadInt32();
            Items = new List<VMInventoryItem>();
            for (int i=0; i<count; i++)
            {
                var item = new VMInventoryItem();
                item.Deserialize(reader);
                Items.Add(item);
            }
        }

        #endregion
    }

}
