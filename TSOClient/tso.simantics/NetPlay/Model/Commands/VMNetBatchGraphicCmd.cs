using System.IO;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMNetBatchGraphicCmd : VMNetCommandBodyAbstract
    {
        public short[] Objects;
        public byte[] Graphics;

        public override bool Execute(VM vm)
        {
            for (int i=0; i<Objects.Length; i++)
            {
                var obj = vm.GetObjectById(Objects[i]);
                if (obj != null && obj is VMGameObject)
                {
                    var g = Graphics[i];
                    if (g < 255)
                        obj.SetValue(SimAntics.Model.VMStackObjectVariable.Graphic, g);
                    else
                        obj.SetValue(SimAntics.Model.VMStackObjectVariable.Hidden, 1);
                    ((VMGameObject)obj).RefreshGraphic();
                }
            }
            return true;
        }

        public override bool AcceptFromClient
        {
            get
            {
                return false;
            }
        }

        public override bool Verify(VM vm, VMAvatar caller)
        {
            return base.Verify(vm, caller);
        }

        #region VMSerializable Members

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(Objects.Length);
            writer.Write(VMSerializableUtils.ToByteArray(Objects));
            writer.Write(Graphics);

        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            var length = reader.ReadInt32();
            Objects = VMSerializableUtils.ToTArray<short>(reader.ReadBytes(length * 2));
            Graphics = reader.ReadBytes(length);
        }

        #endregion
    }
}
