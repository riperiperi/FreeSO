using System;
using System.Collections.Generic;
using System.Linq;
using FSO.SimAntics.Engine.Scopes;
using FSO.Vitaboy;
using FSO.Files.Formats.IFF.Chunks;
using FSO.Content;
using FSO.SimAntics.Model;
using FSO.Files.Formats.OTF;

namespace FSO.SimAntics.Engine.Utils
{
    public class VMMemory
    {
        /// <summary>
        /// Get a variable
        /// </summary>
        /// <param name="context"></param>
        /// <param name="scope"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static short GetVariable(VMStackFrame context, VMVariableScope scope, short data)
        {
            switch (scope){
                case VMVariableScope.MyObjectAttributes: //0
                    return context.Caller.GetAttribute((ushort)data);

                case VMVariableScope.StackObjectAttributes: //1
                    return context.StackObject.GetAttribute((ushort)data);

                case VMVariableScope.TargetObjectAttributes: //2
                    throw new VMSimanticsException("Target Object is Deprecated!", context);

                case VMVariableScope.MyObject: //3
                    return context.Caller.GetValue((VMStackObjectVariable)data);

                case VMVariableScope.StackObject: //4
                    return context.StackObject.GetValue((VMStackObjectVariable)data);

                case VMVariableScope.TargetObject: //5
                    throw new VMSimanticsException("Target Object is Deprecated!", context);

                case VMVariableScope.Global: //6
                    return context.VM.GetGlobalValue((ushort)data);

                case VMVariableScope.Literal: //7
                    return data;

                case VMVariableScope.Temps: //8
                    return context.Thread.TempRegisters[data];

                case VMVariableScope.Parameters: //9
                    return context.Args[data];

                case VMVariableScope.StackObjectID: //10
                    return context.StackObjectID;

                case VMVariableScope.TempByTemp: //11
                    return context.Thread.TempRegisters[context.Thread.TempRegisters[data]];
                    
                case VMVariableScope.TreeAdRange: //12
                    return GetTreeAd(context, 1, (ushort)data);

                case VMVariableScope.StackObjectTemp: //13
                    return context.StackObject.Thread.TempRegisters[data];

                case VMVariableScope.MyMotives: //14
                    return ((VMAvatar)context.Caller).GetMotiveData((VMMotive)data);

                case VMVariableScope.StackObjectMotives: //15
                    return (context.StackObject as VMAvatar)?.GetMotiveData((VMMotive)data) ?? 0;

                case VMVariableScope.StackObjectSlot: //16
                    return context.StackObject.GetSlot(data)?.ObjectID ?? 0;

                case VMVariableScope.StackObjectMotiveByTemp: //17
                    return ((VMAvatar)context.StackObject).GetMotiveData((VMMotive)context.Thread.TempRegisters[data]);

                case VMVariableScope.MyPersonData: //18
                    return ((VMAvatar)context.Caller).GetPersonData((VMPersonDataVariable)data);

                case VMVariableScope.StackObjectPersonData: //19
                    return ((VMAvatar)context.StackObject).GetPersonData((VMPersonDataVariable)data);

                case VMVariableScope.MySlot: //20
                    return context.Caller.GetSlot(data)?.ObjectID ?? 0;

                case VMVariableScope.StackObjectDefinition: //21
                    return GetEntityDefinitionVar(context.StackObject.Object.OBJ, (VMOBJDVariable)data, context);

                case VMVariableScope.StackObjectAttributeByParameter: //22
                    return context.StackObject.GetAttribute((ushort)context.Args[data]);

                case VMVariableScope.RoomByTemp0: //23
                    //returns information on the selected room. Right now we don't have a room system, so always return the same values. (everywhere is indoors, not a pool)
                    var roomID = Math.Max(0, Math.Min(context.VM.Context.RoomInfo.Length-1, context.Thread.TempRegisters[0] + 1));
                    var room = context.VM.Context.RoomInfo[roomID];
                    var baseroom = context.VM.Context.RoomInfo[room.Room.LightBaseRoom];

                    if (data == 0) return 100; //ambient light 0-100
                    else if (data == 1) return (short)((baseroom.Room.IsOutside)?1:0); //outside
                    else if (data == 2) return (short)(baseroom.Room.Floor); //level
                    else if (data == 3) return (short)baseroom.Room.Area; //area (???)
                    else if (data == 4) return (short)(room.Room.IsPool?1:0); //is pool
                    else throw new VMSimanticsException("Invalid room data!", context);

                    //throw new VMSimanticsException("Not implemented...");

                case VMVariableScope.NeighborInStackObject: //24
                    if (!context.VM.TS1) throw new VMSimanticsException("Only valid in TS1.", context);
                    var neighbor = Content.Content.Get().Neighborhood.GetNeighborByID(context.StackObjectID);
                    var fam = neighbor?.PersonData?.ElementAt((int)VMPersonDataVariable.TS1FamilyNumber);

                    FAMI fami = null;
                    if (fam != null)
                        fami = Content.Content.Get().Neighborhood.GetFamily((ushort)fam.Value);
                    if (neighbor == null) return 0;
                    switch (data)
                    {
                        case 0: //instance id
                            //find neighbour in the lot
                            return context.VM.Context.ObjectQueries.Avatars.FirstOrDefault(x => x.Object.GUID == neighbor.GUID)?.ObjectID ?? 0;
                        case 1: //belongs in house
                            return (short)((context.VM.TS1State.CurrentFamily == fami) ? 1:0); //uh, okay.
                        case 2: //person age
                            return neighbor.PersonData?.ElementAt((int)VMPersonDataVariable.PersonsAge) ?? 0;
                        case 3: //relationship raw score
                                //to this person or from? what
                            return 0; //unused in favor of primitive?
                        case 4: //relationship score
                            return 0; //unused in favor of primitive?
                        case 5: //friend count
                            return (short)neighbor.Relationships.Count(n => {
                                if (n.Value[0] >= 50)
                                {
                                    var othern = Content.Content.Get().Neighborhood.GetNeighborByID((short)n.Key);
                                    if (othern != null)
                                    {
                                        List<short> orels;
                                        if (othern.Relationships.TryGetValue(context.StackObjectID, out orels))
                                        {
                                            return orels[0] >= 50;
                                        }
                                    }
                                }
                                return false;
                                }); //interaction - nag friends TEST
                        case 6: //house number
                            return (short)(fami?.HouseNumber ?? 0);
                        case 7: //has telephone
                            return 1;
                        case 8: //has baby
                            return 0;
                        case 9: //family friend count
                            return (short)(fami?.FamilyFriends ?? 0);
                    }
                    throw new VMSimanticsException("Neighbor data out of bounds.", context);
                case VMVariableScope.Local: //25
                    return (short)context.Locals[data];

                case VMVariableScope.Tuning: //26
                    return GetTuningVariable(context.Callee, (ushort)data, context);

                case VMVariableScope.DynSpriteFlagForTempOfStackObject: //27
                    return context.StackObject.IsDynamicSpriteFlagSet((ushort)context.Thread.TempRegisters[data]) ? (short)1 : (short)0;

                case VMVariableScope.TreeAdPersonalityVar: //28
                    return GetTreeAd(context, 2, (ushort)data);

                case VMVariableScope.TreeAdMin: //29
                    return GetTreeAd(context, 0, (ushort)data);

                case VMVariableScope.MyPersonDataByTemp: //30
                    return ((VMAvatar)context.Caller).GetPersonData((VMPersonDataVariable)(context.Thread.TempRegisters[data]));

                case VMVariableScope.StackObjectPersonDataByTemp: //31
                    return ((VMAvatar)context.StackObject).GetPersonData((VMPersonDataVariable)(context.Thread.TempRegisters[data]));

                case VMVariableScope.NeighborPersonData: //32
                    if (!context.VM.TS1) throw new VMSimanticsException("Only valid in TS1.", context);
                    return Content.Content.Get().Neighborhood.GetNeighborByID(context.StackObjectID)?.PersonData?.ElementAt(data) ?? 0;

                case VMVariableScope.JobData: //33 jobdata(temp0, temp1), used a few times to test if a person is at work but that isn't relevant for tso...
                    if (!context.VM.TS1) throw new VMSimanticsException("Only valid in TS1.", context);
                    return Content.Content.Get().Jobs.GetJobData((ushort)context.Thread.TempRegisters[0], context.Thread.TempRegisters[1], data);

                case VMVariableScope.NeighborhoodData: //34
                    return 0; //tutorial values only
                    throw new VMSimanticsException("Should not be used, but if this shows implement an empty shell to return ideal values.", context);

                case VMVariableScope.StackObjectFunction: //35
                    return (short)context.StackObject.EntryPoints[data].ActionFunction;

                case VMVariableScope.MyTypeAttr: //36
                    if (context.VM.TS1) return Content.Content.Get().Neighborhood.GetTATT((context.Caller.MasterDefinition ?? context.Caller.Object.OBJ).TypeAttrGUID, data);
                    return 0;
                
                case VMVariableScope.StackObjectTypeAttr: //37
                    if (context.VM.TS1) return Content.Content.Get().Neighborhood.GetTATT((context.StackObject.MasterDefinition ?? context.StackObject.Object.OBJ).TypeAttrGUID, data);
                    return 0;

                case VMVariableScope.NeighborsObjectDefinition: //38
                    if (!context.VM.TS1) throw new VMSimanticsException("Only valid in TS1.", context);
                    var neighbor2 = Content.Content.Get().Neighborhood.GetNeighborByID(context.StackObjectID);
                    if (neighbor2 == null) return 0;
                    var objd = Content.Content.Get().WorldObjects.Get(neighbor2.GUID)?.OBJ;
                    if (objd == null) return 0;
                    return GetEntityDefinitionVar(objd, (VMOBJDVariable)data, context);

                case VMVariableScope.Unused:
                    return context.VM.TuningCache.GetLimit((VMMotive)data);

                case VMVariableScope.LocalByTemp: //40
                    return (short)context.Locals[context.Thread.TempRegisters[data]];

                case VMVariableScope.StackObjectAttributeByTemp: //41
                    return context.StackObject.GetAttribute((ushort)context.Thread.TempRegisters[data]);
                    
                case VMVariableScope.TempXL: //42
                    //this needs a really intricate special case for specific operations.
                    throw new VMSimanticsException("Caller function does not support TempXL!", context);
                case VMVariableScope.TSOStandardTime: //44
                    //return GetTSOStandardTime(data)
                    var time = context.VM.Context.Clock.UTCNow;

                    switch (data)
                    {
                        case 0:
                            return (short)time.Second;
                        case 1:
                            return (short)time.Minute;
                        case 2:
                            return (short)time.Hour;
                        case 3:
                            return (short)time.Day;
                        case 4:
                            return (short)time.Month;
                        case 5:
                            return (short)time.Year;
                    };
                    return 0;

                case VMVariableScope.CityTime: //43
                case VMVariableScope.GameTime: //45
                    switch (data)
                    {
                        case 0:
                            return (short)context.VM.Context.Clock.Seconds;
                        case 1:
                            return (short)context.VM.Context.Clock.Minutes;
                        case 2:
                            return (short)context.VM.Context.Clock.Hours;
                        case 3:
                            return (short)context.VM.Context.Clock.TimeOfDay;
                        case 4:
                            return (short)context.VM.Context.Clock.DayOfMonth;
                        case 5:
                            return (short)context.VM.Context.Clock.Month;
                        case 6:
                            return (short)context.VM.Context.Clock.Year;

                    };
                    break;

                case VMVariableScope.MyList: //46
                    switch (data)
                    {
                        case 0: return context.Caller.MyList.First.Value; //is this allowed?
                        case 1: return context.Caller.MyList.Last.Value;
                        case 2: return (short)context.Caller.MyList.Count;
                        default: return context.Caller.MyList.ElementAt(context.Thread.TempRegisters[0]);
                    }
                case VMVariableScope.StackObjectList: //47
                    if (context.StackObject == null) return 0;
                    switch (data)
                    {
                        case 0: return context.StackObject.MyList.First.Value;
                        case 1: return context.StackObject.MyList.Last.Value;
                        case 2: return (short)context.StackObject.MyList.Count;
                        default: return context.StackObject.MyList.ElementAt(context.Thread.TempRegisters[0]);
                    }

                case VMVariableScope.MoneyOverHead32Bit: //48
                    //we're poor... will need special case for this in expression like TempXL
                    return 0;

                case VMVariableScope.MyLeadTileAttribute: //49
                    return context.Caller.MultitileGroup.BaseObject.GetAttribute((ushort)data);

                case VMVariableScope.StackObjectLeadTileAttribute: //50
                    return context.StackObject.MultitileGroup.BaseObject.GetAttribute((ushort)data);

                case VMVariableScope.MyLeadTile: //51
                    return context.Caller.MultitileGroup.BaseObject.ObjectID;

                case VMVariableScope.StackObjectLeadTile: //52
                    return context.StackObject.MultitileGroup.BaseObject.ObjectID;

                case VMVariableScope.StackObjectMasterDef: //53
                    //gets definition of the master tile of a multi tile object in the stack object.
                    var masterDef = context.StackObject.MasterDefinition;
                    return GetEntityDefinitionVar((masterDef == null)?context.StackObject.Object.OBJ:masterDef, (VMOBJDVariable)data, context);

                case VMVariableScope.FeatureEnableLevel: //54
                    return 1;
                    //all of them are enabled, dont really care right now

                case VMVariableScope.MyAvatarID: //59
                    uint myPID;
                    if (data < 2) myPID = context.Caller.PersistID;
                    else myPID = context.StackObject.PersistID;
                    switch (data)
                    {
                        case 0:
                        case 2:
                            return (short)myPID;
                        case 1:
                        case 3:
                            return (short)(myPID >> 16);
                        default: return 0;
                    }

            }
            throw new VMSimanticsException("Unknown get variable", context);
        }

