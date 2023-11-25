using FSO.SimAntics.Engine.Scopes;
using FSO.SimAntics.Model;
using System;
using System.Collections.Generic;

namespace FSO.SimAntics.JIT.Translation.CSharp.Engine
{
    public static class CSScopeMemory
    {
        private static string Exp(string str)
        {
            return str; //might change to a list of strings
        }

        private static string EnumValueName<T>(T enumValue)
        {
            if (Enum.IsDefined(typeof(T), enumValue))
                return $"{typeof(T).Name}.{enumValue.ToString()}";
            else
            {
                return $"({typeof(T).Name}){enumValue.ToString()}";
            }
        }

        public static string GetExpression(CSTranslationContext context, VMVariableScope scope, short data, bool big)
        {
            UpdateUsing(context, scope);

            switch (scope)
            {
                case VMVariableScope.MyObjectAttributes: //0
                    return Exp($"context.Caller.GetAttribute({(ushort)data})");

                case VMVariableScope.StackObjectAttributes: //1
                    return Exp($"context.StackObject.GetAttribute({(ushort)data})");

                case VMVariableScope.TargetObjectAttributes: //2
                    throw new Exception("Target Object is Deprecated!");

                case VMVariableScope.MyObject: //3
                    return Exp($"context.Caller.GetValue({EnumValueName((VMStackObjectVariable)data)})");

                case VMVariableScope.StackObject: //4
                    return Exp($"context.StackObject.GetValue({EnumValueName((VMStackObjectVariable)data)})");

                case VMVariableScope.TargetObject: //5
                    throw new Exception("Target Object is Deprecated!");

                case VMVariableScope.Global: //6
                    return Exp($"context.VM.GetGlobalValue({(ushort)data})");

                case VMVariableScope.Literal: //7
                    return Exp(data.ToString());

                case VMVariableScope.Temps: //8
                    return Exp($"temps[{data}]");

                case VMVariableScope.Parameters: //9
                    return Exp($"args[{data}]");

                case VMVariableScope.StackObjectID: //10
                    return Exp($"context.StackObjectID");

                case VMVariableScope.TempByTemp: //11
                    return Exp($"temps[temps[{data}]]");

                case VMVariableScope.TreeAdRange: //12
                    return Exp($"0");
                //throw new VMSimanticsException("Not implemented...", context);

                case VMVariableScope.StackObjectTemp: //13
                    return Exp($"context.StackObject.Thread.TempRegisters[{data}]");

                case VMVariableScope.MyMotives: //14
                    return Exp($"((VMAvatar)context.Caller).GetMotiveData({EnumValueName((VMMotive)data)})");

                case VMVariableScope.StackObjectMotives: //15
                    return Exp($"((context.StackObject as VMAvatar)?.GetMotiveData({EnumValueName((VMMotive)data)}) ?? 0)");

                case VMVariableScope.StackObjectSlot: //16
                    return Exp($"(context.StackObject.GetSlot({data})?.ObjectID ?? 0)");

                case VMVariableScope.StackObjectMotiveByTemp: //17
                    return Exp($"((VMAvatar)context.StackObject).GetMotiveData((VMMotive)temps[{data}])");

                case VMVariableScope.MyPersonData: //18
                    return Exp($"((VMAvatar)context.Caller).GetPersonData({EnumValueName((VMPersonDataVariable)data)})");

                case VMVariableScope.StackObjectPersonData: //19
                    return Exp($"((VMAvatar)context.StackObject).GetPersonData({EnumValueName((VMPersonDataVariable)data)})");

                case VMVariableScope.MySlot: //20
                    return Exp($"(context.Caller.GetSlot({data})?.ObjectID ?? 0)");

                case VMVariableScope.StackObjectDefinition: //21
                    return Exp($"VMMemory.GetEntityDefinitionVar(context.StackObject.Object.OBJ, {EnumValueName((VMOBJDVariable)data)}, context)");

                case VMVariableScope.StackObjectAttributeByParameter: //22
                    return Exp($"context.StackObject.GetAttribute((ushort)args[{data}])");

                //23 : room, fallback
                //24 : nhood, fallback
               
                case VMVariableScope.Local: //25
                    return Exp($"locals[{data}]");

                case VMVariableScope.Tuning: //26
                    return Exp($"VMMemory.GetTuningVariable(context.Callee, {(ushort)data}, context)");

                case VMVariableScope.DynSpriteFlagForTempOfStackObject: //27
                    return Exp($"(context.StackObject.IsDynamicSpriteFlagSet((ushort)temps[{data}]) ? (short)1 : (short)0)");

                case VMVariableScope.TreeAdPersonalityVar: //28
                    throw new Exception("Not implemented...");

                case VMVariableScope.TreeAdMin: //29
                    throw new Exception("Not implemented...");

                case VMVariableScope.MyPersonDataByTemp: //30
                    return Exp($"((VMAvatar)context.Caller).GetPersonData((VMPersonDataVariable)(temps[{data}]))");

                case VMVariableScope.StackObjectPersonDataByTemp: //31
                    return Exp($"((VMAvatar)context.StackObject).GetPersonData((VMPersonDataVariable)(temps[{data}]))");

                case VMVariableScope.NeighborPersonData: //32
                    if (!context.TS1) throw new Exception("Only valid in TS1.");
                    return Exp($"(Content.Content.Get().Neighborhood.GetNeighborByID(context.StackObjectID)?.PersonData?.ElementAt({data}) ?? 0)");

                case VMVariableScope.JobData: //33 jobdata(temp0, temp1), used a few times to test if a person is at work but that isn't relevant for tso...
                    if (!context.TS1) throw new Exception("Only valid in TS1.");
                    return Exp($"Content.Content.Get().Jobs.GetJobData((ushort)temps[0], temps[1], {data})");

                case VMVariableScope.NeighborhoodData: //34
                    return Exp($"0"); //tutorial values only

                case VMVariableScope.StackObjectFunction: //35
                    return Exp($"(short)context.StackObject.EntryPoints[{data}].ActionFunction");

                case VMVariableScope.MyTypeAttr: //36
                    if (context.TS1) return Exp($"Content.Content.Get().Neighborhood.GetTATT((context.Caller.MasterDefinition ?? context.Caller.Object.OBJ).TypeAttrGUID, {data})");
                    return Exp($"0");

                case VMVariableScope.StackObjectTypeAttr: //37
                    if (context.TS1) return Exp($"Content.Content.Get().Neighborhood.GetTATT((context.StackObject.MasterDefinition ?? context.StackObject.Object.OBJ).TypeAttrGUID, {data})");
                    return Exp($"0");

                //38 neighbor object definition: fallback

                case VMVariableScope.Unused:
                    return Exp($"context.VM.TuningCache.GetLimit({EnumValueName((VMMotive)data)})");

                case VMVariableScope.LocalByTemp: //40
                    return Exp($"locals[temps[{data}]]");

                case VMVariableScope.StackObjectAttributeByTemp: //41
                    return Exp($"context.StackObject.GetAttribute((ushort)temps[{data}])");

                case VMVariableScope.TempXL: //42
                    if (big) return Exp($"context.Thread.TempXL[{data}]");
                    else return Exp($"(short)context.Thread.TempXL[{data}]");
                case VMVariableScope.TSOStandardTime: //44
                    //return GetTSOStandardTime(data)
                    var time = "context.VM.Context.Clock.UTCNow";

                    switch (data)
                    {
                        case 0:
                            return Exp($"(short){time}.Second");
                        case 1:
                            return Exp($"(short){time}.Minute");
                        case 2:
                            return Exp($"(short){time}.Hour");
                        case 3:
                            return Exp($"(short){time}.Day");
                        case 4:
                            return Exp($"(short){time}.Month");
                        case 5:
                            return Exp($"(short){time}.Year");
                    };
                    return Exp($"0");

                case VMVariableScope.CityTime: //43
                case VMVariableScope.GameTime: //45
                    var timeN = "context.VM.Context.Clock";
                    switch (data)
                    {
                        case 0:
                            return Exp($"(short){timeN}.Seconds");
                        case 1:
                            return Exp($"(short){timeN}.Minutes");
                        case 2:
                            return Exp($"(short){timeN}.Hours");
                        case 3:
                            return Exp($"(short){timeN}.TimeOfDay");
                        case 4:
                            return Exp($"(short){timeN}.DayOfMonth");
                        case 5:
                            return Exp($"(short){timeN}.Month");
                        case 6:
                            return Exp($"(short){timeN}.Year");
                    };
                    break;

                case VMVariableScope.MyList: //46 (man if only i knew what this meant)
                    switch (data)
                    {
                        case 0: return Exp($"context.Caller.MyList.First.Value"); //is this allowed?
                        case 1: return Exp($"context.Caller.MyList.Last.Value");
                        case 2: return Exp($"(short)context.Caller.MyList.Count");
                        default: return Exp($"context.Caller.MyList.ElementAt(temps[0])");
                    }
                case VMVariableScope.StackObjectList: //47
                    //if (context.StackObject == null) return 0; (this hack is probably needed by something)
                    switch (data)
                    {
                        case 0: return Exp($"context.StackObject.MyList.First.Value");
                        case 1: return Exp($"context.StackObject.MyList.Last.Value");
                        case 2: return Exp($"(short)context.StackObject.MyList.Count");
                        default: return Exp($"context.StackObject.MyList.ElementAt(temps[0])");
                    }

                case VMVariableScope.MoneyOverHead32Bit: //48
                    //we're poor... will need special case for this in expression like TempXL
                    if (big) Exp($"0");
                    return Exp($"0");

                case VMVariableScope.MyLeadTileAttribute: //49
                    return Exp($"context.Caller.MultitileGroup.BaseObject.GetAttribute({(ushort)data})");

                case VMVariableScope.StackObjectLeadTileAttribute: //50
                    return Exp($"context.StackObject.MultitileGroup.BaseObject.GetAttribute({(ushort)data})");

                case VMVariableScope.MyLeadTile: //51
                    return $"context.Caller.MultitileGroup.BaseObject.ObjectID";

                case VMVariableScope.StackObjectLeadTile: //52
                    return $"context.StackObject.MultitileGroup.BaseObject.ObjectID";
                    //throw new Exception("Not implemented...");

                case VMVariableScope.StackObjectMasterDef: //53
                    //gets definition of the master tile of a multi tile object in the stack object.
                    return Exp($"GetEntityDefinitionVar(context.StackObject.MasterDefinition ?? context.StackObject.Object.OBJ, {EnumValueName((VMOBJDVariable)data)}, context)");

                case VMVariableScope.FeatureEnableLevel: //54
                    return Exp($"1");
                //all of them are enabled, dont really care right now

                //59: MyAvatarID, fallback
            }

            return Exp($"VMMemory.GetVariable(context, {EnumValueName(scope)}, {data})");
        }

