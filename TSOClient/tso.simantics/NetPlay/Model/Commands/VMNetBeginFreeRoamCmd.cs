using FSO.Common.Model;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMNetBeginFreeRoamCmd : VMNetCommandBodyAbstract
    {
        public uint AvatarPID;
        public uint TargetLot;
        public LotTransitionInfo Transition;

        public override bool Execute(VM vm)
        {
            // Begin the leave process on the avatar.
            // Change the direct control frame so that it can't exit until the followup message arrives.

            if (!vm.Context.ObjectQueries.AvatarsByPersist.TryGetValue(AvatarPID, out VMAvatar avatar))
            {
                return false;
            }

            avatar.Thread.Interrupt = true;

            // Starts leaving lot, but the player will disconnect a lot earlier.
            avatar.UserLeaveLot(false);

            // If this command is meant for this client, and we're not fast forwarding through state, then begin the lot switch.
            if (vm.MyUID == AvatarPID && !vm.Driver.RunningCatchup)
            {
                // Prepare to transition to the new lot. We'll do this as soon as we disconnect.
                vm.SignalLotSwitch(TargetLot, Transition);
            }

            return true;
        }

        public override bool Verify(VM vm, VMAvatar caller)
        {
            return !FromNet; //can only be sent out by server
        }

        #region VMSerializable Members
        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(AvatarPID);
            writer.Write(TargetLot);
            VMNetSimJoinCmd.PutTransition(writer, Transition);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            AvatarPID = reader.ReadUInt32();
            TargetLot = reader.ReadUInt32();
            Transition = VMNetSimJoinCmd.GetTransition(reader);
        }
        #endregion
    }
}
