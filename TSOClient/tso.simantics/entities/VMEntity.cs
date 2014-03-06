using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using TSO.Content;
using TSO.Simantics.engine;
using TSO.Simantics.model;
using TSO.Files.formats.iff.chunks;
using tso.world;
using Microsoft.Xna.Framework;

namespace TSO.Simantics
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
        public TTAB TreeTable;
        public TTAs TreeTableStrings;
        public Dictionary<string, VMTreeByNameTableEntry> TreeByName;
        public WorldComponent WorldUI;

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
                SemiGlobal = TSO.Content.Content.Get().WorldObjectGlobals.Get(GLOBChunks[0].Name);
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

            TreeTable = obj.Resource.Get<TTAB>(obj.OBJ.TreeTableID);
            if (TreeTable != null) TreeTableStrings = obj.Resource.Get<TTAs>(obj.OBJ.TreeTableID);
            if (TreeTable == null && SemiGlobal != null)
            {
                TreeTable = SemiGlobal.Resource.Get<TTAB>(obj.OBJ.TreeTableID); //tree not in local, try semiglobal
                TreeTableStrings = SemiGlobal.Resource.Get<TTAs>(obj.OBJ.TreeTableID);
            }
            //no you cannot get global tree tables don't even ask

            this.Attributes = new short[numAttributes];
            if (obj.OBJ.GUID == 0x98E0F8BD)
            {
                this.Attributes[0] = 2;
            }
        }

        public virtual void Tick()
        {
            //decrement lockout count
            if (ObjectData[(int)VMStackObjectVariable.LockoutCount] > 0) ObjectData[(int)VMStackObjectVariable.LockoutCount]--;
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
            GenerateTreeByName(context);
            this.Thread = new VMThread(context, this, this.Object.OBJ.StackSize);

            ExecuteEntryPoint(0, context); //Init
            ExecuteEntryPoint(11, context); //User Placement
            if (Object.OBJ.GUID == 0x98E0F8BD || Object.OBJ.GUID == 0x5D7B6688) //let aquarium & flowers run main
            {
                ExecuteEntryPoint(1, context);
            }
        }

        public void GenerateTreeByName(VMContext context)
        {
            TreeByName = new Dictionary<string, VMTreeByNameTableEntry>();

            var bhavs = Object.Resource.List<BHAV>();
            if (bhavs != null)
            {
                foreach (var bhav in bhavs)
                {
                    TreeByName.Add(bhav.ChunkLabel, new VMTreeByNameTableEntry(bhav, Object.Resource));
                }
            }

            /*bhavs = SemiGlobal.Resource.List<BHAV>();    //Globals and semiglobals not included?
            if (bhavs != null)
            {
                foreach (var bhav in bhavs)
                {
                    TreeByName.Add(bhav.ChunkLabel, new VMTreeByNameTableEntry(bhav, SemiGlobal.Resource));
                }
            }

            bhavs = context.Globals.Resource.List<BHAV>();
            if (bhavs != null)
            {
                foreach (var bhav in bhavs)
                {
                    TreeByName.Add(bhav.ChunkLabel, new VMTreeByNameTableEntry(bhav, context.Globals.Resource));
                }
            }*/
        }

        public void ExecuteEntryPoint(int entry, VMContext context)
        {
            
            if (EntryPoints[entry].ActionFunction > 0)
            {
                BHAV bhav;
                GameIffResource CodeOwner;
                ushort ActionID = EntryPoints[entry].ActionFunction;
                if (ActionID < 4096)
                { //global

                    bhav = context.Globals.Resource.Get<BHAV>(ActionID);
                    CodeOwner = context.Globals.Resource;
                }
                else if (ActionID < 8192)
                { //local
                    bhav = Object.Resource.Get<BHAV>(ActionID);
                    CodeOwner = Object.Resource;
                }
                else
                { //semi-global
                    bhav = SemiGlobal.Resource.Get<BHAV>(ActionID);
                    CodeOwner = SemiGlobal.Resource;
                }

                if (bhav == null) throw new Exception("Invalid BHAV call!");
                
                this.Thread.EnqueueAction(new TSO.Simantics.engine.VMQueuedAction
                {
                    Callee = this,
                    CodeOwner = CodeOwner,
                    /** Main function **/
                    Routine = context.VM.Assemble(bhav)
                });
            }
        }

        public VMBHAVOwnerPair GetBHAVWithOwner(ushort ActionID, VMContext context)
        {
            BHAV bhav;
            GameIffResource CodeOwner;
            if (ActionID < 4096)
            { //global
                bhav = context.Globals.Resource.Get<BHAV>(ActionID);
                CodeOwner = context.Globals.Resource;
            }
            else if (ActionID < 8192)
            { //local
                bhav = Object.Resource.Get<BHAV>(ActionID);
                CodeOwner = Object.Resource;
            }
            else
            { //semi-global
                bhav = SemiGlobal.Resource.Get<BHAV>(ActionID);
                CodeOwner = SemiGlobal.Resource;
            }

            if (bhav == null) throw new Exception("Invalid BHAV call!");
            return new VMBHAVOwnerPair(bhav, CodeOwner);

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

        public void SetFlag(VMEntityFlags flag, bool set)
        {
            if (set) ObjectData[(int)VMStackObjectVariable.Flags] |= (short)(flag);
            else ObjectData[(int)VMStackObjectVariable.Flags] &= ((short)~(flag));
            return;
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
                case VMStackObjectVariable.Direction:
                    switch (this.Direction)
                    {
                        case tso.world.model.Direction.LeftBack:
                            return 6;
                        case tso.world.model.Direction.LeftFront:
                            return 4;
                        case tso.world.model.Direction.RightFront:
                            return 2;
                        case tso.world.model.Direction.RightBack:
                            return 0;
                        default:
                            return 0;
                    }

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
            switch (var) //special cases
            {
                case VMStackObjectVariable.Direction:
                    value = (short)(((int)value + 65536)%8);
                    switch (value) {
                        case 6:
                            Direction = tso.world.model.Direction.LeftBack;
                            return true;
                        case 4:
                            Direction = tso.world.model.Direction.LeftFront;
                            return true;
                        case 2:
                            Direction = tso.world.model.Direction.RightFront;
                            return true;
                        case 0:
                            Direction = tso.world.model.Direction.RightBack;
                            return true;
                        default:
                            throw new Exception("Diagonal Set Not Implemented!");
                    }
            }

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

        public abstract Vector3 Position {get; set;}
        public abstract tso.world.model.Direction Direction { get; set; }

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

    public class VMTreeByNameTableEntry
    {
        public BHAV bhav;
        public GameIffResource Owner;

        public VMTreeByNameTableEntry(BHAV bhav, GameIffResource owner)
        {
            this.bhav = bhav;
            this.Owner = owner;
        }
    }
}
