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

        private static VMPersonDataVariable[] LockToSkill =
        {
            VMPersonDataVariable.BodySkill,
            VMPersonDataVariable.CharismaSkill,
            VMPersonDataVariable.CookingSkill,
            VMPersonDataVariable.CreativitySkill,
            VMPersonDataVariable.LogicSkill,
            VMPersonDataVariable.MechanicalSkill
        };

        public override bool Execute(VM vm, VMAvatar caller)
        {
            //need caller to be present
            if (caller == null) return false;
            var limit = caller.SkillLocks;
            SkillID = Math.Min(SkillID, (byte)5); //must be 0-5
            int otherLocked = 0;
            for (int i=0; i<6; i++) //sum other skill locks to see what we can feasibly put in this skill
            {
                if (i == SkillID) continue;
                otherLocked += caller.GetPersonData((VMPersonDataVariable)((int)VMPersonDataVariable.SkillLockBase + i))/100;
            }
            if (otherLocked >= limit) return false; //cannot lock this skill at all
            LockLevel = (byte)Math.Min(caller.GetPersonData(LockToSkill[SkillID])/100, Math.Min(LockLevel, (byte)(limit - otherLocked))); //can only lock up to the limit

            caller.SetPersonData((VMPersonDataVariable)((int)VMPersonDataVariable.SkillLockBase + SkillID), (short)(LockLevel*100));
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
