using FSO.Files.Utils;
using System;
using System.Collections.Generic;
using System.IO;

namespace FSO.Files.Formats.IFF.Chunks
{
    /// <summary>
    /// Full tile offsets of a multitile part within a group, along with the ID of the lead object.
    /// </summary>
    public struct OBJMMultitile
    {
        // Relative tile offset to the lead tile in world space.
        public int OffsetX;
        public int OffsetY;
        public int OffsetLevel;

        // Absolute tile position in group space (the same regardless of rotation). Always positive, doesn't care what tile is the lead.
        public int GroupX;
        public int GroupY;
        public int GroupLevel;

        /// <summary>
        /// If this is 0, this object is the lead.
        /// </summary>
        public short MultitileParentID;

        public OBJMMultitile(IffFieldEncode iop)
        {
            OffsetX = iop.ReadInt32();
            OffsetY = iop.ReadInt32();
            OffsetLevel = iop.ReadInt32();

            GroupX = iop.ReadInt32();
            GroupY = iop.ReadInt32();
            GroupLevel = iop.ReadInt32();

            MultitileParentID = iop.ReadInt16();
        }
    }

    public struct OBJMFootprint
    {
        public int MinY;
        public int MinX;
        public int MaxY;
        public int MaxX;

        public OBJMFootprint(IffFieldEncode iop)
        {
            MinY = iop.ReadInt32();
            MinX = iop.ReadInt32();
            MaxY = iop.ReadInt32();
            MaxX = iop.ReadInt32();
        }
    }

    public struct OBJMPerson
    {
        public int Unknown1;
        public int Unknown2;

        public string Body; //b004mafit_01
        public string BodyTex; //BODY=b004mafitlgtrat
        public string Unk1; //
        public string Unk2; //
        public string LeftHand; //hmlo
        public string LeftHandTex; //HAND=huaolgt
        public string RightHand; //hmro
        public string RightHandTex; //HAND=huaolgt
        public string Head; //c013ma_pompa
        public string HeadTex; //HEAD-HEAD=c013malgt_pompa
        public string[] Pairs;
        public string Animation; //
        public string Unk3; //
        public string BaseAnimation; //a2o-standing-loop;-10;1000;70;1000;1;1;1

        public int UnknownValue;
        public float[] FirstFloats;
        public float[] MotiveDataOld;
        public float[] MotiveData;
        public short[] PersonData;

        public OBJMPerson(uint version, IffFieldEncode iop)
        {
            Unknown1 = iop.ReadInt32();
            Unknown2 = iop.ReadInt32();

            if (Unknown1 != 0 || Unknown2 != 0)
            {
                // Unknown 2 is frequently 1, not sure what that means.
            }

            Body = iop.ReadString(false); //b004mafit_01
            BodyTex = iop.ReadString(false); //BODY=b004mafitlgtrat
            Unk1 = iop.ReadString(false); //
            Unk2 = iop.ReadString(false); //
            LeftHand = iop.ReadString(false); //hmlo
            LeftHandTex = iop.ReadString(false); //HAND=huaolgt
            RightHand = iop.ReadString(false); //hmro
            RightHandTex = iop.ReadString(false); //HAND=huaolgt
            Head = iop.ReadString(false); //c013ma_pompa
            HeadTex = iop.ReadString(true); //HEAD-HEAD=c013malgt_pompa

            var pairsCount = iop.ReadInt32();
            Pairs = new string[pairsCount * 2];
            for (int i = 0; i < pairsCount; i++)
            {
                var key = iop.ReadString(false);
                var value = iop.ReadString(false);

                if (key != "" || value != "")
                {

                }

                Pairs[i * 2] = key;
                Pairs[i * 2 + 1] = value;
            }

            Animation = iop.ReadString(false); //a2o-idle-neutral-lhips-look-1c;1;1000;240;1000;0;1;1
            Unk3 = iop.ReadString(false); //
            BaseAnimation = iop.ReadString(true); //a2o-standing-loop;-10;1000;525;1000;1;1;1

            UnknownValue = iop.ReadInt32();
            if (UnknownValue != 3)
            {

            }

            FirstFloats = new float[9];

            for (int i = 0; i < 9; i++)
            {
                FirstFloats[i] = iop.ReadFloat();
            }

            MotiveDataOld = new float[16];

            for (int i = 0; i < 16; i++)
            {
                MotiveDataOld[i] = iop.ReadFloat();
            }

            MotiveData = new float[16];

            for (int i = 0; i < 16; i++)
            {
                MotiveData[i] = iop.ReadFloat();
            }

            // Unsure of the exact point this field gets larger.
            // Unleashed prebuilds are 0x45 and have 0x50 fields.
            // Field 0x50 is from Superstar.
            // Related to change in NBRS.
            int dataCount = version > 0x45 ? 0x100 : 0x50;

            PersonData = new short[dataCount];

            for (int i = 0; i < PersonData.Length; i++)
            {
                PersonData[i] = iop.ReadInt16();
            }

            int routingFrameCount = iop.ReadInt32();
        }
    }

