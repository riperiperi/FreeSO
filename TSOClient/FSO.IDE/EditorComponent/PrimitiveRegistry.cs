using FSO.IDE.EditorComponent.Model;
using FSO.IDE.EditorComponent.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.IDE.EditorComponent
{
    public static class PrimitiveRegistry
    {
        private static Dictionary<byte, Type> DescriptorById = new Dictionary<byte, Type>()
        {
            {0, typeof(SleepDescriptor) },
            {1, typeof(GenericTSOCallDescriptor) },
            {2, typeof(ExpressionDescriptor) },
            {4, typeof(GrabDescriptor) },
            {5, typeof(DropDescriptor) },
            {6, typeof(ChangeSuitDescriptor) },
            {7, typeof(RefreshDescriptor) },
            {8, typeof(RandomNumberDescriptor) },
            {11, typeof(GetDistanceToDescriptor) },
            {12, typeof(GetDirectionToDescriptor) },
            {13, typeof(PushInteractionDescriptor) },
            {14, typeof(FindBestObjectForFunctionDescriptor) },
            {15, typeof(BreakpointDescriptor) },
            {16, typeof(FindLocationForDescriptor) },
            {17, typeof(IdleForInputDescriptor) },
            {18, typeof(RemoveObjectInstanceDescriptor) },
            {20, typeof(RunFunctionalTreeDescriptor) },
            {22, typeof(LookTowardsDescriptor) },
            {23, typeof(PlaySoundEventDescriptor) },
            {24, typeof(OldRelationshipDescriptor) },
            {25, typeof(TransferFundsDescriptor) },
            {26, typeof(RelationshipDescriptor) },
            {27, typeof(GotoRelativePositionDescriptor) },
            {28, typeof(RunTreeByNameDescriptor) },
            {29, typeof(SetMotiveChangeDescriptor) },
            {31, typeof(SetToNextDescriptor) },
            {32, typeof(TestObjectTypeDescriptor) },
            {36, typeof(DialogDescriptors) },
            {38, typeof(DialogDescriptors) },
            {39, typeof(DialogDescriptors) },
            {41, typeof(SetBalloonHeadlineDescriptor) },
            {42, typeof(CreateObjectInstanceDescriptor) },
            {43, typeof(DropOntoDescriptor) },
            {44, typeof(AnimateSimDescriptor) },
            {45, typeof(GotoRoutingSlotDescriptor) },
            {46, typeof(SnapDescriptor) },
            {49, typeof(NotifyOutOfIdleDescriptor) },
            {50, typeof(ChangeActionStringDescriptor) },
            {51, typeof(TS1InventoryOperationsDescriptor) },
            {62, typeof(InvokePluginDescriptor) },

            {67, typeof(TSOInventoryOperationsDescriptor) }
        };

        public static Dictionary<PrimitiveGroup, List<byte>> PrimitiveGroups = new Dictionary<PrimitiveGroup, List<byte>>
        {
            {PrimitiveGroup.Subroutine, new List<byte>()},
            {PrimitiveGroup.Control, new List<byte> {
                2, //expression
                1, //generic sims online call
                0, //sleep,
                17, //idle for input,
                49, //notify stack object out of idle
                13, //push interaction
                14, //find best object for function
                20, //run functional tree
                28, //run tree by name
                50, //add/change the action string
            }
            },
            {PrimitiveGroup.Debug, new List<byte> {
                15, //breakpoint
                3, //report metric
                21, //show string
                30, //syslog
            } },
            {PrimitiveGroup.Math, new List<byte> {
                2, //expression
                8, //random number
                11, //get distance to (should these be in position?)
                12, //get direction to
            } },
            {PrimitiveGroup.Sim, new List<byte> {
                44, //animate sim
                6, //change suit
                22, //look towards
                25, //transfer funds
                29, //set motive change
                33, //find 5 worst motives
                65, //find best action
                37, //test sim interacting with
                47, //reach

                51, //ts1 inventory
            } },
            {PrimitiveGroup.Object, new List<byte> {
                42, //create new object instance
                18, //remove object instance
                31, //set to next
                32, //test object type
                24, //old relationship
                26, //relationship
                66, //set dynamic object name
                9, //burn
            } },
            {PrimitiveGroup.Looks, new List<byte> {
                41, //set balloon/headline
                7, //refresh
                34, //ui effect
                35, //special effect
                23, //play sound event
                48, //stop all sounds
                36, //dialog - all strings (hacked)
                38,
                39
            } },
            {PrimitiveGroup.Position, new List<byte> {
                16, //find location for
                27, //go to relative position
                45, //go to routing slot
                46, //snap
                4, //grab
                5, //drop
                43, //drop onto
                63, //get terrain info
            } },
            {PrimitiveGroup.TSO, new List<byte> {
                61, //send maxis letter
                62, //invoke plugin
                67, //inventory operations
                40, //onlinejobscall
            } },
        };

        public static Dictionary<byte, PrimitiveGroup> ReverseGroup; //used for primitives without definitions.

        public static PrimitiveGroup GetGroupOf(byte id)
        {
            if (ReverseGroup == null)
            {
                ReverseGroup = new Dictionary<byte, PrimitiveGroup>();
                ReverseGroup.Add(38, PrimitiveGroup.Looks);
                ReverseGroup.Add(39, PrimitiveGroup.Looks);
                foreach (var group in PrimitiveGroups)
                {
                    foreach (var pid in group.Value)
                    {
                        if (!ReverseGroup.ContainsKey(pid)) ReverseGroup.Add(pid, group.Key);
                    }
                }
            }
            return (ReverseGroup.ContainsKey(id))?ReverseGroup[id]:PrimitiveGroup.Unknown;
        }

        public static PrimitiveDescriptor GetDescriptor(ushort id)
        {
            if (id >= 256)
            {
                return new SubroutineDescriptor { PrimID = id };
            }
            else
            {
                if (!DescriptorById.ContainsKey((byte)id)) return new UnknownPrimitiveDescriptor { PrimID = id };
                else
                {
                    var desc = (PrimitiveDescriptor)Activator.CreateInstance(DescriptorById[(byte)id]);
                    desc.PrimID = id;
                    return desc;
                }
            }
        }
    }
}
