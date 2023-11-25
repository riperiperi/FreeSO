using System;
using System.Collections.Generic;
using System.IO;

namespace FSO.Files.HIT
{
    /// <summary>
    /// Enumeration constants.
    /// </summary>
    public struct EventMappingEquate
    {
        public string Label;
        public int Value;
    }

    /// <summary>
    /// This section assigns a sound ID to a number of subroutines 
    /// (exported or not) in the corresponding HIT file.
    /// </summary>
    public struct TrackData
    {
        //The syntax for TrackData is A = B, where A is the sound's File ID 
        //and B is the offset to the subroutine in the accompanying HIT file.
        public long FileID;
        public int SubRoutineOffset;
    }

    /// <summary>
    /// HOT (short for HIT Options Table) is an ini format that defines 
    /// enumeration constants and track data for HIT binary files.
    /// </summary>
    public class Hot
    {
        private int m_Version;
        private int m_LoadPriority;
        private List<EventMappingEquate> m_EventMappingEquations = new List<EventMappingEquate>();
        private List<TrackData> m_TrackDataList = new List<TrackData>();
        public Dictionary<uint, Track> Tracks = new Dictionary<uint, Track>();
        public Dictionary<uint, Patch> Patches = new Dictionary<uint, Patch>();
        public Dictionary<uint, Hitlist> Hitlists = new Dictionary<uint, Hitlist>();
        public Dictionary<uint, uint> TrackData = new Dictionary<uint, uint>();
        public Dictionary<string, EVTEntry> Events = new Dictionary<string, EVTEntry>();
        private Dictionary<string, int> EventMappingEquate = new Dictionary<string, int>();
        public HSM AsmNames;

        /// <summary>
        /// Gets this Hot instance's list of EventMappingEquate instances.
        /// </summary>
        public List<EventMappingEquate> EventMappingEquations
        {
            get { return m_EventMappingEquations; }
        }

        /// <summary>
        /// Gets this Hot instance's list of TrackData instances.
        /// </summary>
        public List<TrackData> TrackDataList
        {
            get { return m_TrackDataList; }
        }

        public Hot(byte[] FileData)
        {
            LoadFrom(FileData);
        }

