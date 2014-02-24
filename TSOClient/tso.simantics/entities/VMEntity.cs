using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using tso.content;
using tso.simantics.engine;
using tso.simantics.model;
using tso.files.formats.iff.chunks;

namespace tso.simantics
{
    public class VMEntityRTTI{
        public string[] AttributeLabels;
    }

    public abstract class VMEntity
    {
        public VMEntityRTTI RTTI;

        /** ID of the object **/
        public short ObjectID;

        public Stack<StackFrame> Stack = new Stack<StackFrame>();
        public List<VMRoutine> Queue = new List<VMRoutine>();
        public GameObject Object;
        public VMThread Thread;

        /** Persistent state variables controlled by bhavs **/
        private short[] Attributes;

        /** VM Variables **/
        public short DirtyLevel;
        public short RoomImpact;
        public VMEntityFlags Flags;
        public short LockoutCount;
        /** Used to show/hide dynamic sprites **/
        public ushort DynamicSpriteFlags;

        public short[] ObjectData;

        public VMEntity(GameObject obj)
        {
            this.Object = obj;
            /** 
             * For some reason, in the aquarium object (maybe others) the numAttributes is set to 0
             * but it should be 4. There are 4 entries in the label table. Go figure?
             */
            ObjectData = new short[80];

            RTTI = new VMEntityRTTI();
            var numAttributes = obj.OBJ.NumAttributes;

            var attributeTable = obj.Resource.Get<STR>(256);
            if (attributeTable != null)
            {
                numAttributes = (ushort)Math.Max(numAttributes, attributeTable.Length);
                RTTI.AttributeLabels = new string[numAttributes];
                for (var i = 0; i < numAttributes; i++)
                {
                    RTTI.AttributeLabels[i] = attributeTable.GetString(i);
                }
            }
            this.Attributes = new short[numAttributes];
            if (obj.OBJ.GUID == 0x98E0F8BD)
            {
                this.Attributes[0] = 2;
            }
        }

        public virtual void Init(VMContext context){
            this.Thread = new VMThread(context, this, this.Object.OBJ.StackSize);
        }

        public bool IsDynamicSpriteFlagSet(ushort index){
            return (DynamicSpriteFlags & (0x1 << index)) > 0;
        }
        public virtual void SetDynamicSpriteFlag(ushort index, bool set){
            if (set){
                ushort bitflag = (ushort)(0x1 << index);
                DynamicSpriteFlags = (ushort)(DynamicSpriteFlags | bitflag);
            }else{
                DynamicSpriteFlags = (ushort)(DynamicSpriteFlags & ((ushort)~(0x1 << index)));
            }
        }

        public virtual short GetAttribute(ushort data){
            return Attributes[data];
        }

        public virtual void SetAttribute(ushort data, short value){
            Attributes[data] = value;
        }

        public virtual short GetValue(VMStackObjectVariable var){
            if ((short)var > 79) throw new Exception("Object Data out of range!");
            return ObjectData[(short)var];

            /*switch (var){
                case VMStackObjectVariable.ObjectId:
                    return ObjectID;
                case VMStackObjectVariable.DirtyLevel:
                    return DirtyLevel;
                case VMStackObjectVariable.RoomImpact:
                    return RoomImpact;
                case VMStackObjectVariable.Flags:
                    return (short)Flags;
                case VMStackObjectVariable.LockoutCount:
                    return LockoutCount;
            }
            */
        }

        public virtual bool SetValue(VMStackObjectVariable var, short value){
            if ((short)var > 79) throw new Exception("Object Data out of range!");
            ObjectData[(short)var] = value;
            return true;

            /*switch (var){
                case VMStackObjectVariable.DirtyLevel:
                    DirtyLevel = value;
                    return true;
                case VMStackObjectVariable.RoomImpact:
                    RoomImpact = value;
                    return true;
                case VMStackObjectVariable.Flags:
                    Flags = (VMEntityFlags)value;
                    return true;
                case VMStackObjectVariable.LockoutCount:
                    LockoutCount = value;
                    return true;
                default:
                    throw new Exception("I dont understand how to set variable " + var);
            }*/
        }

        public void Execute(VMRoutine routine){
            Queue.Add(routine);
        }
    }

    [Flags]
    public enum VMEntityFlags
    {
        ShowGhost = 1,
        DissalowInteraction = 1 << 1,
        HasZeroExtent = 1 << 2,
        CanWalk = 1 << 3,
        AllowPersonInteraction = 1 << 4,
        Occupied = 1 << 5,
        NotifiedByIdleForInput = 1 << 6,
        InteractionCanceled = 1 << 7,
        ChairFacing = 1 << 8,
        Burning = 1 << 9,
        HideForCutaway = 1 << 10,
        FireProof = 1 << 11,
        TurnedOff = 1 << 12,
        NeedsMaintinance = 1 << 13,
        ShowDynObjNameInTooltip = 1 << 14
    }
}
