using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Simantics.engine.scopes;
using TSO.Vitaboy;
using TSO.Files.formats.iff.chunks;
using TSO.Content;
using TSO.Simantics.model;
using TSO.Files.formats.otf;

namespace TSO.Simantics.engine.utils
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
        public static short GetVariable(VMStackFrame context, VMVariableScope scope, ushort data)
        {
            switch (scope){
                case VMVariableScope.MyObjectAttributes: //0
                    return context.Caller.GetAttribute(data);

                case VMVariableScope.StackObjectAttributes: //1
                    return context.StackObject.GetAttribute(data);

                case VMVariableScope.TargetObjectAttributes: //2
                    throw new VMSimanticsException("Target Object is Deprecated!", context);

                case VMVariableScope.MyObject: //3
                    if ((VMStackObjectVariable)data == VMStackObjectVariable.GroupID) ;
                    return context.Caller.GetValue((VMStackObjectVariable)data);

                case VMVariableScope.StackObject: //4
                    return context.StackObject.GetValue((VMStackObjectVariable)data);

                case VMVariableScope.TargetObject: //5
                    throw new VMSimanticsException("Target Object is Deprecated!", context);

                case VMVariableScope.Global: //6
                    return context.VM.GetGlobalValue((ushort)data);

                case VMVariableScope.Literal: //7
                    return (short)data;

                case VMVariableScope.Temps: //8
                    return context.Thread.TempRegisters[data];

                case VMVariableScope.Parameters: //9
                    return (short)context.Args[data];

                case VMVariableScope.StackObjectID: //10
                    if (context.StackObject != null)
                    {
                        return context.StackObject.ObjectID;
                    }
                    return 0; //no object = 0, ids have a base of 1

                case VMVariableScope.TempByTemp: //11
                    return context.Thread.TempRegisters[context.Thread.TempRegisters[data]];
                    
                case VMVariableScope.TreeAdRange: //12
                    throw new VMSimanticsException("Not implemented...", context);

                case VMVariableScope.StackObjectTemp: //13
                    throw new VMSimanticsException("Not implemented...", context); //accesses the stack object's thread and gets its temp...

                case VMVariableScope.MyMotives: //14
                    return ((VMAvatar)context.Caller).GetMotiveData((VMMotive)data);

                case VMVariableScope.StackObjectMotives: //15
                    return ((VMAvatar)context.StackObject).GetMotiveData((VMMotive)data);

                case VMVariableScope.StackObjectSlot: //16
                    var slotObj = context.StackObject.GetSlot(data);
                    return (slotObj == null)?(short)0:slotObj.ObjectID;

                case VMVariableScope.StackObjectMotiveByTemp: //17
                    return ((VMAvatar)context.StackObject).GetMotiveData((VMMotive)context.Thread.TempRegisters[data]);

                case VMVariableScope.MyPersonData: //18
                    return ((VMAvatar)context.Caller).GetPersonData((VMPersonDataVariable)data);

                case VMVariableScope.StackObjectPersonData: //19
                    return ((VMAvatar)context.StackObject).GetPersonData((VMPersonDataVariable)data);

                case VMVariableScope.MySlot: //20
                    var slotObj2 = context.Caller.GetSlot(data);
                    return (slotObj2 == null) ? (short)0 : slotObj2.ObjectID;

                case VMVariableScope.StackObjectDefinition: //21
                    return GetEntityDefinitionVar(context.StackObject.Object.OBJ, (VMOBJDVariable)data, context);

                case VMVariableScope.StackObjectAttributeByParameter: //22
                    return context.StackObject.GetAttribute((ushort)context.Args[data]);

                case VMVariableScope.RoomByTemp0: //23
                    //returns information on the selected room. Right now we don't have a room system, so always return the same values. (everywhere is indoors, not a pool)

                    if (data == 0) return 100; //ambient light 0-100
                    else if (data == 1) return 0; //outside
                    else if (data == 2) return 0; //level
                    else if (data == 3) return 0; //area (???)
                    else if (data == 4) return 0; //is pool
                    else throw new VMSimanticsException("Invalid room data!", context);

                    //throw new VMSimanticsException("Not implemented...");

                case VMVariableScope.NeighborInStackObject: //24
                    throw new VMSimanticsException("Not implemented...", context);

                case VMVariableScope.Local: //25
                    return (short)context.Locals[data];

                case VMVariableScope.Tuning: //26
                    return GetTuningVariable(context.Callee, data, context);

                case VMVariableScope.DynSpriteFlagForTempOfStackObject: //27
                    return context.StackObject.IsDynamicSpriteFlagSet((ushort)context.Thread.TempRegisters[data]) ? (short)1 : (short)0;

                case VMVariableScope.TreeAdPersonalityVar: //28
                    throw new VMSimanticsException("Not implemented...", context);

                case VMVariableScope.TreeAdMin: //29
                    throw new VMSimanticsException("Not implemented...", context);

                case VMVariableScope.MyPersonDataByTemp: //30
                    return ((VMAvatar)context.Caller).GetPersonData((VMPersonDataVariable)(context.Thread.TempRegisters[data]));

                case VMVariableScope.StackObjectPersonDataByTemp: //31
                    return ((VMAvatar)context.StackObject).GetPersonData((VMPersonDataVariable)(context.Thread.TempRegisters[data]));

                case VMVariableScope.NeighborPersonData: //32
                    throw new VMSimanticsException("Not implemented...", context);

                case VMVariableScope.JobData: //33 jobdata(temp0, temp1), used a few times to test if a person is at work but that isn't relevant for tso...
                    throw new VMSimanticsException("Should not be used, but if this shows implement an empty shell to return ideal values.", context);

                case VMVariableScope.NeighborhoodData: //34
                    throw new VMSimanticsException("Should not be used, but if this shows implement an empty shell to return ideal values.", context);

                case VMVariableScope.StackObjectFunction: //35
                    return (short)context.StackObject.EntryPoints[data].ActionFunction;

                case VMVariableScope.MyTypeAttr: //36
                    throw new VMSimanticsException("Unused", context);
                
                case VMVariableScope.StackObjectTypeAttr: //37
                    throw new VMSimanticsException("Unused", context);

                case VMVariableScope.ThirtyEight: //38
                    throw new VMSimanticsException("Really", context);

                case VMVariableScope.LocalByTemp: //40
                    return (short)context.Locals[context.Thread.TempRegisters[data]];

                case VMVariableScope.StackObjectAttributeByTemp: //41
                    return context.StackObject.GetAttribute((ushort)context.Thread.TempRegisters[data]);
                    
                case VMVariableScope.TempXL: //42
                    //this needs a really intricate special case for specific operations.
                    throw new VMSimanticsException("Not implemented...", context);

                case VMVariableScope.CityTime: //43
                    //return GetCityTime(data)
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

                    };
                    break;
                case VMVariableScope.TSOStandardTime: //44
                    //return GetTSOStandardTime(data)
                    return 0;
                    throw new VMSimanticsException("Not implemented...", context);

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

                    };
                    break;

                case VMVariableScope.MyList: //46 (man if only i knew what this meant)
                    switch (data)
                    {
                        case 0: return context.Caller.MyList.First.Value; //is this allowed?
                        case 1: return context.Caller.MyList.Last.Value;
                        case 2: return (short)context.Caller.MyList.Count;
                        default: throw new VMSimanticsException("Unknown List Accessor", context);
                    }
                case VMVariableScope.StackObjectList: //47
                    if (context.StackObject == null) return 0;
                    switch (data)
                    {
                        case 0: return context.StackObject.MyList.First.Value;
                        case 1: return context.StackObject.MyList.Last.Value;
                        case 2: return (short)context.StackObject.MyList.Count;
                        default: throw new VMSimanticsException("Unknown List Accessor", context);
                    }

                case VMVariableScope.MoneyOverHead32Bit: //48
                    //we're poor... will need special case for this in expression like TempXL
                    return 0;

                case VMVariableScope.MyLeadTileAttribute: //49
                    return context.Caller.MultitileGroup.Objects[0].GetAttribute(data);

                case VMVariableScope.StackObjectLeadTileAttribute: //50
                    return context.StackObject.MultitileGroup.Objects[0].GetAttribute(data);

                case VMVariableScope.MyLeadTile: //51
                    throw new VMSimanticsException("Not implemented...", context);

                case VMVariableScope.StackObjectLeadTile: //52
                    throw new VMSimanticsException("Not implemented...", context);

                case VMVariableScope.StackObjectMasterDef: //53
                    //gets definition of the master tile of a multi tile object in the stack object.
                    var masterDef = context.StackObject.MasterDefinition;
                    return GetEntityDefinitionVar((masterDef == null)?context.StackObject.Object.OBJ:masterDef, (VMOBJDVariable)data, context);

                case VMVariableScope.FeatureEnableLevel: //54
                    return 1;
                    //all of them are enabled, dont really care right now

                case VMVariableScope.MyAvatarID: //59
                    return 0;

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

        public static int GetBigVariable(VMStackFrame context, VMVariableScope scope, ushort data) //used by functions which can take 32 bit integers, such as VMExpression.
        {
            switch (scope)
            {
                case VMVariableScope.TempXL:
                    return context.Thread.TempXL[data];
                default:
                    return GetVariable(context, scope, data); //return a normal var
            }
        }
        

        public static short GetTuningVariable(VMEntity entity, ushort data, VMStackFrame context){
            var tableID = (ushort)(data >> 7);
            var keyID = (ushort)(data & 0x7F);

            int mode = 0;
            if (tableID < 64) mode = 0;
            else if (tableID < 128) { tableID = (ushort)((tableID - 64)); mode = 1; }
            else if (tableID < 192) { tableID = (ushort)((tableID - 128)); mode = 2; }

            BCON bcon;
            OTFTable tuning;

            /** This could be in a BCON or an OTF **/

            switch (mode) {
                case 0: //local
                    bcon = context.CodeOwner.Get<BCON>((ushort)(tableID+4096));
                    if (bcon != null) return (short)bcon.Constants[keyID];

                    tuning = context.CodeOwner.Get<OTFTable>((ushort)(tableID+4096));
                    if (tuning != null) return (short)tuning.GetKey(keyID).Value;
                    break;
                case 1: //semi globals
                    ushort testTab = (ushort)(tableID + 8192);
                    bcon = context.CodeOwner.Get<BCON>(testTab);
                    if (bcon != null && keyID < bcon.Constants.Length) return (short)bcon.Constants[keyID];

                    tuning = context.CodeOwner.Get<OTFTable>(testTab);
                    if (tuning != null) return (short)tuning.GetKey(keyID).Value;

                    if (context.CodeOwner.SemiGlobal != null)
                    {
                        bcon = context.CodeOwner.SemiGlobal.Get<BCON>(testTab);
                        if (bcon != null && keyID < bcon.Constants.Length) return (short)bcon.Constants[keyID];

                        tuning = context.CodeOwner.SemiGlobal.Get<OTFTable>(testTab);
                        if (tuning != null) return (short)tuning.GetKey(keyID).Value;
                    }
                    break;
                case 2: //global
                    bcon = context.Global.Resource.Get<BCON>((ushort)(tableID+256));
                    if (bcon != null && keyID < bcon.Constants.Length) return (short)bcon.Constants[keyID];

                    tuning = context.Global.Resource.Get<OTFTable>((ushort)(tableID+256));
                    if (tuning != null) return (short)tuning.GetKey(keyID).Value;
                    break;
            }

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
                case VMOBJDVariable.InitialStackSize:
                    return (short)objd.StackSize;
                case VMOBJDVariable.BaseGraphic:
                    return (short)objd.BaseGraphicID;
                case VMOBJDVariable.NumGraphics:
                    return (short)objd.NumGraphics;
                case VMOBJDVariable.MainTreeID:
                    return (short)objd.BHAV_MainID; // should this use OBJf functions?
                case VMOBJDVariable.GardeningTreeID:
                    return (short)objd.BHAV_GardeningID;
                case VMOBJDVariable.TreeTableID:
                    return (short)objd.TreeTableID;
                case VMOBJDVariable.IntersectionGroup:
                    return (short)objd.InteractionGroupID;
                case VMOBJDVariable.Type:
                    return (short)objd.ObjectType;
                case VMOBJDVariable.MasterID:
                    return (short)objd.MasterID;
                case VMOBJDVariable.SubIndex:
                    return (short)objd.SubIndex;
                case VMOBJDVariable.WashHandsTreeID:
                    return (short)objd.BHAV_WashHandsID;
                case VMOBJDVariable.AnimTableID:
                    return (short)objd.AnimationTableID;
                case VMOBJDVariable.GUID1:
                    return (short)(objd.GUID % 0xFFFF);
                case VMOBJDVariable.GUID2:
                    return (short)(objd.GUID >> 16);
                case VMOBJDVariable.Disabled:
                    return (short)objd.Disabled;
                case VMOBJDVariable.PortalTreeID:
                    throw new VMSimanticsException("Not Implemented!", context);
                case VMOBJDVariable.Price:
                    return (short)objd.Price;
                case VMOBJDVariable.BodyStringsID:
                    return (short)objd.BodyStringID;
                case VMOBJDVariable.SlotsID:
                    return (short)objd.SlotID;
                case VMOBJDVariable.AllowIntersectionTreeID:
                    return (short)objd.BHAV_AllowIntersectionID;
                case VMOBJDVariable.UsesFnTable:
                    return (short)objd.UsesFnTable;
                case VMOBJDVariable.Bitfield1:
                    return (short)objd.BitField1;
                case VMOBJDVariable.PrepareFoodTreeID:
                    return (short)objd.BHAV_PrepareFoodID;
                case VMOBJDVariable.CookFoodTreeID:
                    return (short)objd.BHAV_CookFoodID;
                case VMOBJDVariable.PlaceOnSurfaceTreeID:
                    return (short)objd.BHAV_PlaceSurfaceID;
                case VMOBJDVariable.DisposeTreeID:
                    return (short)objd.BHAV_DisposeID;
                case VMOBJDVariable.EatFoodTreeID:
                    return (short)objd.BHAV_EatID;
                case VMOBJDVariable.PickupFromSlotTreeID:
                    return (short)objd.BHAV_PickupID; //uh
                case VMOBJDVariable.WashDishTreeID:
                    return (short)objd.BHAV_WashDishID;
                case VMOBJDVariable.EatingSurfaceTreeID:
                    return (short)objd.BHAV_EatSurfaceID;
                case VMOBJDVariable.SitTreeID:
                    return (short)objd.BHAV_SitID;
                case VMOBJDVariable.StandTreeID:
                    return (short)objd.BHAV_StandID;
                case VMOBJDVariable.SalePrice:
                    return (short)objd.SalePrice;
                case VMOBJDVariable.Unused35:
                    throw new VMSimanticsException("Not Implemented!", context);
                case VMOBJDVariable.Unused36:
                    throw new VMSimanticsException("Not Implemented!", context);
                case VMOBJDVariable.BrokenBaseGraphicOffset:
                    throw new VMSimanticsException("Not Implemented!", context);
                case VMOBJDVariable.Unused38:
                    throw new VMSimanticsException("Not Implemented!", context);
                case VMOBJDVariable.HasCriticalAttributes:
                    throw new VMSimanticsException("Not Implemented!", context);
                case VMOBJDVariable.BuyModeType:
                    return (short)objd.FunctionFlags;
                case VMOBJDVariable.CatalogStringsID:
                    return (short)objd.CatalogStringsID;
                case VMOBJDVariable.IsGlobalSimObject:
                    return (short)objd.Global;
                case VMOBJDVariable.InitTreeID:
                    return (short)objd.BHAV_Init;
                case VMOBJDVariable.PlaceTreeID:
                    return (short)objd.BHAV_Place;
                case VMOBJDVariable.UserPickupTreeID:
                    return (short)objd.BHAV_UserPickup;
                case VMOBJDVariable.WallStyle:
                    return (short)objd.WallStyle;
                case VMOBJDVariable.LoadTreeID:
                    return (short)objd.BHAV_Load;
                case VMOBJDVariable.UserPlaceTreeID:
                    return (short)objd.BHAV_UserPlace;
                case VMOBJDVariable.ObjectVersion:
                    return (short)objd.ObjectVersion;
                case VMOBJDVariable.RoomChangedTreeID:
                    return (short)objd.BHAV_RoomChange;
                case VMOBJDVariable.MotiveEffectsID:
                    return (short)objd.MotiveEffectsID;
                case VMOBJDVariable.CleanupTreeID:
                    return (short)objd.BHAV_Cleanup;
                case VMOBJDVariable.LevelInfoRequestTreeID:
                    return (short)objd.BHAV_LevelInfo;
                case VMOBJDVariable.CatalogPopupID:
                    return (short)objd.CatalogID;
                case VMOBJDVariable.ServingSurfaceTreeID:
                    return (short)objd.CatalogID;
                case VMOBJDVariable.LevelOffset:
                    return (short)objd.LevelOffset;
                case VMOBJDVariable.Shadow:
                    return (short)objd.Shadow;
                case VMOBJDVariable.NumAttributes:
                    return (short)objd.NumAttributes;
                case VMOBJDVariable.CleanTreeID:
                    return (short)objd.BHAV_Clean;
                case VMOBJDVariable.QueueSkippedTreeID:
                    return (short)objd.BHAV_QueueSkipped;
                case VMOBJDVariable.FrontDirection:
                    return (short)objd.FrontDirection;
                case VMOBJDVariable.WallAdjacencyChangedTreeID:
                    return (short)objd.BHAV_WallAdjacencyChanged;
                case VMOBJDVariable.MyLeadObject:
                    return (short)objd.MyLeadObject;
                case VMOBJDVariable.DynamicSpritesBaseID:
                    return (short)objd.DynamicSpriteBaseId;
                case VMOBJDVariable.NumDynamicSprites:
                    return (short)objd.NumDynamicSprites;
                case VMOBJDVariable.ChairEntryFlags:
                    return (short)objd.ChairEntryFlags;
                case VMOBJDVariable.TileWidth:
                    return (short)objd.TileWidth;
                case VMOBJDVariable.LotCategories:
                    return 0; //NOT IN OBJD RIGHT NOW!
                case VMOBJDVariable.BuildModeType:
                    return (short)objd.BuildModeType;
                case VMOBJDVariable.OriginalGUID1:
                    return (short)objd.OriginalGUID1;
                case VMOBJDVariable.OriginalGUID2:
                    return (short)objd.OriginalGUID2;
                case VMOBJDVariable.SuitGUID1:
                    return (short)objd.SuitGUID1;
                case VMOBJDVariable.SuitGUID2:
                    return (short)objd.SuitGUID2;
                case VMOBJDVariable.PickupTreeID:
                    return (short)objd.BHAV_Pickup;
                case VMOBJDVariable.ThumbnailGraphic:
                    return (short)objd.ThumbnailGraphic;
                case VMOBJDVariable.ShadowFlags:
                    return (short)objd.ShadowFlags;
                case VMOBJDVariable.FootprintMask:
                    return (short)objd.FootprintMask;
                case VMOBJDVariable.DynamicMultiTileUpdateTreeID:
                    return (short)objd.BHAV_DynamicMultiTileUpdate;
                case VMOBJDVariable.ShadowBrightness:
                    return (short)objd.ShadowBrightness;
                case VMOBJDVariable.RepairTreeID:
                    return (short)objd.BHAV_Repair;
                case VMOBJDVariable.WallStyleSpriteID:
                    return (short)objd.WallStyleSpriteID;
                case VMOBJDVariable.RatingHunger:
                    return (short)objd.RatingHunger;
                case VMOBJDVariable.RatingComfort:
                    return (short)objd.CatalogID;
                case VMOBJDVariable.RatingHygiene:
                    return (short)objd.RatingHygiene;
                case VMOBJDVariable.RatingBladder:
                    return (short)objd.RatingBladder;
                case VMOBJDVariable.RatingEnergy:
                    return (short)objd.RatingEnergy;
                case VMOBJDVariable.RatingFun:
                    return (short)objd.RatingFun;
                case VMOBJDVariable.RatingRoom:
                    return (short)objd.RatingRoom;
                case VMOBJDVariable.RatingSkillFlags:
                    return (short)objd.RatingSkillFlags;
                case VMOBJDVariable.NumTypeAttributes:
                    throw new VMSimanticsException("Not Implemented!", context); //??
                case VMOBJDVariable.MiscFlags:
                    throw new VMSimanticsException("Not Implemented!", context); //??
                case VMOBJDVariable.TypeAttrGUID1:
                    throw new VMSimanticsException("Not Implemented!", context);
                case VMOBJDVariable.TypeAttrGUID2:
                    throw new VMSimanticsException("Not Implemented!", context);
                case VMOBJDVariable.InteractionResultStrings:
                    throw new VMSimanticsException("Not Implemented!", context);
                case VMOBJDVariable.ClientHouseJoinTreeID:
                    throw new VMSimanticsException("Not Implemented!", context);
                case VMOBJDVariable.PrepareForSaleTreeID:
                    throw new VMSimanticsException("Not Implemented!", context);
                default:
                    throw new VMSimanticsException("Unknown definition var", context);
            }
        }
            
        /// <summary>
        /// Set a variable
        /// </summary>
        /// <param name="context"></param>
        /// <param name="scope"></param>
        /// <param name="data"></param>
        /// <param name="value"></param>
        public static bool SetVariable(VMStackFrame context, VMVariableScope scope, ushort data, short value){
            switch (scope){
                case VMVariableScope.MyObjectAttributes: //0
                    context.Caller.SetAttribute(data, value);
                    return true;

                case VMVariableScope.StackObjectAttributes: //1
                    context.StackObject.SetAttribute(data, value);
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
                    /** Not too sure if this is illegal **/
                    context.Args[data] = value;
                    return true;

                case VMVariableScope.StackObjectID: //10
                    /** Change the stack object **/
                    context.StackObject = context.VM.GetObjectById(value);
                    return true;

                case VMVariableScope.TempByTemp: //11
                    context.Thread.TempRegisters[context.Thread.TempRegisters[data]] = value;
                    return true;

                case VMVariableScope.TreeAdRange: //12
                    return false; //can't set this!

                case VMVariableScope.StackObjectTemp: //13
                    throw new VMSimanticsException("Not implemented...", context);

                case VMVariableScope.MyMotives: //14
                    return ((VMAvatar)context.Caller).SetMotiveData((VMMotive)data, value);

                case VMVariableScope.StackObjectMotives: //15
                    return ((VMAvatar)context.StackObject).SetMotiveData((VMMotive)data, value);

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
                    context.Locals[data] = (ushort)value;
                    return true;

                case VMVariableScope.Tuning: //26
                    return false; //you can't set this!

                case VMVariableScope.DynSpriteFlagForTempOfStackObject: //27
                    context.StackObject.SetDynamicSpriteFlag((ushort)context.Thread.TempRegisters[data], value > 0);
                    return true;

                case VMVariableScope.TreeAdPersonalityVar: //28
                    return false; //you can't set this!

                case VMVariableScope.TreeAdMin: //29
                    return false; //you can't set this!

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
                    throw new VMSimanticsException("Not implemented...", context);

                case VMVariableScope.StackObjectTypeAttr: //37
                    throw new VMSimanticsException("Not implemented...", context);

                case VMVariableScope.ThirtyEight: //38
                    return false; //you can't set this!

                case VMVariableScope.LocalByTemp: //40
                    context.Locals[context.Thread.TempRegisters[data]] = (ushort)value;
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
                    throw new VMSimanticsException("Not implemented...", context);
                    //needs special case like TempXL.

                case VMVariableScope.MyLeadTileAttribute: //49
                    context.Caller.MultitileGroup.Objects[0].SetAttribute(data, value);
                    return true;

                case VMVariableScope.StackObjectLeadTileAttribute: //50
                    context.StackObject.MultitileGroup.Objects[0].SetAttribute(data, value);
                    return true;

                case VMVariableScope.MyLeadTile: //51
                case VMVariableScope.StackObjectLeadTile: //52
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

        public static bool SetBigVariable(VMStackFrame context, VMVariableScope scope, ushort data, int value)
        {
            switch (scope)
            {
                case VMVariableScope.TempXL:
                    context.Thread.TempXL[data] = value;
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

            switch (scope){
                case VMAnimationScope.Object:
                    var obj = context.Callee.Object;
                    var anitableID = obj.OBJ.AnimationTableID;
                    anitableID = 129;
                    /*
                     * Fridge has an animtable of 0
                     * if (anitableID == 0){
                        return null;
                    }*/
                    //if (anitableID == 0){
                    //    anitableID = 129;
                    //}
                    //129
                    animTable = obj.Resource.Get<STR>(anitableID);
                    break;
                case VMAnimationScope.Misc:
                    animTable = context.Global.Resource.Get<STR>(156);
                    break;
                case VMAnimationScope.PersonStock:
                    animTable = context.Global.Resource.Get<STR>(130);
                    break;
                case VMAnimationScope.Global:
                    animTable = context.Global.Resource.Get<STR>(128);
                    break;
            }

            if (animTable == null){
                return null;
            }

            var animationName = animTable.GetString(id);
            if (animationName != null) return TSO.Content.Content.Get().AvatarAnimations.Get(animationName + ".anim");
            else return null;
        }

        public static Appearance GetSuit(VMStackFrame context, VMSuitScope scope, ushort id){
            switch (scope)
            {
                case VMSuitScope.Object:
                    var suitTable = context.Callee.Object.Resource.Get<STR>(304);
                    if (suitTable != null){
                        var suitFile = suitTable.GetString(id) + ".apr";

                        var apr = TSO.Content.Content.Get().AvatarAppearances.Get(suitFile);
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
                        return slots.Slots[3][data];
                    }
                    return null;
                case VMSlotScope.Literal:
                    return context.StackObject.Slots.Slots[3][data];
                case VMSlotScope.StackVariable:
                    return context.StackObject.Slots.Slots[3][context.Args[data]];
            }
            return null;
        }


        /**
         * Provides a string description of a variable, this is a utility for trace messages
         * that show what he VM is doing
         */
        public static string DescribeVariable(VMStackFrame context, VMVariableScope scope, ushort data){
            bool didPrint = false;
            
            var result = "";
            switch (scope){
                case VMVariableScope.Literal:
                    result += "literal " + data;
                    break;
                case VMVariableScope.Local:
                    result += "local #" + data;
                    if (context != null){
                        result += " (current value = " + VMMemory.GetVariable(context, scope, data) + ")";
                    }
                    break;
                case VMVariableScope.StackObject:
                    result += "callee." + ((VMStackObjectVariable)data).ToString();
                    if (context != null){
                        result += " (current value = " + VMMemory.GetVariable(context, scope, data) + ")";
                    }
                    break;
                case VMVariableScope.MyObjectAttributes:
                    if (context != null)
                    {
                        if (context.Caller.RTTI != null && context.Caller.RTTI.AttributeLabels != null)
                        {
                            string[] attributeLabels = context.Caller.RTTI.AttributeLabels;
                            if (data < attributeLabels.Length)
                            {
                                result += "caller.attributes." + attributeLabels[data] + "(" + data + ")";
                                didPrint = true;
                            }
                        }
                    }
                    if (!didPrint)
                    {
                        result += "caller.attributes.(" + data + ")";
                    }
                    if (context != null)
                    {
                        result += " (current value = " + VMMemory.GetVariable(context, scope, data) + ")";
                    }
                    break;
                case VMVariableScope.StackObjectAttributes:
                    if (context != null){
                        if (context.StackObject.RTTI != null && context.StackObject.RTTI.AttributeLabels != null){
                            string[] attributeLabels = context.StackObject.RTTI.AttributeLabels;
                            if (data < attributeLabels.Length){
                                result += "StackObject.attributes." + attributeLabels[data] + "(" + data + ")";
                                didPrint = true;
                            }
                        }
                    }
                    if (!didPrint){
                        result += "StackObject.attributes.(" + data + ")";
                    }
                    if (context != null){
                        result += " (current value = " + VMMemory.GetVariable(context, scope, data) + ")";
                    }
                    break;
                case VMVariableScope.Tuning:
                    result = "to be actually done";
                    /*if (context != null)
                    {
                        var label = GetTuningVariableLabel(context.StackObject.Object, data);
                        if (label != null)
                        {
                            result += "StackObject.tuning." + label + "(" + data + ") (value = " + GetTuningVariable(context.StackObject, data, context) + ")";
                            didPrint = true;   
                        }
                    }
                    if (!didPrint){
                        result = "StackObject.tuning." + data;
                    }*/
                    break;
                case VMVariableScope.Temps:
                    result = "temp." + data;
                    if (context != null)
                    {
                        result += " (value = " + context.Thread.TempRegisters[data] + ")";
                    }
                    break;
                case VMVariableScope.Parameters:
                    result = "arg." + data;
                    if (context != null)
                    {
                        result += " (value = " + context.Args[data] + ")";
                    }
                    break;
                case VMVariableScope.StackObjectDefinition:
                    result = "stack.objd." + ((VMOBJDVariable)data);
                    if (context != null){
                        result += " (value = " + GetEntityDefinitionVar(context.StackObject.Object.OBJ, ((VMOBJDVariable)data), null) + ")";
                    }
                    break;
                case VMVariableScope.MyPersonData:
                    result = "myPersonData." + ((VMPersonDataVariable)data).ToString();
                    break;
                case VMVariableScope.DynSpriteFlagForTempOfStackObject:
                    result = "stack.dynFlags." + data;
                    break;
                default:
                    result = "Unknown type: " + scope;
                    break;
            }
            return result;
        }
    }
}
