using System;
using System.Collections.Generic;
using System.IO;

namespace FSO.Files.HIT
{
    public class FSC
    {
        /// <summary>
        /// FSC is a tabulated plaintext format that describes a sequence of notes to be played. In this game it is used to sequence the ambient sounds.
        /// The conditions in which the sequence is randomized are not entirely apparent, and have been mostly guessed.
        /// </summary>
        /// 

        public List<FSCNote> Notes;

        public string VersionCode;

        public ushort MasterVolume;
        public ushort Priority;
        public ushort Min;
        public ushort Max;
        public ushort Rows; //these seem to be outright lies, but let's leave them in
        public ushort Columns;
        public ushort Tempo;
        public ushort BPB; //beats per bar
        public ushort SelX;
        public ushort SelY;
        public ushort QuanX;
        public ushort QuanY;
        public ushort DiffX;
        public ushort DiffY;

        public List<int> RandomJumpPoints;
        
        /// <summary>
        /// Creates a new hsm file.
        /// </summary>
        /// <param name="Filedata">The data to create the hsm from.</param>
        public FSC(byte[] Filedata)
        {
            ReadFile(new MemoryStream(Filedata));
        }

        /// <summary>
        /// Creates a new hsm file.
        /// </summary>
        /// <param name="Filedata">The path to the data to create the hsm from.</param>
        public FSC(string Filepath)
        {
            ReadFile(File.Open(Filepath, FileMode.Open, FileAccess.Read, FileShare.Read));
        }

        private void ReadFile(Stream stream)
        {
            var io = new StreamReader(stream);

            Notes = new List<FSCNote>();
            RandomJumpPoints = new List<int>();
            VersionCode = io.ReadLine();
            var line = io.ReadLine();

            while (line.StartsWith("#"))
                line = io.ReadLine();

            //Header
            string[] Head = line.Split('\t');
            MasterVolume = Convert.ToUInt16(Head[1]);
            Priority = Convert.ToUInt16(Head[2]);
            Min = Convert.ToUInt16(Head[3]);
            Max = Convert.ToUInt16(Head[4]);
            Rows = Convert.ToUInt16(Head[5]);
            Columns = Convert.ToUInt16(Head[6]);
            Tempo = Convert.ToUInt16(Head[7]);
            BPB = Convert.ToUInt16(Head[8]);
            SelX = Convert.ToUInt16(Head[9]);
            SelY = Convert.ToUInt16(Head[10]);
            if(Head[11][0] != '-') QuanX = Convert.ToUInt16(Head[11]);
            if (Head[12][0] != '-') QuanY = Convert.ToUInt16(Head[12]);
            DiffX = Convert.ToUInt16(Head[13]);
            DiffY = Convert.ToUInt16(Head[14]);

            line = io.ReadLine();

            while (line.StartsWith("#") || line.StartsWith("cells"))
                line = io.ReadLine();

            while (!io.EndOfStream) //read notes
            {
                string line2 = io.ReadLine();
                string[] Values = line2.Split('\t');
                if (!line.StartsWith("#") && Values.Length == 20)
                {
                    var note = new FSCNote()
                    {
                        Volume = Convert.ToUInt16(Values[1]),
                        Rand = Values[2] != "0",
                        LRPan = Convert.ToUInt16(Values[3]),
                        FBPan = Convert.ToUInt16(Values[4]),
                        Rand2 = Values[5] != "0",

                        Fin = Convert.ToUInt16(Values[6]),
                        FOut = Convert.ToUInt16(Values[7]),
                        dly = Convert.ToUInt16(Values[8]),
                        Rand3 = Values[9] != "0",
                        Loop = Convert.ToUInt16(Values[10]),

                        Loop2 = Values[11] != "0",
                        Quant = Convert.ToUInt16(Values[12]),
                        Prob = Convert.ToUInt16(Values[13]),
                        pitchL = Convert.ToInt16(Values[14]),
                        pitchR = Convert.ToInt16(Values[15]),

                        Fast = Values[16] != "0",
                        GroupID = Convert.ToUInt16(Values[17]),
                        Stereo = Values[18] != "0",
                        Filename = Values[19]
                    };
                    if (note.Rand) RandomJumpPoints.Add(Notes.Count);
                    Notes.Add(note);
                }
            }

            io.Close();
        }
    }

    public struct FSCNote
    {
        public ushort Volume; //0-1024
        public bool Rand;
        public ushort LRPan; //0-1024
        public ushort FBPan; //0-1024, front back
        public bool Rand2;

        public ushort Fin;
        public ushort FOut;
        public ushort dly;
        public bool Rand3; //what
        public ushort Loop;

        public bool Loop2; //might be count then decider here
        public ushort Quant; //but then what is this?
        public ushort Prob; //probably random probability, not sure of range (0-16?)
        public short pitchL; //pitch offsets
        public short pitchR;

        public bool Fast;
        public ushort GroupID;
        public bool Stereo;
        public string Filename;
    }
}
