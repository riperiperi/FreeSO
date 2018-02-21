﻿using FSO.Files.Formats.IFF.Chunks;
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
            var inTarget = (operand.UseObjectInTemp4) ? context.VM.GetObjectById(context.Thread.TempRegisters[4]) : context.Caller;
            if (inTarget is VMGameObject) { }
            var target = (VMAvatar)(inTarget);
            var neighbour = target.GetPersonData(Model.VMPersonDataVariable.NeighborId);
            var inventory = neighbourhood.GetInventoryByNID(neighbour);
            var count = (operand.CountInTemp0) ? context.Thread.TempRegisters[0] : 1;
            var type = operand.TokenType;

            //type 4: magic town purchasables
            //type 6: vacation purchasables
            //note: guid 0, type 5 is used for a global count for downtown objects
            //var guid = (operand.GUID == 0) ? context.CodeOwner.GUID : operand.GUID;
            var guid = operand.GUID;

            switch (operand.Mode)
            {
                case VMTS1InventoryMode.AddToken:
                    inventory = InitInventory(neighbour, inventory);
                    //if we have an existing item add to that
                    var aitem = inventory.FirstOrDefault(x => x.GUID == guid && x.Type == type);

                    if (aitem == null)
                        inventory.Add(new InventoryItem() { Count = (ushort)count, GUID = guid, Type = type });
                    else
                        aitem.Count += (ushort)count;

                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMTS1InventoryMode.DecrementToken:
                    count = 1;
                    goto case VMTS1InventoryMode.RemoveToken;
                case VMTS1InventoryMode.RemoveToken:
                    if (inventory == null) return VMPrimitiveExitCode.GOTO_FALSE; //can't remove a token that isn't there
                    var ritem = inventory.FirstOrDefault(x => x.GUID == guid && x.Type == type);
                    if (ritem == null || ritem.Count < count) return VMPrimitiveExitCode.GOTO_FALSE; //can't remove a token that isn't there
                    ritem.Count -= (ushort)count;
                    if (ritem.Count == 0) inventory.Remove(ritem);
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMTS1InventoryMode.HasToken:
                    if (inventory == null) return VMPrimitiveExitCode.GOTO_FALSE;
                    var items = inventory.FirstOrDefault(x => x.GUID == guid && x.Type == type);
                    var itemcount = (short)(items?.Count ?? 0);
                    context.Thread.TempRegisters[0] = itemcount;
                    return (itemcount >= count)? VMPrimitiveExitCode.GOTO_TRUE : VMPrimitiveExitCode.GOTO_FALSE;
                case VMTS1InventoryMode.HasTokenOfType: //ignores guid
                    if (inventory == null) return VMPrimitiveExitCode.GOTO_FALSE;
                    var items2 = inventory.Where(x => x.Type == type).ToList();
                    context.Thread.TempRegisters[0] = (short)items2.Sum(x => x.Count);
                    return (items2.Count >= count) ? VMPrimitiveExitCode.GOTO_TRUE : VMPrimitiveExitCode.GOTO_FALSE;
                case VMTS1InventoryMode.Temp0NeighborAsAutofollow:
                    inventory = InitInventory(neighbour, inventory);
                    //if we have an existing item replace it
                    aitem = inventory.FirstOrDefault(x => x.GUID == 0 && x.Type == 2);

                    if (aitem == null)
                        inventory.Add(new InventoryItem() { Count = (ushort)context.Thread.TempRegisters[0], GUID = 10, Type = 2 });
                    else
                        aitem.Count = (ushort)count;
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMTS1InventoryMode.Temp0NeighborAsFollowHome:
                    inventory = InitInventory(neighbour, inventory);
                    //if we have an existing item replace it
                    aitem = inventory.FirstOrDefault(x => x.GUID == 1 && x.Type == 2);

                    if (aitem == null)
                        inventory.Add(new InventoryItem() { Count = (ushort)context.Thread.TempRegisters[0], GUID = 11, Type = 2 });
                    else
                        aitem.Count = (ushort)count;
                    return VMPrimitiveExitCode.GOTO_TRUE;
                default:
                    return VMPrimitiveExitCode.GOTO_TRUE;
            }
        }

        private List<InventoryItem> InitInventory( short neighbour, List<InventoryItem> inventory)
        {
            if (inventory == null)
            {
                var neighbourhood = Content.Content.Get().Neighborhood;
                //set up this neighbour's inventory...
                inventory = new List<InventoryItem>();
                neighbourhood.SetInventoryForNID(neighbour, inventory);
            }
            return inventory;
        }
    }

    public class VMTS1InventoryOperationsOperand : VMPrimitiveOperand
    {
        public VMTS1InventoryMode Mode { get; set; }
        public byte TokenType { get; set; } //token type
        public byte Unknown3 { get; set; } //flags
        //1 - unknown
        //2 - count in temp 0
        //4
        //8 - result in param 0?
        //16 - target object in temp 4 (???)
        public byte Unknown4 { get; set; }
        public uint GUID { get; set; }

        public bool CountInTemp0
        {
            get
            {
                return (Unknown3 & 2) > 0;
            }
            set
            {
                Unknown3 &= unchecked((byte)(~2));
                if (value) Unknown3 |= 2;
            }
        }

        public bool UseObjectInTemp4
        {
            get
            {
                return (Unknown4 & 32) > 0;
            }
            set
            {
                Unknown4 &= unchecked((byte)(~32));
                if (value) Unknown4 |= 32;
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
        Temp0NeighborAsAutofollow = 5,
        Temp0NeighborAsFollowHome = 6,
        Unknown7 = 7
    }
}
