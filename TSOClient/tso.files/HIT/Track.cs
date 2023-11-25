using System;
using System.IO;

namespace FSO.Files.HIT
{
    /// <summary>
    /// TRK is a CSV format that defines a HIT track.
    /// </summary>
    public class Track
    {
        private bool TWODKT = false; //Optional encoding as Pascal string, typical Maxis...
        public string MagicNumber;
        public uint Version;
        public string TrackName;
        public uint SoundID;
        public uint TrackID;
        public HITArgs ArgType;
        public HITControlGroups ControlGroup;
        public HITDuckingPriorities DuckingPriority;
        public uint Looped;
        public uint Volume;

        public bool LoopDefined = false;

        //ts1
        public uint SubroutineID;
        public uint HitlistID;

        /// <summary>
        /// Creates a new track.
        /// </summary>
        /// <param name="Filedata">The data to create the track from.</param>
        public Track(byte[] Filedata)
        {
            BinaryReader Reader = new BinaryReader(new MemoryStream(Filedata));

            MagicNumber = new string(Reader.ReadChars(4));

            if(MagicNumber == "2DKT")
                TWODKT = true;

            int CurrentVal = 8;
            string data;

            if(!TWODKT)
                data = new string(Reader.ReadChars(Filedata.Length));
            else
                data = new string(Reader.ReadChars(Reader.ReadInt32()));
            string[] Values = data.Split(',');

            //MagicNumber = Values[0];
            Version = ParseHexString(Values[1]);
            TrackName = Values[2];
            SoundID = ParseHexString(Values[3]);
            TrackID = ParseHexString(Values[4]);
            if (Values[5] != "\r\n" && Values[5] != "ETKD" && Values[5] != "") //some tracks terminate here...
            {
                ArgType = (HITArgs)ParseHexString(Values[5]);
                ControlGroup = (HITControlGroups)ParseHexString(Values[7]);

                if (Version == 2)
                    CurrentVal++;

                CurrentVal += 3; //skip two unknowns and clsid

                DuckingPriority = (HITDuckingPriorities)ParseHexString(Values[CurrentVal]);
                CurrentVal++;
                Looped = ParseHexString(Values[CurrentVal]);
                LoopDefined = true;
                CurrentVal++;
                Volume = ParseHexString(Values[CurrentVal]);
            }

            Reader.Close();
        }

        public Track() { }

        private uint ParseHexString(string input)
        {
            bool IsHex = false;

            if (input == "") return 0;
            if (input.StartsWith("0x"))
            {
                input = input.Substring(2);
                IsHex = true;
            }
            else if (input.Contains("a") || input.Contains("b") || input.Contains("c") || input.Contains("d") || input.Contains("e") || input.Contains("f"))
            {
                IsHex = true;
            }

            if (IsHex)
            {
                return Convert.ToUInt32(input, 16);
            }
            else
            {
                try
                {
                    return Convert.ToUInt32(input);
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }
    }
}