        public static bool ScopeMutable(VMVariableScope scope)
        {
            switch (scope)
            {
                case VMVariableScope.Temps:
                case VMVariableScope.Parameters:
                case VMVariableScope.StackObjectID:
                case VMVariableScope.TempByTemp:
                case VMVariableScope.Local:
                case VMVariableScope.LocalByTemp:
                case VMVariableScope.TempXL:
                case VMVariableScope.StackObjectTemp:
                    return true;
            }
            return false;
        }

        public static void UpdateUsing(CSTranslationContext context, VMVariableScope scope)
        {
            var csClass = context.CurrentClass;
            if (!csClass.UseLocals &&
                (scope == VMVariableScope.Local || scope == VMVariableScope.LocalByTemp))
                csClass.UseLocals = true;

            if (!csClass.UseTemps &&
                (scope == VMVariableScope.Temps || scope == VMVariableScope.TempByTemp || scope == VMVariableScope.DynSpriteFlagForTempOfStackObject
                || scope == VMVariableScope.LocalByTemp || scope == VMVariableScope.MyPersonDataByTemp || scope == VMVariableScope.RoomByTemp0
                || scope == VMVariableScope.StackObjectAttributeByTemp || scope == VMVariableScope.StackObjectMotiveByTemp || scope == VMVariableScope.StackObjectPersonDataByTemp
                || scope == VMVariableScope.StackObjectTemp || scope == VMVariableScope.TempXL || scope == VMVariableScope.JobData || scope == VMVariableScope.MyList
                || scope == VMVariableScope.StackObjectList))
                csClass.UseTemps = true;

            if (!csClass.UseParams &&
                (scope == VMVariableScope.Parameters || scope == VMVariableScope.StackObjectAttributeByParameter))
                csClass.UseParams = true;
        }

