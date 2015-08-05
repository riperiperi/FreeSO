/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Text;
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
            StreamReader Reader = new StreamReader(new MemoryStream(FileData));
            bool EventState = false, OptionsState = false, TrackDataState = false;

            while (!Reader.EndOfStream)
            {
                string CurrentLine = Reader.ReadLine().Trim().Replace("\r\n", "");

                switch (CurrentLine)
                {
                    case "[EventMappingEquate]":
                        EventState = true;
                        break;
                    case "[Options]":
                        OptionsState = true;
                        break;
                    case "[TrackData]":
                        TrackDataState = true;
                        break;
                }

                if (!CurrentLine.Contains("["))
                {
                    if (!CurrentLine.Contains("]"))
                    {
                        if (!CurrentLine.Contains(""))
                        {
                            //EventMappingEquate fields look like: kSndobPlay=1
                            if (EventState)
                            {
                                string[] EventParams = CurrentLine.Split("=".ToCharArray());
                                EventMappingEquate EMappingEquate = new EventMappingEquate();
                                EMappingEquate.Label = EventParams[0];
                                EMappingEquate.Value = int.Parse(EventParams[1]);
                            }
                            //Options fields look like: Version=1   
                            else if (OptionsState)
                            {
                                string[] OptionParams = CurrentLine.Split("=".ToCharArray());

                                switch (OptionParams[0])
                                {
                                    case "Version":
                                        m_Version = int.Parse(OptionParams[1]);
                                        break;
                                    case "LoadPriority":
                                        m_LoadPriority = int.Parse(OptionParams[1]);
                                        break;
                                }
                            }
                            //TrackData fields look like: 0xb0f4=0x10
                            else if (TrackDataState)
                            {
                                string[] TrackParams = CurrentLine.Split("=".ToCharArray());
                                TrackData TData = new TrackData();
                                TData.FileID = long.Parse(TrackParams[0]);
                                TData.SubRoutineOffset = int.Parse(TrackParams[1]);
                            }
                        }
                    }
                }
            }
        }

        public Hot(string Filepath)
        {
            StreamReader Reader = new StreamReader(File.Open(Filepath, FileMode.Open, FileAccess.Read, FileShare.Read));
            bool EventState = false, OptionsState = false, TrackDataState = false;

            while (!Reader.EndOfStream)
            {
                string CurrentLine = Reader.ReadLine().Trim().Replace("\r\n", "");

                switch (CurrentLine)
                {
                    case "[EventMappingEquate]":
                        EventState = true;
                        break;
                    case "[Options]":
                        OptionsState = true;
                        break;
                    case "[TrackData]":
                        TrackDataState = true;
                        break;
                }

                if (!CurrentLine.Contains("["))
                {
                    if (!CurrentLine.Contains("]"))
                    {
                        if (!CurrentLine.Contains(""))
                        {
                            //EventMappingEquate fields look like: kSndobPlay=1
                            if (EventState)
                            {
                                string[] EventParams = CurrentLine.Split("=".ToCharArray());
                                EventMappingEquate EMappingEquate = new EventMappingEquate();
                                EMappingEquate.Label = EventParams[0];
                                EMappingEquate.Value = int.Parse(EventParams[1]);
                            }
                            //Options fields look like: Version=1   
                            else if (OptionsState)
                            {
                                string[] OptionParams = CurrentLine.Split("=".ToCharArray());

                                switch (OptionParams[0])
                                {
                                    case "Version":
                                        m_Version = int.Parse(OptionParams[1]);
                                        break;
                                    case "LoadPriority":
                                        m_LoadPriority = int.Parse(OptionParams[1]);
                                        break;
                                }
                            }
                            //TrackData fields look like: 0xb0f4=0x10
                            else if (TrackDataState)
                            {
                                string[] TrackParams = CurrentLine.Split("=".ToCharArray());
                                TrackData TData = new TrackData();
                                TData.FileID = long.Parse(TrackParams[0]);
                                TData.SubRoutineOffset = int.Parse(TrackParams[1]);
                            }
                        }
                    }
                }
            }
        }
    }
}
