using FSO.LotView.Components;
using FSO.LotView.Components.Model;
using System.IO;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    internal class VMNetSM64PositionCmd : VMNetCommandBodyAbstract
    {
        public SM64VisualState VisualState;

        public override bool Execute(VM vm, VMAvatar caller)
        {
            // Tell the SM64 component about this sim's mario instance.
            if (caller == null || caller.WorldUI == null || !(caller.WorldUI is AvatarComponent)) return false;

            if (caller.PersistID != vm.MyUID)
            {
                vm.Context.Blueprint.SM64?.UpdateOtherMario((AvatarComponent)caller.WorldUI, VisualState);
            }

            return true;
        }

        #region VMSerializable Members

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);

            writer.Write(VisualState.Active);
            writer.Write(VisualState.GlobalAnimTimer);
            writer.Write(VisualState.PosX);
            writer.Write(VisualState.PosY);
            writer.Write(VisualState.PosZ);
            writer.Write(VisualState.ScaleX);
            writer.Write(VisualState.ScaleY);
            writer.Write(VisualState.ScaleZ);
            writer.Write(VisualState.AngleX);
            writer.Write(VisualState.AngleY);
            writer.Write(VisualState.AngleZ);
            writer.Write(VisualState.AnimID);
            writer.Write(VisualState.AnimYTrans);
            writer.Write(VisualState.AnimFrame);
            writer.Write(VisualState.AnimTimer);
            writer.Write(VisualState.AnimFrameAccelAssist);
            writer.Write(VisualState.AnimAccel);
    }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);

            VisualState = new SM64VisualState();

            VisualState.Active = reader.ReadBoolean();
            VisualState.GlobalAnimTimer = reader.ReadInt32();
            VisualState.PosX = reader.ReadSingle();
            VisualState.PosY = reader.ReadSingle();
            VisualState.PosZ = reader.ReadSingle();
            VisualState.ScaleX = reader.ReadSingle();
            VisualState.ScaleY = reader.ReadSingle();
            VisualState.ScaleZ = reader.ReadSingle();
            VisualState.AngleX = reader.ReadInt16();
            VisualState.AngleY = reader.ReadInt16();
            VisualState.AngleZ = reader.ReadInt16();
            VisualState.AnimID = reader.ReadInt16();
            VisualState.AnimYTrans = reader.ReadInt16();
            VisualState.AnimFrame = reader.ReadInt16();
            VisualState.AnimTimer = reader.ReadUInt16();
            VisualState.AnimFrameAccelAssist = reader.ReadInt32();
            VisualState.AnimAccel = reader.ReadInt32();
        }

        #endregion
    }
}