        public static bool IsBig(VMVariableScope scope)
        {
            return (scope == VMVariableScope.TempXL || scope == VMVariableScope.MoneyOverHead32Bit);
        }

        public static string GetConstant(CSTranslationContext context, VMVariableScope scope, short data)
        {
            switch (scope)
            {
                case VMVariableScope.Literal:
                    return data.ToString();
                case VMVariableScope.Tuning:
                    return GetTuningVariable(context, (ushort)data).ToString();
            }
            throw new Exception("Scope " + scope.ToString() + " not supported as Constant.");
        }

        public static string SetStatement(CSTranslationContext context, VMVariableScope scope, short data, string op, string value, bool big)
        {
            UpdateUsing(context, scope);
            if (big && !IsBig(scope))
                value = $"(short)({value})";

            if (ScopeMutable(scope))
            {
                //the GetVariable version is mutable, meaning we can use +=, %= etc.
                var exp = GetExpression(context, scope, data, big);
                return $"{exp} {op} {value};";
            }
            else
            {
                if (op != "=")
                {
                    op = op.Substring(0, 1);
                    var exp = GetExpression(context, scope, data, big);
                    value = $"(short)({exp} {op} {value})"; //eg. "x[0] += 1" becomes "x[0] = x[0] + 1";
                }
                switch (scope)
                {
                    case VMVariableScope.MyObjectAttributes: //0
                        return $"context.Caller.SetAttribute({(ushort)data}, {value});";

                    case VMVariableScope.StackObjectAttributes: //1
                        return $"context.StackObject.SetAttribute({(ushort)data}, {value});";

                    case VMVariableScope.TargetObjectAttributes: //2
                        throw new Exception("Target Object is Deprecated!");

                    case VMVariableScope.MyObject: //3
                        return $"context.Caller.SetValue({EnumValueName((VMStackObjectVariable)data)}, {value});";

                    case VMVariableScope.StackObject: //4
                        return $"context.StackObject.SetValue({EnumValueName((VMStackObjectVariable)data)}, {value});";

                    case VMVariableScope.TargetObject: //5
                        throw new Exception("Target Object is Deprecated!");

                    case VMVariableScope.Global: //6
                        return $"context.VM.SetGlobalValue({(ushort)data}, {value});";

                    case VMVariableScope.Literal: //7
                        /** Huh? **/
                        return $"";
                    case VMVariableScope.TreeAdRange: //12
                        return $""; //can't set this!
                    case VMVariableScope.StackObjectTemp: //13
                        break;

                    case VMVariableScope.MyMotives: //14
                        return $"((VMAvatar)context.Caller).SetMotiveData({EnumValueName((VMMotive)data)}, {value});";

                    case VMVariableScope.StackObjectMotives: //15
                        return $"(context.StackObject as VMAvatar)?.SetMotiveData({EnumValueName((VMMotive)data)}, {value});";

                    case VMVariableScope.StackObjectSlot: //16
                        return $"/* attempting to assign stack object slot? */"; //can't set this!
                        //throw new Exception("Not implemented...");

                    case VMVariableScope.StackObjectMotiveByTemp: //17
                        return $"((VMAvatar)context.StackObject).SetMotiveData((VMMotive)temps[{data}], {value});";

                    case VMVariableScope.MyPersonData: //18
                        return $"((VMAvatar)context.Caller).SetPersonData({EnumValueName((VMPersonDataVariable)data)}, {value});";

                    case VMVariableScope.StackObjectPersonData: //19
                        return $"((VMAvatar)context.StackObject).SetPersonData({EnumValueName((VMPersonDataVariable)data)}, {value});";

                    case VMVariableScope.MySlot: //20
                        throw new Exception("Not implemented...");

                    case VMVariableScope.StackObjectDefinition: //21
                        return $""; //you can't set this!

                    case VMVariableScope.StackObjectAttributeByParameter: //22
                        return $"context.StackObject.SetAttribute((ushort)args[{data}], {value});";

                    //23: room by temp 0
                    //24: neighbor in stack object

                    case VMVariableScope.Tuning: //26
                        return $""; //you can't set this!

                    case VMVariableScope.DynSpriteFlagForTempOfStackObject: //27
                        return $"context.StackObject.SetDynamicSpriteFlag((ushort)temps[{data}], {value} > 0);";

                    case VMVariableScope.TreeAdPersonalityVar: //28
                        return $""; //you can't set this!

                    case VMVariableScope.TreeAdMin: //29
                        return $""; //you can't set this!

                    case VMVariableScope.MyPersonDataByTemp: //30
                        return $"((VMAvatar)context.Caller).SetPersonData((VMPersonDataVariable)temps[{data}], {value});";

                    case VMVariableScope.StackObjectPersonDataByTemp: //31
                        return $"((VMAvatar)context.StackObject).SetPersonData((VMPersonDataVariable)temps[{data}], {value});";

                    case VMVariableScope.NeighborPersonData: //32
                        throw new Exception("Not implemented...");

                    case VMVariableScope.JobData: //33
                        throw new Exception("Not implemented...");

                    case VMVariableScope.NeighborhoodData: //34
                        throw new Exception("Not implemented...");

                    case VMVariableScope.StackObjectFunction: //35
                        return $""; //you can't set this!

                    case VMVariableScope.MyTypeAttr: //36
                        if (context.TS1) return $"Content.Content.Get().Neighborhood.SetTATT((context.Caller.MasterDefinition ?? context.Caller.Object.OBJ).TypeAttrGUID, {data}, {value});";
                        return $"";

                    case VMVariableScope.StackObjectTypeAttr: //37
                        if (context.TS1) return $"Content.Content.Get().Neighborhood.SetTATT((context.StackObject.MasterDefinition ?? context.StackObject.Object.OBJ).TypeAttrGUID, {data}, {value});";
                        return $"";

                    case VMVariableScope.NeighborsObjectDefinition: //38
                        return $""; //you can't set this!

                    case VMVariableScope.StackObjectAttributeByTemp: //41
                        return $"context.StackObject.SetAttribute((ushort)temps[{data}], {value});";

                    case VMVariableScope.CityTime: //43
                    case VMVariableScope.TSOStandardTime: //44
                    case VMVariableScope.GameTime: //45
                        return $""; //you can't set this!

                    case VMVariableScope.MyList: //46
                        return $"/** Set My List **/";
                        //throw new Exception("Not implemented...");

                    case VMVariableScope.StackObjectList: //47
                        return $"/** Set Stack Obj List **/"; //you can't set this!
                        //throw new Exception("Not implemented...");

                    case VMVariableScope.MoneyOverHead32Bit: //48
                        return $"((VMAvatar)context.Caller).ShowMoneyHeadline({value});";

                    case VMVariableScope.MyLeadTileAttribute: //49
                        return $"context.Caller.MultitileGroup.BaseObject.SetAttribute({(ushort)data}, {value});";

                    case VMVariableScope.StackObjectLeadTileAttribute: //50
                        return $"context.StackObject.MultitileGroup.BaseObject.SetAttribute({(ushort)data}, {value});";

                    case VMVariableScope.MyLeadTile: //51
                        return $""; //you can't set this!

                    case VMVariableScope.StackObjectLeadTile: //52
                        return $""; //you can't set this!

                    case VMVariableScope.StackObjectMasterDef: //53
                        return $"";

                    case VMVariableScope.FeatureEnableLevel: //54
                        throw new Exception("Not implemented...");

                    case VMVariableScope.MyAvatarID: //59
                        return $""; //you can't set this!
                }
            }
            return $"VMMemory.SetVariable(context, {EnumValueName(scope)}, {data}, {value});";
        }

