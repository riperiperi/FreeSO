using System.IO;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    /// <summary>
    /// Causes a sim to begin being deleted. Can be user initiated, but they will be disconnected when their sim is fully gone.
    /// </summary>
    public class VMNetSimLeaveCmd : VMNetCommandBodyAbstract
    {
        public override bool Execute(VM vm, VMAvatar sim)
        {
            if (sim != null && !sim.Dead)
            {
                // the user has left the lot with their sim still on it...
                // force leave lot. generate an action with incredibly high priority and cancel current

                sim.UserLeaveLot();
            }
            return true;
        }

        #region VMSerializable Members
        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
        }
        #endregion
    }
}