    public struct OBJMSlot
    {
        public short Unknown;
        public short ObjectID;

        public OBJMSlot(short unk, short objId)
        {
            Unknown = unk;
            ObjectID = objId;
        }
    }

    public struct OBJMResource
    {
        public OBJD OBJD;
        public OBJTEntry OBJT;
    }

    public struct OBJMStackFrame
    {
        public short StackObjectID;
        public short TreeID;
        public short NodeID;
        public short[] Parameters;
        public short[] Locals;
        public int PrimitiveState;
        public short CodeOwnerObjType;

        public OBJMStackFrame(short stackObj, short treeID, short nodeID, short[] parameters, short[] locals, int unknown, short codeOwner)
        {
            StackObjectID = stackObj;
            TreeID = treeID;
            NodeID = nodeID;
            Parameters = parameters;
            Locals = locals;
            PrimitiveState = unknown;
            CodeOwnerObjType = codeOwner;
        }

        public OBJMStackFrame(IffFieldEncode iop)
        {
            StackObjectID = iop.ReadInt16();
            TreeID = iop.ReadInt16();
            NodeID = iop.ReadInt16();

            byte locals = iop.ReadByte();
            byte parameters = iop.ReadByte();

            Locals = new short[locals];
            Parameters = new short[parameters];

            for (int j = 0; j < parameters; j++)
            {
                Parameters[j] = iop.ReadInt16();
            }

            for (int j = 0; j < locals; j++)
            {
                Locals[j] = iop.ReadInt16();
            }

            PrimitiveState = iop.ReadInt32();
            CodeOwnerObjType = iop.ReadInt16(); // object type that owns the running code
        }
    }

    public struct OBJMRelationshipEntry
    {
        public int IsPresent;

        public int TargetID;
        public int[] Values;

        public OBJMRelationshipEntry(int isPresent, int targetID, int[] values)
        {
            IsPresent = isPresent;
            TargetID = targetID;
            Values = values;
        }

        public OBJMRelationshipEntry(IffFieldEncode iop)
        {
            IsPresent = iop.ReadInt32();

            if (IsPresent != 0)
            {
                TargetID = iop.ReadInt32();
                var relToCount = iop.ReadInt32();

                Values = new int[relToCount];

                for (int i = 0; i < relToCount; i++)
                {
                    Values[i] = iop.ReadInt32();
                }
            }
            else
            {
                TargetID = 0;
                Values = null;

                throw new Exception($"Unexpected IsPresent value: {IsPresent}");
            }
        }
    }
    
    public struct OBJMInstance
    {
        public const int TempCount = 8;
        public const int ObjectDataCount = 68;
        public const int ExtraDataCount = 5;

        public OBJD OBJD;
        public OBJTEntry OBJT;

        public OBJMFootprint Footprint;

        public int X;
        public int Y;
        public int Level;

        public short UnknownData;

        public short[] Attributes;
        public short[] TempRegisters;
        public short[] ObjectData;
        public short[] ObjectDataExtra;

        public int StackFlags; //512 usually
        public OBJMStackFrame[] Stack;

        public OBJMRelationshipEntry[] Relationships;

        /// <summary>
        /// Each tile has a linked list of objects on the same tile - this slot points to the next object.
        /// The first object in a tile is pointed to by the ARRY resource.
        /// </summary>
        public OBJMSlot LinkedSlot;

        /// <summary>
        /// A pair for each object container slot.
        /// Second field appears to be the contained object ID, unclear what first is (0 even when slot id non-zero)
        /// </summary>
        public OBJMSlot[] Slots;

        public bool[] DynamicSpriteFlags;

        /// <summary>
        /// Data present for multitile objects.
        /// </summary>
        public OBJMMultitile? MultitileData;

        /// <summary>
        /// Data present for portal type objects.
        /// </summary>
        public float[] PortalData;