        public static short GetTuningVariable(TranslationContext ctx, ushort data)
        {
            var tableID = (ushort)(data >> 7);
            var keyID = (ushort)(data & 0x7F);

            int mode = 0;
            if (tableID < 64) mode = 0;
            else if (tableID < 128) { tableID = (ushort)((tableID - 64)); mode = 1; }
            else if (tableID < 192) { tableID = (ushort)((tableID - 128)); mode = 2; }

            /** Dynamic Tuning not available when writing constant tuning **/

            uint targID = 0;
            Dictionary<uint, short> tuningCache = null;

            var myRes = (ctx.ObjectRes ?? ctx.SemiGlobalRes ?? ctx.GlobalRes);
            if (myRes == null) return (short)data;
            switch (mode)
            {
                case 0: //local
                    tuningCache = myRes.TuningCache;
                    targID = ((uint)(tableID + 4096) << 16) | keyID;
                    break;
                case 1: //semi globals
                    targID = ((uint)(tableID + 8192) << 16) | keyID;
                    if (ctx.SemiGlobalRes != null)
                        tuningCache = ctx.SemiGlobalRes.TuningCache;
                    else
                        tuningCache = myRes.TuningCache;
                    break;
                case 2: //global
                    targID = ((uint)(tableID + 256) << 16) | keyID;
                    tuningCache = ctx.GlobalRes.TuningCache;
                    break;
            }

            short value;
            if (tuningCache.TryGetValue(targID, out value)) return value;
            //throw new Exception("Could not find tuning constant!");
            myRes.Recache();
            return 0;
        }
    }
}
