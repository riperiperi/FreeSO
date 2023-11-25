using FSO.Files.Formats.IFF.Chunks;
using FSO.Files.Utils;
using FSO.SimAntics.Engine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
            //weird remove request with wrong type (0 instead of 8):
            //0x82, 0x09

            //NOTES:
            // - has token and remove token currently ignore the type, and only look for entries with the given GUID.
            //   I haven't seen inventory items appear under multiple types, so this shouldn't affect anything right now
            //   ...but there is probably a flag for it!

            //$FamilyAssets $NameAttrib $NeighborLocal $FameTitleLocal

            //=== from exe ===
            //1. | of count | and index to Temp | . | Returning count to Temp | 0. | starting at index  | stored in Temp | from Stack Object's GUID | with object |
            // (Using Inventory of object in Temp 4)
            //types:
            // SKILL   SOUVENIR    PURCHASE    SIMDATA     DATE    INGREDIENT  MAGIC   GIFT

            var neighbourhood = Content.Content.Get().Neighborhood;
            var inTarget = (operand.UseObjectInTemp4) ? context.VM.GetObjectById(context.Thread.TempRegisters[4]) : context.Caller;
            if (inTarget is VMGameObject) { }
            var target = (VMAvatar)(inTarget);
            var neighbour = target.GetPersonData(Model.VMPersonDataVariable.NeighborId);
            var inventory = neighbourhood.GetInventoryByNID(neighbour);
            var count = (operand.CountInTemp) ? context.Thread.TempRegisters[0] : 1;
            var type = operand.TokenType; //type 0 on find of type indicates "any type".
            var index = context.Thread.TempRegisters[operand.IndexTemp];
            
            //type 4: magic town purchasables
            //type 6: vacation purchasables
            var guid = (operand.GUID == 0) ? (uint)context.StackObject.Object.GUID : operand.GUID;
            //var guid = operand.GUID;

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

                case VMTS1InventoryMode.RemoveToken:
                    if (inventory == null) return VMPrimitiveExitCode.GOTO_FALSE; //can't remove a token that isn't there
                    var ritem = inventory.FirstOrDefault(x => x.GUID == guid && (type == 0 || x.Type == type));
                    if (ritem == null || ritem.Count < count) return VMPrimitiveExitCode.GOTO_FALSE; //can't remove a token that isn't there
                    if (count == -1) count = ritem.Count; //count of -1 means remove all
                    ritem.Count -= (ushort)count;
                    if (ritem.Count == 0) inventory.Remove(ritem);
                    //todo: does this write the index?
                    return VMPrimitiveExitCode.GOTO_TRUE;

                case VMTS1InventoryMode.RemoveTokenAtIndex:
                    //
                    if (inventory == null || index < 0 || index >= inventory.Count)
                        return VMPrimitiveExitCode.GOTO_FALSE; //can't remove a token that isn't there

                    ritem = inventory[index];
                    if (count == -1) count = ritem.Count; //count of -1 means remove all
                    ritem.Count -= (ushort)count;
                    if (ritem.Count == 0) inventory.Remove(ritem);
                    if (operand.NextIndexIntoTemp)
                        context.Thread.TempRegisters[operand.IndexTemp] = (short)(index-1);
                    return VMPrimitiveExitCode.GOTO_TRUE;

                case VMTS1InventoryMode.FindToken:
                    if (inventory == null) return VMPrimitiveExitCode.GOTO_FALSE;
                    var foundindex = inventory.FindIndex(x => x.GUID == guid && (type == 0 || x.Type == type));
                    if (foundindex == -1) return VMPrimitiveExitCode.GOTO_FALSE;
                    var items = inventory[foundindex];
                    var itemcount = (short)(items.Count);
                    context.Thread.TempRegisters[operand.CountTemp] = itemcount;
                    if (operand.FoundIndexIntoTemp) context.Thread.TempRegisters[operand.IndexTemp] = (short)foundindex;
                    return (itemcount > 0) ? VMPrimitiveExitCode.GOTO_TRUE : VMPrimitiveExitCode.GOTO_FALSE;
                    //return (itemcount >= count)? VMPrimitiveExitCode.GOTO_TRUE : VMPrimitiveExitCode.GOTO_FALSE;

                case VMTS1InventoryMode.SetToNextTokenOfType: //ignores guid
                    if (inventory == null) return VMPrimitiveExitCode.GOTO_FALSE;
                    var items2 = inventory.Where(x => x.Type == type).ToList();
                    context.Thread.TempRegisters[operand.CountTemp] = (short)items2.Sum(x => x.Count);
                    var next = items2.FirstOrDefault(x => inventory.IndexOf(x) > index);
                    if (next == null) return VMPrimitiveExitCode.GOTO_FALSE;
                    foundindex = inventory.IndexOf(next);
                    if (operand.NextIndexIntoTemp) context.Thread.TempRegisters[operand.IndexTemp] = (short)foundindex;
                    return VMPrimitiveExitCode.GOTO_TRUE;

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
        public byte Flags { get; set; } //flags
        //1 - unknown (regularly set for find token, ONLY this is set for add token and remove)
        //2 - count in temp
        //4-8 - temp[num] := count (set to next related)
        //16 - found index into temp (regularly set for find token)
        //32 - ??
        //64 - ??
        //128 - index in temp (set to next)
        public byte Flags2 { get; set; }
        //1-2 - temp[num] := index (1 regularly set, not in set to next? would be index in temp 0)
        //4 - (set in find token)
        //8 - (very regularly set)
        //16 - ??
        //32 - object in temp 4
        //64 - ??
        //128 - mode 8 remove time tokens?
        public uint GUID { get; set; }

        public int CountTemp
        {
            get
            {
                return (Flags >> 2) & 3;
            }
        }

        public int IndexTemp
        {
            get
            {
                return Flags2 & 3;
            }
        }

        public bool NextIndexIntoTemp
        {
            get
            {
                return (Flags & 0x80) > 0;
            }
        }

        public bool FoundIndexIntoTemp
        {
            get
            {
                return (Flags & 0x10) > 0;
            }
        }

        public bool CountInTemp
        {
            get
            {
                return (Flags & 2) > 0;
            }
            set
            {
                Flags &= unchecked((byte)(~2));
                if (value) Flags |= 2;
            }
        }

        public bool UseObjectInTemp4
        {
            get
            {
                return (Flags2 & 32) > 0;
            }
            set
            {
                Flags2 &= unchecked((byte)(~32));
                if (value) Flags2 |= 32;
            }
        }

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                Mode = (VMTS1InventoryMode)io.ReadByte();
                TokenType = io.ReadByte();
                Flags = io.ReadByte();
                Flags2 = io.ReadByte();
                GUID = io.ReadUInt32();
            }
        }

        public void Write(byte[] bytes)
        {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write((byte)Mode);
                io.Write(TokenType);
                io.Write(Flags);
                io.Write(Flags2);
                io.Write(GUID);
            }
        }
        #endregion
    }

    public enum VMTS1InventoryMode : byte
    {
        AddToken = 0, //add
        RemoveToken = 1,
        RemoveTokenAtIndex = 2,
        FindToken = 3, //count in temp0
        SetToNextTokenOfType = 4, //count in temp0. ignores guid.
        Temp0NeighborAsAutofollow = 5, 
        Temp0NeighborAsFollowHome = 6,
        UnusedRemoveTimeData = 8
    }
}
