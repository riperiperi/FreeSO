using FSO.SimAntics.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMNetSkillLockCmd : VMNetCommandBodyAbstract
    {
        public byte SkillID;
        public byte LockLevel;

        public override bool Execute(VM vm, VMAvatar caller)
        {
            //need caller to be present
            if (caller == null) return false;
            var limit = caller.GetPersonData(VMPersonDataVariable.SkillLock);
            SkillID = Math.Min(SkillID, (byte)5); //must be 0-5
            int otherLocked = 0;
            for (int i=0; i<6; i++) //sum other skill locks to see what we can feasibly put in this skill
            {
                if (i == SkillID) continue;
                otherLocked += caller.GetPersonData((VMPersonDataVariable)((int)VMPersonDataVariable.SkillLockBase + i));
            }
            if (otherLocked >= limit) return false; //cannot lock this skill at all
            LockLevel = Math.Min(LockLevel, (byte)(limit - otherLocked)); //can only lock up to the limit

            caller.SetPersonData((VMPersonDataVariable)((int)VMPersonDataVariable.SkillLockBase + SkillID), LockLevel);
            return true;
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            SkillID = reader.ReadByte();
            LockLevel = reader.ReadByte();
        }

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(SkillID);
            writer.Write(LockLevel);
        }
    }
}
