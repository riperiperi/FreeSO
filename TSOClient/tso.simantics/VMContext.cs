using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using tso.world;
using TSO.Simantics.engine;
using TSO.Simantics.engine.primitives;
using TSO.Simantics.primitives;
using TSO.Content;
using TSO.Files.formats.iff;
using tso.world.model;
using tso.world.components;

namespace TSO.Simantics
{
    public class VMContext
    {
        public Blueprint Blueprint;
        public VMClock Clock { get; internal set; }
        public World World { get; internal set; }
        public Dictionary<ushort, VMPrimitiveRegistration> Primitives = new Dictionary<ushort, VMPrimitiveRegistration>();

        public GameGlobal Globals;
        
        public VM VM;

        public VMContext(World world){
            this.World = world;
            this.Clock = new VMClock();

            AddPrimitive(new VMPrimitiveRegistration(new VMGotoRoutingSlot()) {
                Opcode = 45,
                Name = "goto_routing_slot",
                OperandModel = typeof(VMGotoRoutingSlotOperand)
            });
            AddPrimitive(new VMPrimitiveRegistration(new VMAnimateSim()) {
                Opcode = 44,
                Name = "animate",
                OperandModel = typeof(VMAnimateSimOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMCreateObjectInstance())
            {
                Opcode = 42,
                Name = "create_object_instance",
                OperandModel = typeof(VMCreateObjectInstanceOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMRemoveObjectInstance())
            {
                Opcode = 18,
                Name = "remove_object_instance",
                OperandModel = typeof(VMRemoveObjectInstanceOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMRunTreeByName())
            {
                Opcode = 28,
                Name = "run_tree_by_name",
                OperandModel = typeof(VMRunTreeByNameOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMPushInteraction())
            {
                Opcode = 13,
                Name = "push_interaction",
                OperandModel = typeof(VMPushInteractionOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration (new VMExpression()) {
                Opcode = 2,
                Name = "expression",
                OperandModel = typeof(VMExpressionOperand)
            });
            AddPrimitive(new VMPrimitiveRegistration(new VMRandomNumber())
            {
                Opcode = 8,
                Name = "random_number",
                OperandModel = typeof(VMRandomNumberOperand)
            });
            AddPrimitive(new VMPrimitiveRegistration(new VMPlaySound())
            {
                Opcode = 23,
                Name = "play_sound",
                OperandModel = typeof(VMPlaySoundOperand)
            });
            AddPrimitive(new VMPrimitiveRegistration(new VMSleep())
            {
                Opcode = 0,
                Name = "sleep",
                OperandModel = typeof(VMSleepOperand)
            });
            AddPrimitive(new VMPrimitiveRegistration(new VMRefresh())
            {
                Opcode = 7,
                Name = "refresh",
                OperandModel = typeof(VMRefreshOperand)
            });
            AddPrimitive(new VMPrimitiveRegistration(new VMNotifyOutOfIdle())
            {
                Opcode = 49,
                Name = "stackobj_notify_out_of_idle",
                OperandModel = typeof(VMAnimateSimOperand)
            });
            AddPrimitive(new VMPrimitiveRegistration(new VMSetMotiveChange())
            {
                Opcode = 29,
                Name = "set_motive_deltas",
                OperandModel = typeof(VMSetMotiveChangeOperand)
            });
            AddPrimitive(new VMPrimitiveRegistration(new VMChangeSuitOrAccessory())
            {
                Opcode = 6,
                Name = "change_suit_or_accessory",
                OperandModel = typeof(VMChangeSuitOrAccessoryOperand)
            });
            AddPrimitive(new VMPrimitiveRegistration(new VMTransferFunds())
            {
                Opcode = 25,
                Name = "transfer_funds",
                OperandModel = typeof(VMTransferFundsOperand)
            });
            AddPrimitive(new VMPrimitiveRegistration(new VMSetToNext())
            {
                Opcode = 31,
                Name = "set_to_next",
                OperandModel = typeof(VMSetToNextOperand)
            });
            AddPrimitive(new VMPrimitiveRegistration(new VMGotoRelativePosition())
            {
                Opcode = 27,
                Name = "goto_relative",
                OperandModel = typeof(VMGotoRelativePositionOperand)
            });
            AddPrimitive(new VMPrimitiveRegistration(new VMTestObjectType())
            {
                Opcode = 32,
                Name = "test_object_type",
                OperandModel = typeof(VMTestObjectTypeOperand)
            });
            AddPrimitive(new VMPrimitiveRegistration(new VMSpecialEffect())
            {
                Opcode = 35,
                Name = "special_effect",
                OperandModel = typeof(VMSpecialEffectOperand)
            });
            AddPrimitive(new VMPrimitiveRegistration(new VMSetBalloonHeadline())
            {
                Opcode = 41,
                Name = "set_balloon_headline",
                OperandModel = typeof(VMSetBalloonHeadlineOperand)
            });
            AddPrimitive(new VMPrimitiveRegistration(new VMDialogPrivateStrings())
            {
                Opcode = 36,
                Name = "dialog_private",
                OperandModel = typeof(VMDialogPrivateStringsOperand)
            });
            AddPrimitive(new VMPrimitiveRegistration(new VMStopAllSounds())
            {
                Opcode = 48,
                Name = "stop_all_sounds",
                OperandModel = typeof(VMStopAllSoundsOperand)
            });
        }

        public VMGameObject CreateObjectInstance(UInt32 GUID, short x, short y, sbyte level, Direction direction)
        {
            var objDefinition = TSO.Content.Content.Get().WorldObjects.Get(GUID);
            if (objDefinition == null) return null;

            var worldObject = new ObjectComponent(objDefinition);
            worldObject.Direction = direction;

            var vmObject = new VMGameObject(objDefinition, worldObject);

            VM.AddEntity(vmObject);
            Blueprint.ChangeObjectLocation(worldObject, x, y, level);
            return vmObject;
        }

        public VMPrimitiveRegistration GetPrimitive(ushort opcode)
        {
            if (Primitives.ContainsKey(opcode)){
                return Primitives[opcode];
            }
            return null;
        }

        public void AddPrimitive(VMPrimitiveRegistration primitive){
            Primitives.Add(primitive.Opcode, primitive);
        }

        public void ThreadIdle(VMThread thread){
            /** Switch thread to idle **/
            VM.ThreadIdle(thread);
        }

        public void ThreadActive(VMThread thread){
            /** Switch thread to active **/
            VM.ThreadActive(thread);
        }
    }
}