        public static LinkedList<short> GetList(VMStackFrame context, VMVariableScope scope)
        {
            switch (scope)
            {
                case VMVariableScope.MyList:
                    return context.Caller.MyList;
                case VMVariableScope.StackObjectList:
                    return context.StackObject.MyList;
                default:
                    throw new VMSimanticsException("Cannot get specified variable scope as a list.", context);
            }
        }

        public static int GetBigVariable(VMStackFrame context, VMVariableScope scope, short data) //used by functions which can take 32 bit integers, such as VMExpression.
        {
            switch (scope)
            {
                case VMVariableScope.TempXL:
                    return context.Thread.TempXL[data];
                default:
                    return GetVariable(context, scope, data); //return a normal var
            }
        }

        private static ushort[] TableIDOffsets = new ushort[]{
            4096,
            8192,
            256
        };

        public static short GetTuningVariable(VMEntity entity, ushort data, VMStackFrame context) {
            var tableID = (ushort)(data >> 7);
            var keyID = (ushort)(data & 0x7F);

            int mode = 0;
            if (tableID < 64) mode = 0;
            else if (tableID < 128) { tableID = (ushort)((tableID - 64)); mode = 1; }
            else if (tableID < 192) { tableID = (ushort)((tableID - 128)); mode = 2; }

            BCON bcon;
            OTFTable tuning;

            /** This could be in a BCON or an OTF **/
            tableID = (ushort)(tableID + TableIDOffsets[mode]);

            var replacement = entity.TuningReplacement?.TryGetEntry(tableID, keyID);
            if (replacement.HasValue) return replacement.Value;
            /*
            var dyn = context.VM.Tuning;
            if (dyn != null) {
                string name = "object";
                switch (mode)
                {
                    case 0: //local
                        name = context.ScopeResource.MainIff.Filename; break;
                    case 1: //semi globals
                        name = context.ScopeResource.SemiGlobal?.Iff?.Filename ?? "unknown"; break;
                    case 2: //semi globals
                        name = "global.iff"; break;
                }
                var replacement = dyn.GetTuning(name, tableID, keyID);
                if (replacement != null) return (short)replacement;
            }
            */

            uint targID = ((uint)tableID << 16) | keyID;
            Dictionary<uint, short> tuningCache = null;

            switch (mode) {
                case 0: //local
                    tuningCache = context.ScopeResource.TuningCache;
                    break;
                case 1: //semi globals
                    if (context.ScopeResource.SemiGlobal != null)
                        tuningCache = context.ScopeResource.SemiGlobal.TuningCache;
                    else
                        tuningCache = context.ScopeResource.TuningCache;
                    break;
                case 2: //global
                    tuningCache = context.Global.Resource.TuningCache;
                    break;
            }

            short value;
            if (tuningCache.TryGetValue(targID, out value)) return value;
            //throw new Exception("Could not find tuning constant!");
            return 0;
        }