        /// <summary>
        /// Data present for person type objects.
        /// </summary>
        public OBJMPerson? PersonData;

        public int UnknownInt;
        public string UnhandledData;

        public OBJMInstance(uint version, IffFieldEncode iop, long skipPosition, Func<short, OBJMResource> idToOBJD)
        {
            Footprint = new OBJMFootprint(iop);

            X = iop.ReadInt32();
            Y = iop.ReadInt32();

            Level = iop.ReadInt32();

            if (Level != 1 && Level != 2)
            {
                throw new Exception($"Unexpected level (should be 1/2): {Level}");
            }

            UnknownData = iop.ReadInt16();

            if (UnknownData != 1)
            {

            }

            short attrCount = iop.ReadInt16();
            Attributes = new short[attrCount];

            for (int i = 0; i < attrCount; i++)
            {
                Attributes[i] = iop.ReadInt16();
            }

            TempRegisters = new short[TempCount];

            for (int i = 0; i < TempCount; i++)
            {
                TempRegisters[i] = iop.ReadInt16();
            }

            ObjectData = new short[ObjectDataCount];

            for (int i = 0; i < ObjectDataCount; i++)
            {
                ObjectData[i] = iop.ReadInt16();
            }

            ObjectDataExtra = new short[ExtraDataCount];

            for (int i = 0; i < ExtraDataCount; i++)
            {
                ObjectDataExtra[i] = iop.ReadInt16();
            }

            var resources = idToOBJD(ObjectData[11]);
            OBJD = resources.OBJD;
            OBJT = resources.OBJT;

            int stackFrames = iop.ReadInt32();
            StackFlags = iop.ReadInt32();

            if (StackFlags != 512)
            {

            }

            Stack = new OBJMStackFrame[stackFrames];
            for (int i = 0; i < Stack.Length; i++)
            {
                Stack[i] = new OBJMStackFrame(iop);
            }

            Relationships = null;
            Slots = null;

            var relFlag = iop.ReadInt32();

            if (relFlag < 0)
            {
                var relCount = iop.ReadInt32();
                Relationships = new OBJMRelationshipEntry[relCount];

                for (int i = 0; i < relCount; i++)
                {
                    Relationships[i] = new OBJMRelationshipEntry(iop);
                }
            }
            else
            {
                throw new Exception($"Unknown relationship flag: {relFlag}");
            }

            var slotsCount = iop.ReadInt16();

            if (slotsCount < 0)
            {
                throw new Exception($"Unknown slots count: {slotsCount}");
            }

            Slots = new OBJMSlot[Math.Max(0, slotsCount - 1)];
            LinkedSlot = default;

            for (int i = 0; i < slotsCount; i++)
            {
                short unk = iop.ReadInt16();
                short objId = iop.ReadInt16();

                if (i == 0)
                {
                    LinkedSlot = new OBJMSlot(unk, objId);
                }
                else
                {
                    Slots[i-1] = new OBJMSlot(unk, objId);
                }

                if (unk != 0)
                {

                }
            }

            DynamicSpriteFlags = new bool[iop.ReadInt16()];

            for (int i = 0; i < DynamicSpriteFlags.Length; i++)
            {
                short flag = iop.ReadInt16();

                if (flag != 0 && flag != 1)
                {
                    throw new Exception($"Unexpected dynamic sprite flag {flag}");
                }

                DynamicSpriteFlags[i] = flag != 0;
            }

            MultitileData = null;

            if (OBJD != null && OBJD.IsMultiTile)
            {
                MultitileData = new OBJMMultitile(iop);
            }

            PortalData = null;
            if (OBJT.OBJDType == OBJDType.Portal)
            {
                int portalDataCount = iop.ReadInt32();
                PortalData = new float[portalDataCount];

                for (int i = 0; i < portalDataCount; i++)
                {
                    PortalData[i] = iop.ReadFloat();
                }
            }

            if (OBJT.OBJDType == OBJDType.Person)
            {
                //var test = iop.ReadString(true);
                PersonData = new OBJMPerson(version, iop);

                UnknownInt = 0;
            }
            else
            {
                PersonData = null;
                // "Placeholder" tends to go out of bounds here
                UnknownInt = iop.ReadInt32();
                //UnknownInt = 0;
            }

            UnhandledData = iop.BitDebugTil(skipPosition);
        }
    }

    public class OBJM : IffChunk
    {
        //work in progress

