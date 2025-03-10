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

    public struct OBJMMotiveDelta
    {
        public int Motive;
        public float TickDelta; // 30 ticks per minute, 60 ticks per hour
        public float StopAt;

        public OBJMMotiveDelta(IffFieldEncode iop)
        {
            Motive = iop.ReadInt32();
            TickDelta = iop.ReadFloat();
            StopAt = iop.ReadFloat();
        }
    }

    public struct OBJMInteraction
    {
        public int UID;
        public short CallerID;
        public short TargetID;
        public short Icon;
        public int TTAIndex;
        public short[] Args;
        public int Priority;
        public short ActionTreeID;
        public float Attenuation;

        // 1: unknown
        // 2: appears on group meal continuation
        // 4: user initiated? also appears for social interactions from other sim, but those use push interaction
        // 8: seems to randomly disappear (not on mourn/go here, is on "sit")
        // 16: appears on goto work, group meal continuation
        // 32: appears when the interaction becomes a "last interaction"?
        // 64: appears on goto work
        // 256: manually interrupted (not just priority override)
        public int Flags;

        public OBJMInteraction(IffFieldEncode iop)
        {
            int unkZero = iop.ReadInt32();
            if (unkZero != 0)
            {
                throw new Exception("Expected zero at start of interaction...");
            }

            UID = iop.ReadInt32();
            CallerID = iop.ReadInt16();
            TargetID = iop.ReadInt16();
            Icon = iop.ReadInt16();
            TTAIndex = iop.ReadInt32();

            Args = new short[4];
            for (int i = 0; i < 4; i++)
            {
                Args[i] = iop.ReadInt16();
            }

            Priority = iop.ReadInt32();
            ActionTreeID = iop.ReadInt16();
            Attenuation = iop.ReadFloat();
            Flags = iop.ReadInt32();
        }

        public bool IsValid()
        {
            return TTAIndex != -1;
        }
    }

    public struct OBJMAccessory
    {
        public string Name;
        public string Binding;

        public OBJMAccessory(IffFieldEncode iop)
        {
            Name = iop.ReadString(false);
            Binding = iop.ReadString(false); // Assuming this is the binding - it's usually blank.
        }
    }

    public struct OBJMObjectUse
    {
        public short TargetID;
        public int StackLength; // If the stack length falls below this, then the object is no longer in use.
        public byte Unknown2; // 1 when call functional tree?

        public OBJMObjectUse(IffFieldEncode iop)
        {
            TargetID = iop.ReadInt16();
            StackLength = iop.ReadInt32();
            Unknown2 = iop.ReadByte();

            if (Unknown2 != 0 && Unknown2 != 1)
            {

            }
        }
    }

    public struct OBJMPerson
    {
        // Number of events fired during the current animation.
        public int AnimEventCount;
        // 0 when routing or waiting for notify (can be interrupted), 1 otherwise.
        public int Engaged;

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
        public OBJMAccessory[] Accessories;
        public string Animation; //
        public string CarryAnimation; //
        public string BaseAnimation; //a2o-standing-loop;-10;1000;70;1000;1;1;1

        public int RoutingState;
        public float[] FirstFloats;
        public float[] MotiveDataOld;
        public float[] MotiveData;
        public short[] PersonData;
        public int RoutingFrameCount;

        public OBJMInteraction ActiveInteraction;
        public OBJMInteraction LastInteraction;
        public OBJMInteraction[] InteractionQueue;
        public OBJMObjectUse[] ObjectUses;
        public OBJMMotiveDelta[] MotiveDeltas;

        public OBJMPerson(uint version, IffFieldEncode iop)
        {
            AnimEventCount = iop.ReadInt32();
            Engaged = iop.ReadInt32();

            if (Engaged != 0 && Engaged != 1)
            {
                // Seems to be on/off, not sure if other values can appear.
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

            var accessoryCount = iop.ReadInt32();
            Accessories = new OBJMAccessory[accessoryCount];
            for (int i = 0; i < accessoryCount; i++)
            {
                Accessories[i] = new OBJMAccessory(iop);
            }

            // name;priority;speed (1/1000ths);frame;weight? (1/1000ths);loop;unk;unk
            // note: routing animation state is not saved
            Animation = iop.ReadString(false); //a2o-idle-neutral-lhips-look-1c;1;1000;240;1000;0;1;1
            CarryAnimation = iop.ReadString(false); //a2o-rarm-carry-loop;10;0;1000;1000;0;1;1
            BaseAnimation = iop.ReadString(true); //a2o-standing-loop;-10;1000;525;1000;1;1;1

            RoutingState = iop.ReadInt32();
            // Seems to be related to routing
            // 9: actively moving to dest?
            // 6: accelerating
            // 4: turning?
            // 3: stopped? this seems to linger when sims go to work
            // 0: no movement (maybe resets when scripted animation starts)

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

            RoutingFrameCount = iop.ReadInt32();

            ActiveInteraction = new OBJMInteraction(iop);
            LastInteraction = new OBJMInteraction(iop);

            var interactionQueueCount = iop.ReadInt32();

            InteractionQueue = new OBJMInteraction[interactionQueueCount];

            for (int i = 0; i < interactionQueueCount; i++)
            {
                InteractionQueue[i] = new OBJMInteraction(iop);
            }

            int useObjCount = iop.ReadInt32();

            ObjectUses = new OBJMObjectUse[useObjCount];
            for (int i = 0; i < useObjCount; i++)
            {
                ObjectUses[i] = new OBJMObjectUse(iop);
            }

            int motiveDeltas = iop.ReadInt32();
            MotiveDeltas = new OBJMMotiveDelta[motiveDeltas];

            for (int i = 0; i < motiveDeltas; i++)
            {
                MotiveDeltas[i] = new OBJMMotiveDelta(iop);
            }
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

        /// <summary>
        /// While each flag is essentially a boolean, the game _does_ store a short value for each flag...
        /// </summary>
        public short[] DynamicSpriteFlags;

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
                    Slots[i - 1] = new OBJMSlot(unk, objId);
                }

                if (unk != 0)
                {

                }
            }

            DynamicSpriteFlags = new short[iop.ReadInt16()];

            for (int i = 0; i < DynamicSpriteFlags.Length; i++)
            {
                DynamicSpriteFlags[i] = iop.ReadInt16();
            }

            MultitileData = null;

            // If we don't have the OBJD, assume the object is multitile if it has a blank name.
            if (OBJD != null ? OBJD.IsMultiTile : OBJT.Name == "")
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

                UnknownInt = iop.ReadInt32();
            }
            else
            {
                PersonData = null;
                // "Placeholder" tends to go out of bounds here
                UnknownInt = iop.ReadInt32();
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