        public void LoadFrom(byte[] FileData)
        {
            StreamReader Reader = new StreamReader(new MemoryStream(FileData));
            HotReadMode ActiveState = HotReadMode.None;

            while (!Reader.EndOfStream)
            {
                string CurrentLine = Reader.ReadLine().Trim().Replace("\r\n", "");

                switch (CurrentLine)
                {
                    case "[EventMappingEquate]":
                        ActiveState = HotReadMode.EventMappingEquate;
                        break;
                    case "[Options]":
                        ActiveState = HotReadMode.Options;
                        break;
                    case "[TrackData]":
                        ActiveState = HotReadMode.TrackData;
                        break;
                    case "[EventMapping]":
                        //equivalent to .evt file
                        //(name)=(eventMappingEquate as event type),(trackid),0,0,0,0
                        ActiveState = HotReadMode.EventMapping;
                        break;
                    case "[Track]":
                        //equivalent to a lot of track files
                        //(trackid)=0,(subroutine),(volume),(arguments),(duckingPriority),(controlGroup),(soundPressureLevel),@(hitlistID),(patchID)
                        //patch id is usually 0, in favor of single item hitlists
                        ActiveState = HotReadMode.Track;
                        break;
                    case "[Patch]":
                        //(patchid)=(name),(filenameInQuotes),(looped),(piano),0,0,0
                        ActiveState = HotReadMode.Patch;
                        break;
                    case "[GlobalHitList]":
                        //(hitlistid)=(hitlistString)
                        //note: many hitlists contain just one patch
                        ActiveState = HotReadMode.GlobalHitList;
                        break;
                }

                if (!CurrentLine.Contains("["))
                {
                    if (!CurrentLine.Contains("]"))
                    {
                        if (CurrentLine != "")
                        {
                            string[] Params = CurrentLine.Split("=".ToCharArray());
                            //EventMappingEquate fields look like: kSndobPlay=1
                            switch (ActiveState)
                            {
                                case HotReadMode.EventMappingEquate:
                                    EventMappingEquate EMappingEquate = new EventMappingEquate();
                                    EMappingEquate.Label = Params[0];
                                    EMappingEquate.Value = int.Parse(Params[1]);
                                    EventMappingEquate[EMappingEquate.Label] = EMappingEquate.Value;
                                    break;
                                //Options fields look like: Version=1   
                                case HotReadMode.Options:
                                    switch (Params[0])
                                    {
                                        case "Version":
                                            m_Version = int.Parse(Params[1]);
                                            break;
                                        case "LoadPriority":
                                            m_LoadPriority = int.Parse(Params[1]);
                                            break;
                                    }
                                    break;
                                //TrackData fields look like: 0xb0f4=0x10
                                case HotReadMode.TrackData:
                                    TrackData.Add(Convert.ToUInt32(Params[0], 16), Convert.ToUInt32(Params[1], 16));
                                    break;
                                case HotReadMode.EventMapping:
                                    var commaSplit = Params[1].Split(',');
                                    Events[Params[0].ToLowerInvariant()] = new EVTEntry
                                    {
                                        Name = Params[0].ToLowerInvariant(),
                                        EventType = (uint)ParseEME(commaSplit[0]),
                                        TrackID = (commaSplit.Length>1)?(uint)ParseEME(commaSplit[1]):0
                                    };
                                    break;
                                case HotReadMode.Track:
                                    var tid = uint.Parse(Params[0]);
                                    var tcSplit = Params[1].Split(',');
                                    
                                    var trk = new Track()
                                    {
                                        SubroutineID = 0,//(uint)HSMConst(tcSplit[1]),
                                        Volume = (uint)ParseEME(tcSplit[2]),
                                        ArgType = (HITArgs)ParseEME(tcSplit[3]),
                                        DuckingPriority = (HITDuckingPriorities)ParseEME(tcSplit[4]),
                                        ControlGroup = (HITControlGroups)ParseEME(tcSplit[5]),
                                        HitlistID = (uint)HSMConst(tcSplit[7].Substring(1)), //cut out @
                                        SoundID = (uint)ParseEME(tcSplit[8])
                                    };

                                    if (trk.HitlistID != 0 && TrackData.ContainsKey(trk.HitlistID)) trk.SubroutineID = TrackData[trk.HitlistID];
                                    if (trk.SoundID != 0 && TrackData.ContainsKey(trk.SoundID)) trk.SubroutineID = TrackData[trk.SoundID];

                                    Tracks[tid] = trk;
                                    break;
                                case HotReadMode.Patch:
                                    var pid = uint.Parse(Params[0]);
                                    var patch = new Patch(Params[1]);
                                    Patches[pid] = patch;
                                    break;
                                case HotReadMode.GlobalHitList:
                                    var hid = uint.Parse(Params[0]);
                                    try
                                    {
                                        var hitlist = Hitlist.HitlistFromString(Params[1]);
                                        Hitlists[hid] = hitlist;
                                    } catch (Exception)
                                    {
                                        /*
                                         * todo: saxophone seems to reference an hsm.
                                         * 20016=sulsaxj_way_aa
                                         * 20017=sulsaxk_way_solo
                                         * 20018=sulsaxl_way_fin
                                         * these labels are in the hsm but they have a different case...
                                         */
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
        }

        private int HSMConst(string input)
        {
            int result = 0;
            AsmNames?.Constants?.TryGetValue(input, out result);
            return result;
        }

        private int ParseEME(string eme)
        {
            int result = 0;
            if (int.TryParse(eme, out result)) return result;
            EventMappingEquate.TryGetValue(eme, out result);
            return result;
        }

        public Hot(string Filepath) : this(File.ReadAllBytes(Filepath))
        {
        }

        public Hot(string Filepath, HSM myAsm)
        {
            AsmNames = myAsm;
            LoadFrom(File.ReadAllBytes(Filepath));
        }
    }

    public enum HotReadMode
    {
        None,
        EventMappingEquate,
        Options,
        TrackData,
        EventMapping,
        Track,
        Patch,
        GlobalHitList
    }
}