        public static string GetTuningVariableLabel(GameObject obj, ushort data)
        {
            var tableID = (ushort)(4096 + (data >> 7));
            var keyID = (ushort)(data & 0x7F);

            /** This could be in a BCON or an OTF **/
            var bcon = obj.Resource.Get<BCON>(tableID);
            if (bcon != null)
            {
                return "bcon." + data;
            }

            var tuning = obj.Resource.Get<OTFTable>(tableID);
            if (tuning != null)
            {
                return tuning.GetKey(keyID).Label;
            }

            return "unknown, probably global or semiglobal";
            //throw new VMSimanticsException("Could not find tuning constant!");
        }

        //hilariously large switch case. there's got to be a better way
        public static short GetEntityDefinitionVar(OBJD objd, VMOBJDVariable var, VMStackFrame context){
            switch (var)
            {
                case VMOBJDVariable.Version1:
                    return (short)(objd.Version%0xFFFF);
                case VMOBJDVariable.Version2:
                    return (short)(objd.Version>>16);
                default:
                    return (short)objd.RawData[((int)var) - 2];
            }
        }
            
        /// <summary>
        /// Set a variable
        /// </summary>
        /// <param name="context"></param>
        /// <param name="scope"></param>
        /// <param name="data"></param>
        /// <param name="value"></param>
        public static bool SetVariable(VMStackFrame context, VMVariableScope scope, short data, short value){
            switch (scope){
                case VMVariableScope.MyObjectAttributes: //0
                    context.Caller.SetAttribute((ushort)data, value);
                    return true;

                case VMVariableScope.StackObjectAttributes: //1
                    context.StackObject.SetAttribute((ushort)data, value);
                    return true;

                case VMVariableScope.TargetObjectAttributes: //2
                    throw new VMSimanticsException("Target Object is Deprecated!", context);

                case VMVariableScope.MyObject: //3
                    return context.Caller.SetValue((VMStackObjectVariable)data, value);

                case VMVariableScope.StackObject: //4
                    return context.StackObject.SetValue((VMStackObjectVariable)data, value);

                case VMVariableScope.TargetObject: //5
                    throw new VMSimanticsException("Target Object is Deprecated!", context);

                case VMVariableScope.Global: //6
                    return context.VM.SetGlobalValue((ushort)data, value);

                case VMVariableScope.Literal: //7
                    /** Huh? **/
                    return false;

                case VMVariableScope.Temps: //8
                    context.Thread.TempRegisters[data] = value;
                    return true;

                case VMVariableScope.Parameters: //9
                    context.Args[data] = value;
                    return true;

                case VMVariableScope.StackObjectID: //10
                    /** Change the stack object **/
                    context.StackObjectID = value;
                    return true;

                case VMVariableScope.TempByTemp: //11
                    context.Thread.TempRegisters[context.Thread.TempRegisters[data]] = value;
                    return true;

                case VMVariableScope.TreeAdRange: //12
                    return SetTreeAd(context, 1, (ushort)data, value);

                case VMVariableScope.StackObjectTemp: //13
                    context.StackObject.Thread.TempRegisters[data] = value;
                    return true;

                case VMVariableScope.MyMotives: //14
                    return ((VMAvatar)context.Caller).SetMotiveData((VMMotive)data, value);

                case VMVariableScope.StackObjectMotives: //15
                    return (context.StackObject as VMAvatar)?.SetMotiveData((VMMotive)data, value) ?? false;

                case VMVariableScope.StackObjectSlot: //16
                    throw new VMSimanticsException("Not implemented...", context);

                case VMVariableScope.StackObjectMotiveByTemp: //17
                    return ((VMAvatar)context.StackObject).SetMotiveData((VMMotive)context.Thread.TempRegisters[data], value);

                case VMVariableScope.MyPersonData: //18
                    return ((VMAvatar)context.Caller).SetPersonData((VMPersonDataVariable)data, value);

                case VMVariableScope.StackObjectPersonData: //19
                    return ((VMAvatar)context.StackObject).SetPersonData((VMPersonDataVariable)data, value);

                case VMVariableScope.MySlot: //20
                    throw new VMSimanticsException("Not implemented...", context);

                case VMVariableScope.StackObjectDefinition: //21
                    return false; //you can't set this!

                case VMVariableScope.StackObjectAttributeByParameter: //22
                    context.StackObject.SetAttribute((ushort)context.Args[data], value);
                    return true;

                case VMVariableScope.RoomByTemp0: //23
                    throw new VMSimanticsException("Not implemented...", context);

                case VMVariableScope.NeighborInStackObject: //24
                    throw new VMSimanticsException("Not implemented...", context);

                case VMVariableScope.Local: //25
                    context.Locals[data] = value;
                    return true;

                case VMVariableScope.Tuning: //26
                    return false; //you can't set this!

                case VMVariableScope.DynSpriteFlagForTempOfStackObject: //27
                    context.StackObject.SetDynamicSpriteFlag((ushort)context.Thread.TempRegisters[data], value > 0);
                    return true;

                case VMVariableScope.TreeAdPersonalityVar: //28
                    return SetTreeAd(context, 2, (ushort)data, value);

                case VMVariableScope.TreeAdMin: //29
                    return SetTreeAd(context, 0, (ushort)data, value);

                case VMVariableScope.MyPersonDataByTemp: //30
                    return ((VMAvatar)context.Caller).SetPersonData((VMPersonDataVariable)context.Thread.TempRegisters[data], value);

                case VMVariableScope.StackObjectPersonDataByTemp: //31
                    return ((VMAvatar)context.StackObject).SetPersonData((VMPersonDataVariable)context.Thread.TempRegisters[data], value);

                case VMVariableScope.NeighborPersonData: //32
                    throw new VMSimanticsException("Not implemented...", context);

                case VMVariableScope.JobData: //33
                    throw new VMSimanticsException("Not implemented...", context);

                case VMVariableScope.NeighborhoodData: //34
                    throw new VMSimanticsException("Not implemented...", context);

                case VMVariableScope.StackObjectFunction: //35
                    return false; //you can't set this!

                case VMVariableScope.MyTypeAttr: //36
                    if (context.VM.TS1) Content.Content.Get().Neighborhood.SetTATT((context.Caller.MasterDefinition ?? context.Caller.Object.OBJ).TypeAttrGUID, data, value);
                    return true;

                case VMVariableScope.StackObjectTypeAttr: //37
                    if (context.VM.TS1) Content.Content.Get().Neighborhood.SetTATT((context.StackObject.MasterDefinition ?? context.StackObject.Object.OBJ).TypeAttrGUID, data, value);
                    return true;

                case VMVariableScope.NeighborsObjectDefinition: //38
                    return false; //you can't set this!

                case VMVariableScope.LocalByTemp: //40
                    context.Locals[context.Thread.TempRegisters[data]] = value;
                    return true;

                case VMVariableScope.StackObjectAttributeByTemp: //41
                    context.StackObject.SetAttribute((ushort)context.Thread.TempRegisters[data], value);
                    return true;

                case VMVariableScope.TempXL: //42
                    throw new VMSimanticsException("Not implemented...", context);
                    //this will need a special case for the expression primitive

                case VMVariableScope.CityTime: //43
                case VMVariableScope.TSOStandardTime: //44
                case VMVariableScope.GameTime: //45
                    return false; //you can't set this!

                case VMVariableScope.MyList: //46
                    throw new VMSimanticsException("Not implemented...", context);

                case VMVariableScope.StackObjectList: //47
                    throw new VMSimanticsException("Not implemented...", context);

                case VMVariableScope.MoneyOverHead32Bit: //48
                    //throw new VMSimanticsException("Not implemented...", context);
                    return true;
                    //needs special case like TempXL.

                case VMVariableScope.MyLeadTileAttribute: //49
                    context.Caller.MultitileGroup.BaseObject.SetAttribute((ushort)data, value);
                    return true;

                case VMVariableScope.StackObjectLeadTileAttribute: //50
                    context.StackObject.MultitileGroup.BaseObject.SetAttribute((ushort)data, value);
                    return true;

                case VMVariableScope.MyLeadTile: //51
                    return false; //can't set object ID.
                case VMVariableScope.StackObjectLeadTile: //52
                    return false; //can't set object ID.
                case VMVariableScope.StackObjectMasterDef: //53
                    return false;

                case VMVariableScope.FeatureEnableLevel: //54
                    throw new VMSimanticsException("Not implemented...", context);

                case VMVariableScope.MyAvatarID: //59
                    return false; //you can't set this!
                    
                default:
                    throw new VMSimanticsException("Unknown scope for set variable!", context);
            }
        }