        private struct CompressedData
        {
            public int Offset;
            public byte[] Data;

            public CompressedData(int offset, byte[] data)
            {
                Offset = offset;
                Data = data;
            }
        }

        public uint Version;
        public Dictionary<ushort, ushort> IDToOBJT;
        private CompressedData[] CompressedObjectInstances;

        public Dictionary<int, MappedObject> ObjectData;

        public override void Read(IffFile iff, Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                io.ReadUInt32(); //pad
                Version = io.ReadUInt32();

                //house 00: 33 00 00 00
                //house 03: 3E 00 00 00
                //house 79: 45 00 00 00
                //completec:49 00 00 00
                //corresponds to house version?

                var MjbO = io.ReadUInt32();

                if (MjbO != 0x4f626a4d)
                {
                    throw new Exception("Invalid OBJM magic number");
                }

                // Offsets are from here.
                long offsetBase = io.Position;

                var compressionCode = io.ReadByte();
                if (compressionCode != 1) throw new Exception($"Currently expects OBJM to be compressed, but got compression code {compressionCode}");

                var iop = new IffFieldEncode(io);

                var table = new Dictionary<ushort, ushort>();
                while (io.HasMore)
                {
                    var id = iop.ReadUInt16();
                    if (id == 0) break;

                    var type = iop.ReadUInt16();
                    table[id] = type;
                }

                IDToOBJT = table;

                iop.Interrupt();

                int objCount = IDToOBJT.Count;
                CompressedObjectInstances = new CompressedData[objCount];

                for (int i = 0; i < objCount; i++)
                {
                    var skipOffset = io.ReadInt32();
                    long skipPosition = offsetBase + skipOffset;
                    int offset = (int)(io.Position - offsetBase);

                    CompressedObjectInstances[i] = new CompressedData(offset, io.ReadBytes((int)(skipPosition - io.Position)));
                }

                // The rest tends to be 0s, then ends with a3.
            }
        }

        public void Prepare(Func<ushort, OBJMResource> typeIdToResource)
        {
            var objects = new List<OBJMInstance>();
            ObjectData = new Dictionary<int, MappedObject>();

            Func<short, OBJMResource> idToResource = (short objID) =>
            {
                var typeID = IDToOBJT[(ushort)objID];

                return typeIdToResource(typeID);
            };

            foreach (var instance in CompressedObjectInstances)
            {
                using (var io = IoBuffer.FromBytes(instance.Data, ByteOrder.LITTLE_ENDIAN))
                {
                    var iop = new IffFieldEncode(io, (instance.Offset & 1) == 1);

                    var obj = new OBJMInstance(Version, iop, instance.Data.Length, idToResource);

                    objects.Add(obj);

                    var mapped = new MappedObject(obj);
                    ObjectData[mapped.ObjectID] = mapped;
                }
            }
        }

        private void ExperimentalInteractionReader(IffFieldEncode iop)
        {
            int id = iop.ReadInt32();
            short callerID = iop.ReadInt16();
            short targetObjID = iop.ReadInt16();
            short secondObjID = iop.ReadInt16(); //probably icon owner?
            int ttabActionID = iop.ReadInt32();

            var args = new short[4];

            for (int i = 0; i < 4; i++)
            {
                args[i] = iop.ReadInt16();
            }

            int priority = iop.ReadInt32();
            short actionTreeID = iop.ReadInt16();

            float unkFloat = iop.ReadFloat(); //0.3, 0.02...

            int unkInt = iop.ReadInt32();
            int unkZero = iop.ReadInt32();

            if (unkZero != 0)
            {

            }

            /*
            int unkID = iop.ReadInt32(); // Seems to increment each time, can be rather high. 129, 130
            */
        }

        public class MappedObject
        {
            public OBJMInstance Instance;

            // From instance
            // See VMStackObjectVariable
            public short ObjectID => Instance.ObjectData[11];
            public short Direction => Instance.ObjectData[1];
            public short ParentID => Instance.ObjectData[26];
            public int ContainerID => Instance.ObjectData[2];
            public int ContainerSlot => Instance.ObjectData[3];

            // From OBJT
            public string Name;
            public uint GUID;

            // From ARRY
            public int ArryX;
            public int ArryY;
            public int ArryLevel;

            public MappedObject(OBJMInstance instance)
            {
                Instance = instance;
            }

            public override string ToString()
            {
                return Name ?? "(unreferenced)";
            }
        }
    }
}
