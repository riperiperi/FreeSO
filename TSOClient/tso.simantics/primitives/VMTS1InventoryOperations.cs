using FSO.Files.Formats.IFF.Chunks;
using FSO.Files.Utils;
using FSO.SimAntics.Engine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.Primitives
{
    public class VMTS1InventoryOperations : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            //todo: check condition
            var operand = (VMTS1InventoryOperationsOperand)args;

            //tragic clown magic growth qualify: hasToken, stack object
            //6 17 45

            //magic person a - check inventory (caller??)
            //7 17 45

            //teleport here check: hasToken
            //6 17 13
            //has wand check:
            //7 17 13

            //get 35 coins:
            //7 3 9
            //get wand, toadsweat:
            //7 1 9

            //spawn purchases test (mode 4)
            //0 180 4
            //5 180 4
            //4 180 4

            //remove token caller get in wagon:
            //1 132 8
            //mode 4 something:
            //1 180 4
            //3 180 4
            //


            var neighbourhood = Content.Content.Get().Neighborhood;
            var inTarget = (operand.UseStackObject) ? context.VM.GetObjectById(context.Thread.TempRegisters[4]) : context.Caller;
            if (inTarget is VMGameObject) { }
            var target = (VMAvatar)(inTarget);
            var neighbour = target.GetPersonData(Model.VMPersonDataVariable.NeighborId);
            var inventory = neighbourhood.GetInventoryByNID(neighbour);
            var count = (operand.CountInTemp0) ? context.Thread.TempRegisters[0] : 1;


            switch (operand.Mode)
            {
                case VMTS1InventoryMode.AddToken:
                    if (inventory == null)
                    {
                        //set up this neighbour's inventory...
                        inventory = new List<InventoryItem>();
                        neighbourhood.SetInventoryForNID(neighbour, inventory);
                    }
                    //if we have an existing item add to that
                    var aitem = inventory.FirstOrDefault(x => x.GUID == operand.GUID && x.Type == operand.TokenType);

                    if (aitem == null)
                        inventory.Add(new InventoryItem() { Count = (ushort)count, GUID = operand.GUID, Type = operand.TokenType });
                    else
                        aitem.Count += (ushort)count;

                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMTS1InventoryMode.DecrementToken:
                    count = 1;
                    goto case VMTS1InventoryMode.RemoveToken;
                case VMTS1InventoryMode.RemoveToken:
                    if (inventory == null) return VMPrimitiveExitCode.GOTO_FALSE; //can't remove a token that isn't there
                    var ritem = inventory.FirstOrDefault(x => x.GUID == operand.GUID && x.Type == operand.TokenType);
                    if (ritem == null || ritem.Count < count) return VMPrimitiveExitCode.GOTO_FALSE; //can't remove a token that isn't there
                    ritem.Count -= (ushort)count;
                    if (ritem.Count == 0) inventory.Remove(ritem);
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMTS1InventoryMode.HasToken:
                    if (inventory == null) return VMPrimitiveExitCode.GOTO_FALSE;
                    var items = inventory.FirstOrDefault(x => x.GUID == operand.GUID && x.Type == operand.TokenType);
                    var itemcount = (short)(items?.Count ?? 0);
                    context.Thread.TempRegisters[0] = itemcount;
                    return (itemcount >= count)? VMPrimitiveExitCode.GOTO_TRUE : VMPrimitiveExitCode.GOTO_FALSE;
                case VMTS1InventoryMode.HasTokenOfType: //ignores guid
                    if (inventory == null) return VMPrimitiveExitCode.GOTO_FALSE;
                    var items2 = inventory.Where(x => x.Type == operand.TokenType).ToList();
                    context.Thread.TempRegisters[0] = (short)items2.Sum(x => x.Count);
                    return (items2.Count >= count) ? VMPrimitiveExitCode.GOTO_TRUE : VMPrimitiveExitCode.GOTO_FALSE;
                default:
                    return VMPrimitiveExitCode.GOTO_TRUE;
            }
        }
    }

    public class VMTS1InventoryOperationsOperand : VMPrimitiveOperand
    {
        public VMTS1InventoryMode Mode { get; set; }
        public byte TokenType; //token type
        public byte Unknown3; //flags
        //1 - unknown
        //2 - count in temp 0
        //4
        //8 - result in param 0?
        //16 - target object in temp 4 (???)
        public byte Unknown4;
        public uint GUID;

        public bool CountInTemp0
        {
            get
            {
                return (Unknown3 & 2) > 0;
            }
        }

        public bool UseStackObject
        {
            get
            {
                return (Unknown4 & 32) > 0;
            }
        }

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                Mode = (VMTS1InventoryMode)io.ReadByte();
                TokenType = io.ReadByte();
                Unknown3 = io.ReadByte();
                Unknown4 = io.ReadByte();
                GUID = io.ReadUInt32();
            }
        }

        public void Write(byte[] bytes)
        {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write((byte)Mode);
                io.Write(TokenType);
                io.Write(Unknown3);
                io.Write(Unknown4);
                io.Write(GUID);
            }
        }
        #endregion
    }

    public enum VMTS1InventoryMode : byte
    {
        AddToken = 0, //add
        DecrementToken = 1,
        RemoveToken = 2,
        HasToken = 3, //count in temp0
        HasTokenOfType = 4, //count in temp0. ignores guid.
    }
}