        public static bool SetBigVariable(VMStackFrame context, VMVariableScope scope, short data, int value)
        {
            switch (scope)
            {
                case VMVariableScope.TempXL:
                    context.Thread.TempXL[data] = value;
                    return true;
                case VMVariableScope.MoneyOverHead32Bit:
                    ((VMAvatar)context.Caller).ShowMoneyHeadline(value);
                    return true;
                default:
                    return SetVariable(context, scope, data, (short)value); //truncate value and set the relevant 16 bit var to it.
            }
        }

        /// <summary>
        /// Get an animation
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static Animation GetAnimation(VMStackFrame context, VMAnimationScope scope, ushort id){

            STR animTable = null;
            bool child = ((VMAvatar)context.Caller).GetPersonData(VMPersonDataVariable.PersonsAge) < 18 && context.VM.TS1;
            //a2o, c2o, a2c, c2a

            switch (scope){
                case VMAnimationScope.Object:
                    var obj = context.CodeOwner;
                    var anitableID = (ushort)(obj.OBJ.AnimationTableID + (child ? 1 : 0));
                    animTable = obj.Resource.Get<STR>(anitableID);
                    if (animTable == null) animTable = obj.Resource.Get<STR>((ushort)(child ? 130 : 129));
                    break;
                case VMAnimationScope.Misc:
                    animTable = context.Global.Resource.Get<STR>((ushort)(child ? 157 : 156));
                    break;
                case VMAnimationScope.PersonStock:
                    animTable = context.Global.Resource.Get<STR>(130);
                    break;
                case VMAnimationScope.Global:
                    animTable = context.Global.Resource.Get<STR>(128);
                    break;
                case VMAnimationScope.StackObject:
                    var obj2 = context.StackObject.Object;
                    var anitableID2 = (ushort)(obj2.OBJ.AnimationTableID + (child ? 1 : 0));
                    animTable = obj2.Resource.Get<STR>(anitableID2);
                    if (animTable == null) animTable = obj2.Resource.Get<STR>((ushort)(child ? 130 : 129));
                    break;
            }

            if (animTable == null){
                return null;
            }

            var animationName = animTable.GetString(id);
            if (animationName != null) return FSO.Content.Content.Get().AvatarAnimations.Get(animationName + ".anim");
            else return null;
        }

