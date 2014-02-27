using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.simantics.engine.scopes;
using tso.vitaboy;
using tso.files.formats.iff.chunks;
using tso.content;
using tso.simantics.model;
using tso.files.formats.otf;

namespace tso.simantics.engine.utils
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
                    throw new Exception("Target Object is Deprecated!");

                case VMVariableScope.MyObject: //3
                    return context.Caller.GetValue((VMStackObjectVariable)data);

                case VMVariableScope.StackObject: //4
                    return context.StackObject.GetValue((VMStackObjectVariable)data);

                case VMVariableScope.TargetObject: //5
                    throw new Exception("Target Object is Deprecated!");

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
                    return -1;

                case VMVariableScope.TempByTemp: //11
                    return context.Thread.TempRegisters[context.Thread.TempRegisters[data]];
                    
                case VMVariableScope.TreeAdRange: //12
                    throw new Exception("Not implemented...");

                case VMVariableScope.StackObjectTemp: //13
                    throw new Exception("Not implemented..."); //accesses the stack object's thread and gets its temp...

                case VMVariableScope.MyMotives: //14
                    return ((VMAvatar)context.Caller).GetMotiveData((VMMotive)data);

                case VMVariableScope.StackObjectMotives: //15
                    return ((VMAvatar)context.StackObject).GetMotiveData((VMMotive)data);

                case VMVariableScope.StackObjectSlot: //16
                    throw new Exception("Not implemented...");

                case VMVariableScope.StackObjectMotiveByTemp: //17
                    return ((VMAvatar)context.StackObject).GetMotiveData((VMMotive)context.Thread.TempRegisters[data]);

                case VMVariableScope.MyPersonData: //18
                    return ((VMAvatar)context.Caller).GetPersonData((VMPersonDataVariable)data);

                case VMVariableScope.StackObjectPersonData: //19
                    return ((VMAvatar)context.StackObject).GetPersonData((VMPersonDataVariable)data);

                case VMVariableScope.MySlot: //20
                    throw new Exception("Not implemented...");

                case VMVariableScope.StackObjectDefinition: //21
                    return GetEntityDefinitionVar(context.StackObject.Object, (VMStackObjectDefinitionVariable)data);

                case VMVariableScope.StackObjectAttributeByParameter: //22
                    return context.StackObject.GetAttribute((ushort)context.Args[data]);

                case VMVariableScope.RoomByTemp0: //23
                    throw new Exception("Not implemented...");

                case VMVariableScope.NeighborInStackObject: //24
                    throw new Exception("Not implemented...");

                case VMVariableScope.Local: //25
                    return (short)context.Locals[data];

                case VMVariableScope.Tuning: //26
                    return GetTuningVariable(context.Callee, data, context);

                case VMVariableScope.DynSpriteFlagForTempOfStackObject: //27
                    return context.StackObject.IsDynamicSpriteFlagSet(data) ? (short)1 : (short)0;

                case VMVariableScope.TreeAdPersonalityVar: //28
                    throw new Exception("Not implemented...");

                case VMVariableScope.TreeAdMin: //29
                    throw new Exception("Not implemented...");

                case VMVariableScope.MyPersonDataByTemp: //30
                    return ((VMAvatar)context.Caller).GetPersonData((VMPersonDataVariable)(context.Thread.TempRegisters[data]));

                case VMVariableScope.StackObjectPersonDataByTemp: //31
                    return ((VMAvatar)context.StackObject).GetPersonData((VMPersonDataVariable)(context.Thread.TempRegisters[data]));

                case VMVariableScope.NeighborPersonData: //32
                    throw new Exception("Not implemented...");

                case VMVariableScope.JobData: //33 jobdata(temp0, temp1), used a few times to test if a person is at work but that isn't relevant for tso...
                    throw new Exception("Should not be used, but if this shows implement an empty shell to return ideal values.");

                case VMVariableScope.NeighborhoodData: //34
                    throw new Exception("Should not be used, but if this shows implement an empty shell to return ideal values.");

                case VMVariableScope.StackObjectFunction: //35
                    throw new Exception("Not implemented...");

                case VMVariableScope.MyTypeAttr: //36
                    throw new Exception("Unused");
                
                case VMVariableScope.StackObjectTypeAttr: //37
                    throw new Exception("Unused");

                case VMVariableScope.ThirtyEight: //38
                    throw new Exception("Really");

                case VMVariableScope.LocalByTemp: //40
                    return (short)context.Locals[context.Thread.TempRegisters[data]];

                case VMVariableScope.StackObjectAttributeByTemp: //41
                    return context.StackObject.GetAttribute((ushort)context.Thread.TempRegisters[data]);
                    
                case VMVariableScope.TempXL: //42
                    //this needs a really intricate special case for specific operations.
                    throw new Exception("Not implemented...");

                case VMVariableScope.CityTime: //43
                    //return GetCityTime(data)
                    throw new Exception("Not implemented...");

                case VMVariableScope.TSOStandardTime: //44
                    //return GetTSOStandardTime(data)
                    throw new Exception("Not implemented...");

                case VMVariableScope.GameTime: //45
                    //return GameTime(data)
                    throw new Exception("Not implemented...");

                case VMVariableScope.MyList: //46 (man if only i knew what this meant)
                    throw new Exception("Not implemented...");

                case VMVariableScope.StackObjectList: //47
                    throw new Exception("Not implemented...");

                case VMVariableScope.MoneyOverHead32Bit: //48
                    //we're poor... will need special case for this in expression like TempXL
                    return 0;

                case VMVariableScope.MyLeadTileAttribute: //49
                    throw new Exception("Not implemented...");

                case VMVariableScope.StackObjectLeadTileAttribute: //50
                    throw new Exception("Not implemented...");

                case VMVariableScope.MyLeadTile: //51
                    throw new Exception("Not implemented...");

                case VMVariableScope.StackObjectLeadTile: //52
                    throw new Exception("Not implemented...");

                case VMVariableScope.StackObjectMasterDef: //53
                    //gets definition of the master tile of a multi tile object in the stack object.
                    throw new Exception("Not implemented...");

                case VMVariableScope.FeatureEnableLevel: //54
                    return 1;
                    //all of them are enabled, dont really care right now

                case VMVariableScope.MyAvatarID: //59
                    return 0;

            }
            throw new Exception("Unknown get variable");
        }

        public static short GetTuningVariable(VMEntity entity, ushort data, VMStackFrame context){
            var tableID = (ushort)(4096 + (data >> 7));
            var keyID = (ushort)(data & 0x7F);

            BCON bcon;
            OTFTable tuning;

            /** This could be in a BCON or an OTF **/
            bcon = entity.Object.Resource.Get<BCON>(tableID);
            if (bcon != null) return (short)bcon.Constants[keyID];

            tuning = entity.Object.Resource.Get<OTFTable>(tableID);
            if (tuning != null) return (short)tuning.GetKey(keyID).Value;

            /** test for in semi globals **/
            if (entity.SemiGlobal != null)
            {
                bcon = entity.SemiGlobal.Resource.Get<BCON>((ushort)(tableID + 4032));
                if (bcon != null) return (short)bcon.Constants[keyID];

                tuning = entity.SemiGlobal.Resource.Get<OTFTable>((ushort)(tableID + 4032));
                if (tuning != null) return (short)tuning.GetKey(keyID).Value;
            }

            /** test for in globals **/

            bcon = context.Global.Resource.Get<BCON>((ushort)(tableID - 3968));
            if (bcon != null) return (short)bcon.Constants[keyID];

            tuning = context.Global.Resource.Get<OTFTable>((ushort)(tableID - 3968));
            if (tuning != null) return (short)tuning.GetKey(keyID).Value;

            throw new Exception("Could not find tuning constant!");
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
            //throw new Exception("Could not find tuning constant!");
        }

        public static short GetEntityDefinitionVar(GameObject obj, VMStackObjectDefinitionVariable var){
            var objd = obj.OBJ;
            switch (var)
            {
                case VMStackObjectDefinitionVariable.NumGraphics:
                    return (short)objd.NumGraphics;
                case VMStackObjectDefinitionVariable.OriginalGUID1:
                    return (short)(objd.GUID % (ushort)0xFFFF);
                case VMStackObjectDefinitionVariable.OriginalGUID2:
                    return (short)(objd.GUID / (ushort)0xFFFF);
                case VMStackObjectDefinitionVariable.SubIndex:
                    return (short)(objd.SubIndex);
                default:
                    throw new Exception("Unknown definition var");
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
                    throw new Exception("Target Object is Deprecated!");

                case VMVariableScope.MyObject: //3
                    return context.Caller.SetValue((VMStackObjectVariable)data, value);

                case VMVariableScope.StackObject: //4
                    return context.StackObject.SetValue((VMStackObjectVariable)data, value);

                case VMVariableScope.TargetObject: //5
                    throw new Exception("Target Object is Deprecated!");

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
                    throw new Exception("Not implemented...");

                case VMVariableScope.MyMotives: //14
                    return ((VMAvatar)context.Caller).SetMotiveData((VMMotive)data, value);

                case VMVariableScope.StackObjectMotives: //15
                    return ((VMAvatar)context.StackObject).SetMotiveData((VMMotive)data, value);

                case VMVariableScope.StackObjectSlot: //16
                    throw new Exception("Not implemented...");

                case VMVariableScope.StackObjectMotiveByTemp: //17
                    return ((VMAvatar)context.StackObject).SetMotiveData((VMMotive)context.Thread.TempRegisters[data], value);

                case VMVariableScope.MyPersonData: //18
                    return ((VMAvatar)context.Caller).SetPersonData((VMPersonDataVariable)data, value);

                case VMVariableScope.StackObjectPersonData: //19
                    return ((VMAvatar)context.StackObject).SetPersonData((VMPersonDataVariable)data, value);

                case VMVariableScope.MySlot: //20
                    throw new Exception("Not implemented...");

                case VMVariableScope.StackObjectDefinition: //21
                    return false; //you can't set this!

                case VMVariableScope.StackObjectAttributeByParameter: //22
                    context.StackObject.SetAttribute((ushort)context.Args[data], value);
                    return true;

                case VMVariableScope.RoomByTemp0: //23
                    throw new Exception("Not implemented...");

                case VMVariableScope.NeighborInStackObject: //24
                    throw new Exception("Not implemented...");

                case VMVariableScope.Local: //25
                    context.Locals[data] = (ushort)value;
                    return true;

                case VMVariableScope.Tuning: //26
                    return false; //you can't set this!

                case VMVariableScope.DynSpriteFlagForTempOfStackObject: //27
                    context.StackObject.SetDynamicSpriteFlag(data, value > 0);
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
                    throw new Exception("Not implemented...");

                case VMVariableScope.JobData: //33
                    throw new Exception("Not implemented...");

                case VMVariableScope.NeighborhoodData: //34
                    throw new Exception("Not implemented...");

                case VMVariableScope.StackObjectFunction: //35
                    return false; //you can't set this!

                case VMVariableScope.MyTypeAttr: //36
                    throw new Exception("Not implemented...");

                case VMVariableScope.StackObjectTypeAttr: //37
                    throw new Exception("Not implemented...");

                case VMVariableScope.ThirtyEight: //38
                    return false; //you can't set this!

                case VMVariableScope.LocalByTemp: //40
                    context.Locals[context.Thread.TempRegisters[data]] = (ushort)value;
                    return true;

                case VMVariableScope.StackObjectAttributeByTemp: //41
                    context.StackObject.SetAttribute((ushort)context.Thread.TempRegisters[data], value);
                    return true;

                case VMVariableScope.TempXL: //42
                    throw new Exception("Not implemented...");
                    //this will need a special case for the expression primitive

                case VMVariableScope.CityTime: //43
                case VMVariableScope.TSOStandardTime: //44
                case VMVariableScope.GameTime: //45
                    return false; //you can't set this!

                case VMVariableScope.MyList: //46
                    throw new Exception("Not implemented...");

                case VMVariableScope.StackObjectList: //47
                    throw new Exception("Not implemented...");

                case VMVariableScope.MoneyOverHead32Bit: //48
                    throw new Exception("Not implemented...");
                    //needs special case like TempXL.

                case VMVariableScope.MyLeadTileAttribute: //49
                    throw new Exception("Not implemented...");

                case VMVariableScope.StackObjectLeadTileAttribute: //50
                    throw new Exception("Not implemented...");

                case VMVariableScope.MyLeadTile: //51
                case VMVariableScope.StackObjectLeadTile: //52
                case VMVariableScope.StackObjectMasterDef: //53
                    return false;

                case VMVariableScope.FeatureEnableLevel: //54
                    throw new Exception("Not implemented...");

                case VMVariableScope.MyAvatarID: //59
                    return false; //you can't set this!
                    
                default:
                    throw new Exception("Unknown scope for set variable!");
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
                    /*
                     * Fridge has an animtable of 0
                     * if (anitableID == 0){
                        return null;
                    }*/
                    if (anitableID == 0){
                        anitableID = 129;
                    }
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
            return Content.Get().AvatarAnimations.Get(animationName + ".anim");
        }

        public static Appearance GetSuit(VMStackFrame context, VMSuitScope scope, ushort id){
            switch (scope)
            {
                case VMSuitScope.Object:
                    var suitTable = context.Callee.Object.Resource.Get<STR>(304);
                    if (suitTable != null){
                        var suitFile = suitTable.GetString(id) + ".apr";

                        var apr = Content.Get().AvatarAppearances.Get(suitFile);
                        return apr;
                    }
                    return null;
                default:
                    throw new Exception("I dont know about this suit scope");
            }
        }

        public static SLOTItem GetSlot(VMStackFrame context, VMSlotScope scope, ushort data)
        {
            switch (scope){
                case VMSlotScope.Global:
                    var slots = context.Global.Resource.Get<SLOT>(100);
                    if (slots != null && data < slots.Slots.Length){
                        return slots.Slots[data];
                    }
                    return null;
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
                    result = "stack.objd." + ((VMStackObjectDefinitionVariable)data);
                    if (context != null){
                        result += " (value = " + GetEntityDefinitionVar(context.StackObject.Object, ((VMStackObjectDefinitionVariable)data)) + ")";
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
