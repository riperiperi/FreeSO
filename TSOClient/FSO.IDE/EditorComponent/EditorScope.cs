using FSO.Content;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using FSO.Files.Formats.OTF;
using FSO.IDE.EditorComponent.Model;
using FSO.SimAntics.Engine.Scopes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.IDE.EditorComponent
{
    public class EditorScope
    {
        public static GameGlobal Globals;
        public static IffFile Behaviour;

        public GameObject Object;
        public TPRP BHAVNames;
        public GameGlobalResource SemiGlobal;
        public string SemiGlobalName;

        public GameObject StackObject;
        public GameObject CallerObject;

        private STR AttributeTable;
        private BHAV Active;

        public EditorScope(GameObject obj, BHAV active)
        {
            Object = obj;
            var GLOBChunks = Object.Resource.List<GLOB>();
            if (GLOBChunks != null)
            {
                SemiGlobalName = GLOBChunks[0].Name;
                SemiGlobal = FSO.Content.Content.Get().WorldObjectGlobals.Get(SemiGlobalName).Resource;
            }

            AttributeTable = obj.Resource.Get<STR>(256);
            StackObject = obj;
            CallerObject = obj;

            BHAVNames = GetLabels(active.ChunkID);
            Active = active;
        }

        public TPRP GetLabels(ushort chunkID)
        {
            if (chunkID >= 8192)
            {
                // Semi-Global
                if (SemiGlobal != null)
                {
                    return SemiGlobal.Get<TPRP>(chunkID);
                }
                return null;
            }
            else if (chunkID >= 4096)
            {
                // Private sub-routine call
                if(Object != null)
                {
                    return Object.Resource.Get<TPRP>(chunkID);
                }
                return null;
            }
            else
            {
                // Global sub-routine call
                if(Globals != null)
                {
                    return Globals.Resource.Get<TPRP>(chunkID);
                }
                return null;
            }
        }

        public string GetSubroutineName(ushort id)
        {
            string preface = (id >= 8192) ? "Semi-global: " : ((id >= 4096) ? "Private: " : "Global: ");

            var bhav = GetBHAV(id);
            if (bhav == null) return preface + "#" + id.ToString() + " (missing)";
            else return preface + bhav.ChunkLabel;
        }

        public string GetVarScopeName(VMVariableScope scope)
        {
            return Behaviour.Get<STR>(132).GetString((int)scope);
        }

        public string GetVarName(VMVariableScope scope, short data)
        {
            switch (scope)
            {
                case VMVariableScope.StackObjectID:
                    return GetVarScopeName(scope);
                case VMVariableScope.Literal:
                    return data.ToString();
                case VMVariableScope.Parameters:
                    if (BHAVNames == null) break;
                    if (data < 0 || data >= BHAVNames.ParamNames.Length) break;
                    return BHAVNames.ParamNames[data].Trim(new char[] { (char)0xA3, (char)0x05 });
                case VMVariableScope.Local:
                    if (BHAVNames == null) break;
                    if (data < 0 || data >= BHAVNames.LocalNames.Length) break;
                    return BHAVNames.LocalNames[data].Trim(new char[] { (char)0xA3, (char)0x05 });
                default:
                    break;
            }
            var dataName = GetVarScopeDataName(scope, data);
            if (dataName == null) dataName = data.ToString();

            return GetVarScopeName(scope) + " " + dataName;
        }

        private List<ScopeDataDefinition> DataListFromSTR(STR strings, short startValue, string[] descriptions)
        {
            var result = new List<ScopeDataDefinition>();
            for (int i=0; i<strings.Length; i++)
            {
                result.Add(new ScopeDataDefinition(strings.GetString(i), startValue++, (descriptions== null)?"No Description Available.":descriptions[i]));
            }
            return result;
        }

        private List<ScopeDataDefinition> DataListFromStrings(string[] strings, short startValue, string[] descriptions)
        {
            var result = new List<ScopeDataDefinition>();
            for (int i = 0; i < strings.Length; i++)
            {
                result.Add(new ScopeDataDefinition(strings[i], startValue++, (descriptions == null) ? "No Description Available." : descriptions[i]));
            }
            return result;
        }

        public List<ScopeDataDefinition> GetVarScopeDataNames(VMVariableScope scope)
        {
            switch (scope)
            {
                case VMVariableScope.MyMotives:
                case VMVariableScope.StackObjectMotives:
                    return DataListFromSTR(Behaviour.Get<STR>(134), 0, null);
                case VMVariableScope.MyPersonData:
                case VMVariableScope.NeighborPersonData:
                case VMVariableScope.StackObjectPersonData:
                    return DataListFromSTR(Behaviour.Get<STR>(200), 0, null);
                case VMVariableScope.MyObject:
                case VMVariableScope.StackObject:
                    return DataListFromSTR(Behaviour.Get<STR>(141), 0, null);
                case VMVariableScope.MyObjectAttributes:
                    var callerAttr = CallerObject.Resource.Get<STR>(256);
                    if (callerAttr == null) return DataListFromStrings(MakeEnumeratedStrings("Attribute ", Object.OBJ.NumAttributes), 0, null);
                    return DataListFromSTR(callerAttr, 0, null);
                case VMVariableScope.StackObjectAttributes:
                case VMVariableScope.StackObjectLeadTileAttribute:
                    var stackAttr = StackObject.Resource.Get<STR>(256);
                    if (stackAttr == null) return DataListFromStrings(MakeEnumeratedStrings("Attribute ", Object.OBJ.NumAttributes), 0, null);
                    return DataListFromSTR(stackAttr, 0, null);
                case VMVariableScope.Global:
                    return DataListFromSTR(Behaviour.Get<STR>(129), 0, null);
                case VMVariableScope.Parameters:
                case VMVariableScope.StackObjectAttributeByParameter:
                    if (BHAVNames == null) return DataListFromStrings(MakeEnumeratedStrings("Parameter ", Math.Max(4, (int)Active.Args)), 0, null);
                    return DataListFromStrings(BHAVNames.ParamNames, 0, null);
                case VMVariableScope.Local:
                    if (BHAVNames == null) return DataListFromStrings(MakeEnumeratedStrings("Local ", Active.Locals), 0, null);
                    return DataListFromStrings(BHAVNames.LocalNames, 0, null);
                case VMVariableScope.Temps:
                    return DataListFromStrings(MakeEnumeratedStrings("Temp ", 20), 0, null);
                case VMVariableScope.TempXL:
                    return DataListFromStrings(MakeEnumeratedStrings("TempXL ", 2), 0, null);
                default:
                    break;
            }
            return null;
        }

        public string[] MakeEnumeratedStrings(string prefix, int length)
        {
            var result = new string[length];
            for (int i=0; i<length; i++)
            {
                result[i] = prefix + i;
            }
            return result;
        }

        public string GetVarScopeDataName(VMVariableScope scope, short data)
        {
            switch (scope)
            {
                case VMVariableScope.Tuning:
                    return GetTuningVariableLabel((ushort)data) + " ("+GetTuningVariable((ushort)data)+")";
                case VMVariableScope.MyMotives:
                case VMVariableScope.StackObjectMotives:
                    return Behaviour.Get<STR>(134).GetString(data);
                case VMVariableScope.MyPersonData:
                case VMVariableScope.NeighborPersonData:
                case VMVariableScope.StackObjectPersonData:
                    return Behaviour.Get<STR>(200).GetString(data);
                case VMVariableScope.MyObject:
                case VMVariableScope.StackObject:
                    return Behaviour.Get<STR>(141).GetString(data);
                case VMVariableScope.MyObjectAttributes:
                    var callerAttr = CallerObject.Resource.Get<STR>(256);
                    if (callerAttr == null) break;
                    var attr = callerAttr.GetString(data);
                    return (attr == null) ? data.ToString() : attr;
                case VMVariableScope.StackObjectAttributes:
                case VMVariableScope.StackObjectLeadTileAttribute:
                    var stackAttr = StackObject.Resource.Get<STR>(256);
                    if (stackAttr == null) break;
                    attr = stackAttr.GetString(data);
                    return (attr == null) ? data.ToString() : attr;
                case VMVariableScope.Global:
                    return Behaviour.Get<STR>(129).GetString(data);
                default:
                    break;
            }
            return data.ToString();
        }

        public short GetTuningVariable(ushort data)
        {
            var tableID = (ushort)(data >> 7);
            var keyID = (ushort)(data & 0x7F);

            int mode = 0;
            if (tableID < 64) mode = 0;
            else if (tableID < 128) { tableID = (ushort)((tableID - 64)); mode = 1; }
            else if (tableID < 192) { tableID = (ushort)((tableID - 128)); mode = 2; }

            BCON bcon;
            OTFTable tuning;

            /** This could be in a BCON or an OTF **/

            switch (mode)
            {
                case 0: //local
                    bcon = Object.Resource.Get<BCON>((ushort)(tableID + 4096));
                    if (bcon != null) return (short)bcon.Constants[keyID];

                    tuning = Object.Resource.Get<OTFTable>((ushort)(tableID + 4096));
                    if (tuning != null) return (short)tuning.GetKey(keyID).Value;
                    break;
                case 1: //semi globals
                    ushort testTab = (ushort)(tableID + 8192);
                    bcon = Object.Resource.Get<BCON>(testTab);
                    if (bcon != null && keyID < bcon.Constants.Length) return (short)bcon.Constants[keyID];

                    tuning = Object.Resource.Get<OTFTable>(testTab);
                    if (tuning != null) return (short)tuning.GetKey(keyID).Value;

                    if (SemiGlobal != null)
                    {
                        bcon = SemiGlobal.Get<BCON>(testTab);
                        if (bcon != null && keyID < bcon.Constants.Length) return (short)bcon.Constants[keyID];

                        tuning = SemiGlobal.Get<OTFTable>(testTab);
                        if (tuning != null) return (short)tuning.GetKey(keyID).Value;
                    }
                    break;
                case 2: //global
                    bcon = Globals.Resource.Get<BCON>((ushort)(tableID + 256));
                    if (bcon != null && keyID < bcon.Constants.Length) return (short)bcon.Constants[keyID];

                    tuning = Globals.Resource.Get<OTFTable>((ushort)(tableID + 256));
                    if (tuning != null) return (short)tuning.GetKey(keyID).Value;
                    break;
            }
            
            return 0;
        }


        public string GetTuningVariableLabel(ushort data)
        {
            var tableID = (ushort)(data >> 7);
            var keyID = (ushort)(data & 0x7F);

            int mode = 0;
            if (tableID < 64) mode = 0;
            else if (tableID < 128) { tableID = (ushort)((tableID - 64)); mode = 1; }
            else if (tableID < 192) { tableID = (ushort)((tableID - 128)); mode = 2; }

            /** This could be in a BCON or an OTF **/
            BCON bcon;
            OTFTable tuning;
            switch (mode)
            {
                case 0:
                    bcon = Object.Resource.Get<BCON>((ushort)(tableID + 4096));
                    if (bcon != null)
                    {
                        return bcon.ChunkLabel + " #" + keyID;
                    }

                    tuning = Object.Resource.Get<OTFTable>((ushort)(tableID + 4096));
                    if (tuning != null)
                    {
                        return tuning.GetKey(keyID).Label;
                    }
                    break;
                case 1:
                    bcon = SemiGlobal.Get<BCON>((ushort)(tableID + 8192));
                    if (bcon != null)
                    {
                        return bcon.ChunkLabel + " #" + keyID;
                    }

                    tuning = SemiGlobal.Get<OTFTable>((ushort)(tableID + 8192));
                    if (tuning != null)
                    {
                        return tuning.GetKey(keyID).Label;
                    }
                    break;
                case 2:
                    bcon = Globals.Resource.Get<BCON>((ushort)(tableID + 256));
                    if (bcon != null)
                    {
                        return bcon.ChunkLabel + " #" + keyID;
                    }

                    tuning = Globals.Resource.Get<OTFTable>((ushort)(tableID + 256));
                    if (tuning != null)
                    {
                        return tuning.GetKey(keyID).Label;
                    }
                    break;
            }

            return "#"+data.ToString();
        }

        public BHAV GetBHAV(ushort id)
        {
            if (id >= 8192) return SemiGlobal.Get<BHAV>(id); //semiglobal
            else if (id >= 4096) return Object.Resource.Get<BHAV>(id); //private
            else return Globals.Resource.Get<BHAV>(id); //global
        }

        public List<InstructionIDNamePair> GetAllSubroutines(ScopeSource source)
        {
            var bhavs = GetAllResource<BHAV>(source);
            var output = new List<InstructionIDNamePair>();
            if (bhavs != null)
            {
                foreach (var bhav in bhavs)
                {
                    output.Add(new InstructionIDNamePair(bhav.ChunkLabel, bhav.ChunkID));
                }
            }

            output = output.OrderBy(o => o.Name).ToList();
            return output;
        }

        public List<T> GetAllResource<T>(ScopeSource source)
        {
            switch (source)
            {
                case ScopeSource.Private:
                    return Object.Resource.List<T>();
                case ScopeSource.SemiGlobal:
                    return (SemiGlobal == null) ? new List<T>():SemiGlobal.List<T>();
                case ScopeSource.Global:
                    return Globals.Resource.List<T>();
                default:
                    return new List<T>();
            }
        }

        public T GetResource<T>(ushort id, ScopeSource source)
        {
            switch (source)
            {
                case ScopeSource.Private:
                    return Object.Resource.Get<T>(id);
                case ScopeSource.SemiGlobal:
                    return SemiGlobal.Get<T>(id);
                case ScopeSource.Global:
                    return Globals.Resource.Get<T>(id);
                default:
                    return default(T);
            }
        }

        public OBJD GetOBJD()
        {
            return Object.OBJ;
        }

        public string GetFilename(ScopeSource scope)
        {
            switch (scope)
            {
                case ScopeSource.Global:
                    return "global";
                case ScopeSource.SemiGlobal:
                    return SemiGlobalName;
                default:
                    return Object.Resource.Name;
            }
            
        }

        public ScopeSource GetScopeFromID(ushort id)
        {
            if (id >= 8192) return ScopeSource.SemiGlobal;
            else if (id >= 4096) return ScopeSource.Private;
            else return ScopeSource.Global;
        }
        
    }

    public class ScopeDataDefinition
    {
        public string Name;
        public short Value;
        public string Description;
        public string Total;

        public ScopeDataDefinition(string name, short value, string desc)
        {
            Name = name;
            if (Name == "") Name = value.ToString();
            Value = value;
            Description = desc;
            Total = (value.ToString() + " " + name).ToLowerInvariant();
        }

        public override string ToString()
        {
            return Name;
        }

        public bool SearchMatch(string[] words)
        {
            int score = 0;
            foreach (var word in words)
            {
                if (Total.Contains(word)) score++;
            }
            return (score >= words.Length);
        }
    }

    public enum ScopeSource
    {
        Private,
        SemiGlobal,
        Global
    }
}
