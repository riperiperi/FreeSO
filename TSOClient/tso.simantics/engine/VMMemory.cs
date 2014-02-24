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
                case VMVariableScope.Literal:
                    return (short)data;
                case VMVariableScope.Local:
                    return (short)context.Locals[data];
                case VMVariableScope.StackObject:
                    return context.Callee.GetValue((VMStackObjectVariable)data);
                case VMVariableScope.StackObjectAttributes:
                    return context.Callee.GetAttribute(data);
                case VMVariableScope.MyObjectAttributes:
                    return context.Caller.GetAttribute(data);
                case VMVariableScope.MyObject:
                    return context.Caller.GetValue((VMStackObjectVariable)data);
                case VMVariableScope.Temps:
                    return context.Thread.TempRegisters[data];
                case VMVariableScope.Parameters:
                    return (short)context.Args[data];
                case VMVariableScope.StackObjectsDefinition:
                    return GetEntityDefinitionVar(context.Callee.Object, (VMStackObjectDefinitionVariable)data);
                case VMVariableScope.StackObjectID:
                    if (context.Callee != null){
                        return context.Callee.ObjectID;
                    }
                    return -1;
                case VMVariableScope.StackObjectTuning:
                    return GetTuningVariable(context.Callee.Object, data);
                case VMVariableScope.DynSpriteFlagForTempOfStackObject:
                    return context.Callee.IsDynamicSpriteFlagSet(data) ? (short)1 : (short)0;
                case VMVariableScope.MyPersonData:
                    var avatar = (VMAvatar)context.Caller;
                    return avatar.GetPersonData((VMPersonDataVariable)data);
            }
            throw new Exception("Unknown get variable");
        }

        public static short GetTuningVariable(GameObject obj, ushort data){
            var tableID = (ushort)(4096 + (data >> 7));
            var keyID = (ushort)(data & 0x7F);

            /** This could be in a BCON or an OTF **/
            var bcon = obj.Resource.Get<BCON>(tableID);
            if (bcon != null){
                return (short)bcon.Constants[keyID];
            }

            var tuning = obj.Resource.Get<OTFTable>(tableID);
            if (tuning != null){
                return (short)tuning.GetKey(keyID).Value;
            }
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
            throw new Exception("Could not find tuning constant!");
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
                case VMVariableScope.Local:
                    context.Locals[data] = (ushort)value;
                    return true;
                case VMVariableScope.Literal:
                    /** Huh? **/
                    return false;
                case VMVariableScope.StackObject:
                    return context.Callee.SetValue((VMStackObjectVariable)data, value);
                case VMVariableScope.StackObjectAttributes:
                    context.Callee.SetAttribute(data, value);
                    return true;
                case VMVariableScope.MyObjectAttributes:
                    context.Caller.SetAttribute(data, value);
                    return true;
                case VMVariableScope.MyObject:
                    return context.Caller.SetValue((VMStackObjectVariable)data, value);
                case VMVariableScope.StackObjectID:
                    /** Change the stack object **/
                    context.Callee = context.VM.GetObjectById(value);
                    return true;
                case VMVariableScope.Temps:
                    context.Thread.TempRegisters[data] = value;
                    return true;
                case VMVariableScope.Parameters:
                    /** Not too sure if this is illegal **/
                    context.Args[data] = value;
                    return true;
                case VMVariableScope.DynSpriteFlagForTempOfStackObject:
                    context.Callee.SetDynamicSpriteFlag(data, value > 0);
                    return true;
                case VMVariableScope.MyPersonData:
                    var avatar = ((VMAvatar)context.Caller);
                    return avatar.SetPersonData((VMPersonDataVariable)data, value);
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
                    animTable = context.Global.Get<STR>(156);
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
                    var slots = context.Global.Get<SLOT>(100);
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
                            string[] attributeLabels = context.Callee.RTTI.AttributeLabels;
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
                        if (context.Callee.RTTI != null && context.Callee.RTTI.AttributeLabels != null){
                            string[] attributeLabels = context.Callee.RTTI.AttributeLabels;
                            if (data < attributeLabels.Length){
                                result += "callee.attributes." + attributeLabels[data] + "(" + data + ")";
                                didPrint = true;
                            }
                        }
                    }
                    if (!didPrint){
                        result += "callee.attributes.(" + data + ")";
                    }
                    if (context != null){
                        result += " (current value = " + VMMemory.GetVariable(context, scope, data) + ")";
                    }
                    break;
                case VMVariableScope.StackObjectTuning:
                    if (context != null)
                    {
                        var label = GetTuningVariableLabel(context.Callee.Object, data);
                        if (label != null)
                        {
                            result += "callee.tuning." + label + "(" + data + ") (value = " + GetTuningVariable(context.Callee.Object, data) + ")";
                            didPrint = true;   
                        }
                    }
                    if (!didPrint){
                        result = "callee.tuning." + data;
                    }
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
                case VMVariableScope.StackObjectsDefinition:
                    result = "stack.objd." + ((VMStackObjectDefinitionVariable)data);
                    if (context != null){
                        result += " (value = " + GetEntityDefinitionVar(context.Callee.Object, ((VMStackObjectDefinitionVariable)data)) + ")";
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