        public static Appearance GetSuit(VMStackFrame context, VMSuitScope scope, ushort id){
            switch (scope)
            {
                case VMSuitScope.Object:
                    var suitTable = context.Callee.Object.Resource.Get<STR>(304);
                    if (suitTable != null){
                        var suitFile = suitTable.GetString(id) + ".apr";

                        var apr = FSO.Content.Content.Get().AvatarAppearances.Get(suitFile);
                        return apr;
                    }
                    return null;
                default:
                    return null;
                    throw new VMSimanticsException("I dont know about this suit scope", context);
            }
        }

        public static SLOTItem GetSlot(VMStackFrame context, VMSlotScope scope, ushort data)
        {
            switch (scope){
                case VMSlotScope.Global:
                    var slots = context.Global.Resource.Get<SLOT>(100);
                    if (slots != null && slots.Slots.ContainsKey(3) && data < slots.Slots[3].Count){
                        if (data >= slots.Slots[3].Count) return null;
                        var slot = slots.Slots[3][data];
                        return slot;
                    }
                    return null;
                case VMSlotScope.Literal:
                    if (data >= context.StackObject.Slots.Slots[3].Count) return null;
                    return context.StackObject.Slots.Slots[3][data];
                case VMSlotScope.StackVariable:
                    if (context.Args[data] >= context.StackObject.Slots.Slots[3].Count) return null;
                    return context.StackObject.Slots.Slots[3][context.Args[data]];
            }
            return null;
        }

