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
        public GameGlobal SemiGlobal;

        /** Persistent state variables controlled by bhavs **/
        private short[] Attributes;

        /** Used to show/hide dynamic sprites **/
        public ushort DynamicSpriteFlags;

        /** Entry points for specific events, eg. init, main, clean... **/
        public OBJfFunctionEntry[] EntryPoints;

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

            if (obj.OBJ.UsesInTable == 0) EntryPoints = GenerateFunctionTable(obj.OBJ);
            else
            {
                var OBJfChunks = obj.Resource.List<OBJf>();
                if (OBJfChunks != null) EntryPoints = OBJfChunks[0].functions;
            }

            var GLOBChunks = obj.Resource.List<GLOB>();
            if (GLOBChunks != null)
            {
                SemiGlobal = Content.Get().WorldObjectGlobals.Get(GLOBChunks[0].Name);
            }

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

        public OBJfFunctionEntry[] GenerateFunctionTable(OBJD obj)
        {
            OBJfFunctionEntry[] result = new OBJfFunctionEntry[33];

            result[0].ActionFunction = obj.BHAV_Init;
            result[1].ActionFunction = obj.BHAV_MainID;
            result[2].ActionFunction = obj.BHAV_Load;
            result[3].ActionFunction = obj.BHAV_Cleanup;
            result[4].ActionFunction = obj.BHAV_QueueSkipped;
            result[5].ActionFunction = obj.BHAV_AllowIntersectionID;
            result[6].ActionFunction = obj.BHAV_WallAdjacencyChanged;
            result[7].ActionFunction = obj.BHAV_RoomChange;
            result[8].ActionFunction = 0; //dynamic multi tile update
            result[9].ActionFunction = obj.BHAV_Place;
            result[10].ActionFunction = obj.BHAV_PickupID;
            result[11].ActionFunction = obj.BHAV_UserPlace;
            result[12].ActionFunction = obj.BHAV_UserPickup;
            result[13].ActionFunction = obj.BHAV_LevelInfo;
            result[14].ActionFunction = obj.BHAV_ServingSurface;
            result[15].ActionFunction = 0; //portal
            result[16].ActionFunction = obj.BHAV_GardeningID;
            result[17].ActionFunction = obj.BHAV_WashHandsID;
            result[18].ActionFunction = obj.BHAV_PrepareFoodID;
            result[19].ActionFunction = obj.BHAV_CookFoodID;
            result[20].ActionFunction = obj.BHAV_PlaceSurfaceID;
            result[21].ActionFunction = obj.BHAV_DisposeID;
            result[22].ActionFunction = obj.BHAV_EatID;
            result[23].ActionFunction = 0; //pickup from slor
            result[24].ActionFunction = obj.BHAV_WashDishID;
            result[25].ActionFunction = obj.BHAV_EatSurfaceID;
            result[26].ActionFunction = obj.BHAV_SitID;
            result[27].ActionFunction = obj.BHAV_StandID;
            result[28].ActionFunction = obj.BHAV_Clean;
            result[29].ActionFunction = 0; //repair
            result[30].ActionFunction = 0; //client house join
            result[31].ActionFunction = 0; //prepare for sale
            result[32].ActionFunction = 0; //house unload

            return result;
        }

        public virtual void Init(VMContext context){
            this.Thread = new VMThread(context, this, this.Object.OBJ.StackSize);

            ExecuteEntryPoint(0, context);
            if (Object.OBJ.GUID == 0x98E0F8BD)
            {
                ExecuteEntryPoint(1, context);
            }
        }

        public void ExecuteEntryPoint(int entry, VMContext context)
        {
            
            if (EntryPoints[entry].ActionFunction > 0)
            {
                BHAV bhav;
                ushort ActionID = EntryPoints[entry].ActionFunction;
                if (ActionID < 4096)
                { //global

                    bhav = context.Globals.Resource.Get<BHAV>(ActionID);
                }
                else if (ActionID < 8192)
                { //local
                    bhav = Object.Resource.Get<BHAV>(ActionID);
                }
                else
                { //semi-global
                    bhav = SemiGlobal.Resource.Get<BHAV>(ActionID);
                }

                if (bhav == null) throw new Exception("Invalid BHAV call!");
                
                this.Thread.EnqueueAction(new tso.simantics.engine.VMQueuedAction
                {
                    Callee = this,
                    /** Main function **/
                    Routine = context.VM.Assemble(bhav)
                });
            }
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
            switch (var) //special cases
            {
                case VMStackObjectVariable.ObjectId:
                    return ObjectID;
            }
            if ((short)var > 79) throw new Exception("Object Data out of range!");
            return ObjectData[(short)var];


                /*case VMStackObjectVariable.DirtyLevel:
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
