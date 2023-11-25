using FSO.SimAntics.Model.Platform;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics.Primitives;
using FSO.SimAntics.Utils;

namespace FSO.SimAntics.Model.TS1Platform
{
    public class VMTS1LotState : VMAbstractLotState
    {
        public SIMI SimulationInfo;
        public FAMI CurrentFamily;

        public VMTS1LotState() : base() { }
        public VMTS1LotState(int version) : base(version) { }

        public void ActivateFamily(VM vm, FAMI family)
        {
            if (family == null) return;
            vm.SetGlobalValue(9, (short)family.ChunkID);
            CurrentFamily = family;
        }

        /// <summary>
        /// Ensure all members of the family are present on the lot.
        /// Spawns missing family members at the mailbox.
        /// </summary>
        public void VerifyFamily(VM vm)
        {
            if (CurrentFamily == null)
            {
                vm.SetGlobalValue(32, 1);
                return;
            }
            vm.SetGlobalValue(32, 0);
            vm.SetGlobalValue(9, (short)CurrentFamily.ChunkID);
            var missingMembers = new HashSet<uint>(CurrentFamily.RuntimeSubset);
            foreach (var avatar in vm.Context.ObjectQueries.Avatars)
            {
                missingMembers.Remove(avatar.Object.OBJ.GUID);
            }

            foreach (var member in missingMembers)
            {
                var sim = vm.Context.CreateObjectInstance(member, LotView.Model.LotTilePos.OUT_OF_WORLD, LotView.Model.Direction.NORTH).Objects[0];
                ((VMAvatar)sim).SetPersonData(VMPersonDataVariable.TS1FamilyNumber, (short)CurrentFamily.ChunkID);
                var mailbox = vm.Entities.FirstOrDefault(x => (x.Object.OBJ.GUID == 0xEF121974 || x.Object.OBJ.GUID == 0x1D95C9B0));
                if (mailbox != null) VMFindLocationFor.FindLocationFor(sim, mailbox, vm.Context, VMPlaceRequestFlags.Default);
                ((VMAvatar)sim).AvatarState.Permissions = Model.TSOPlatform.VMTSOAvatarPermissions.Owner;

                vm.Scheduler.RescheduleInterrupt(sim);
            }

        }


        public override void Deserialize(BinaryReader reader)
        {
            if (reader.ReadBoolean())
            {
                SimulationInfo = new SIMI() { ChunkID = 1, ChunkLabel = "", ChunkType = "SIMI" };
                SimulationInfo.Read(null, reader.BaseStream);
            }

            //this is really only here for future networking. families should be activated (see abover) when joining lots for the first time
            var famID = reader.ReadUInt16(); 
            if (famID < 65535)
            {
                CurrentFamily = new FAMI() { ChunkID = famID, ChunkLabel = "", ChunkType = "FAMI" };
                CurrentFamily.Read(null, reader.BaseStream);
            }
        }

        public override void SerializeInto(BinaryWriter writer)
        {
            writer.Write(SimulationInfo != null);
            SimulationInfo?.Write(null, writer.BaseStream);
            writer.Write(CurrentFamily?.ChunkID ?? 65535);
            if (CurrentFamily != null) CurrentFamily.Write(null, writer.BaseStream);
        }

        public override void Tick(VM vm, object owner)
        {
        }

        public void UpdateSIMI(VM vm)
        {
            if (SimulationInfo == null) return;

            var objValue = VMArchitectureStats.GetObjectValue(vm);
            SimulationInfo.ArchitectureValue = VMArchitectureStats.GetArchValue(vm.Context.Architecture) + objValue.Item2;
            SimulationInfo.ObjectsValue = objValue.Item1;
            SimulationInfo.Version = 0x3E;
            SimulationInfo.GlobalData = vm.GlobalState;
        }

        public override void ActivateValidator(VM vm)
        {
            Validator = new VMDefaultValidator(vm);
        }
    }
}