        public static short GetTreeAd(VMStackFrame context, int type, ushort data)
        {
            // first - try to get the tree ad we've set
            var adDict = context.Thread.MotiveAdChanges;
            short result;
            if (adDict != null && adDict.TryGetValue((type << 16) | data, out result))
            {
                return result;
            }
            // otherwise find it in the currently active action
            // be prepared for some overly complex garbage
            var action = context.Thread.ActiveAction;
            if (action == null || data < 0) return 0;
            var tree = action.Callee.TreeTable;
            if (tree == null) return 0;
            TTABInteraction interaction;
            if (!tree.InteractionByIndex.TryGetValue((uint)action.InteractionNumber, out interaction)) return 0;
            if (data >= interaction.MotiveEntries.Length) return 0;
            var entry = interaction.MotiveEntries[data];
            switch (type)
            {
                case 0:
                    return entry.EffectRangeMinimum;
                case 1:
                    return entry.EffectRangeDelta;
                case 2:
                    return (short)entry.PersonalityModifier;
            }
            return 0;
        }

        public static bool SetTreeAd(VMStackFrame context, int type, ushort data, short value)
        {
            if (context.Thread.MotiveAdChanges == null)
            {
                context.Thread.MotiveAdChanges = new Dictionary<int, short>();
            }
            context.Thread.MotiveAdChanges[(type << 16) | data] = value;
            return true;
        }
    }
}
