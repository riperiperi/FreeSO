using System;
using System.Collections.Generic;
using System.IO;

namespace FSO.Files.HIT
{
    public class FSC
    {
        /// <summary>
        /// FSC is a tabulated plaintext format that describes a sequence of notes to be played. In this game it is used to sequence the ambient sounds.
        /// </summary>
        /// 

        public List<FSCNote> Notes;

        /// <summary>
        /// Each row contains a sequence of notes that play in parallel with other rows. (essentially, a track)
        /// A column contains all the notes that need to evaluate to move to the next column. (essentially, one compound note)
        /// Typically this is used to play ambience at a set interval, using the note probability to essentially
        /// select one of many possible sounds to play at a time (or none)
        /// </summary>
        public FSCNote[][] NoteColumns;

        public string VersionCode;

        public ushort MasterVolume;
        public ushort Priority;
        public ushort Min;
        public ushort Max;
        public ushort Rows;
        public ushort Columns;
        public ushort Tempo;
        public ushort BPB; //beats per bar
        public ushort SelX;
        public ushort SelY;
        public ushort QuanX;
        public ushort QuanY;
        public ushort DiffX;
        public ushort DiffY;
        
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

            NoteColumns = new FSCNote[Columns][];

            for (int i = 0; i < NoteColumns.Length; i++)
            {
                NoteColumns[i] = new FSCNote[Rows];
            }

            int column = 0;
            int row = 0;

            line = io.ReadLine();

            while (!io.EndOfStream) //read notes
            {
                line = io.ReadLine();
                string[] Values = line.Split('\t');
                if (!line.StartsWith("#") && Values.Length >= 20)
                {
                    var note = new FSCNote()
                    {
                        Volume = Convert.ToUInt16(Values[1]),
                        RandomVolume = Values[2] != "0",
                        LRPan = Convert.ToUInt16(Values[3]),
                        FBPan = Convert.ToUInt16(Values[4]),
                        RandomPan = Values[5] != "0",

                        Fin = Convert.ToUInt16(Values[6]),
                        FOut = Convert.ToUInt16(Values[7]),
                        dly = Convert.ToUInt16(Values[8]),
                        Rand3 = Values[9] != "0",
                        Loop = Convert.ToUInt16(Values[10]),

                        Loop2 = Values[11] != "0",
                        Quant = Convert.ToUInt16(Values[12]),
                        Prob = Convert.ToUInt16(Values[13]),
                        pitchL = Convert.ToInt16(Values[14]),
                        pitchH = Convert.ToInt16(Values[15]),

                        Fast = Values[16] != "0",
                        GroupID = Convert.ToUInt16(Values[17]),
                        Stereo = Values[18] != "0",
                        Filename = Values[19],
                        ExclusionCells = Values.Length > 20 ? ParseExclusionCells([.. Values.Skip(20).Where(x => x != "")]) : null
                    };

                    NoteColumns[column][row] = note;

                    // Notes fill columns one at a time. Move onto the next column each note.
                    column++;
                    if (column >= Columns)
                    {
                        // When the row is full, move to the next row.
                        column = 0;
                        row++;
                        if (row >= NoteColumns.Length)
                        {
                            break;
                        }
                    }
                }
            }

            io.Close();
        }

        private static FSCExclusionCell[] ParseExclusionCells(string[] split)
        {
            if (split.Length == 0)
            {
                return null;
            }

            var result = new FSCExclusionCell[split.Length];

            for (int i = 0; i < split.Length; i++)
            {
                string elem = split[i];

                if (elem.StartsWith("\"(") && elem.EndsWith(")\""))
                {
                    string[] parts = elem.Substring(2, elem.Length - 4).Split(',');
                    if (parts.Length != 2 || !int.TryParse(parts[0], out int x) || !int.TryParse(parts[1], out int y))
                    {
                        return null;
                    }

                    result[i] = new FSCExclusionCell()
                    {
                        X = x,
                        Y = y
                    };
                }
                else
                {
                    return null;
                }
            }

            return result;
        }
    }

    public struct FSCExclusionCell
    {
        public int X;
        public int Y;
    }

    public struct FSCNote
    {
        public ushort Volume; //0-1024
        public bool RandomVolume;
        public ushort LRPan; //0-1024
        public ushort FBPan; //0-1024, front back
        public bool RandomPan;

        public ushort Fin;
        public ushort FOut;
        public ushort dly; // Delay?
        public bool Rand3; // Randomize delay?
        public ushort Loop;

        public bool Loop2; //might be count then decider here
        public ushort Quant; //but then what is this?
        public ushort Prob; // random probability of note playing in %
        public short pitchL; //pitch range: low
        public short pitchH; // pitch range: high

        public bool Fast;
        public ushort GroupID;
        public bool Stereo;
        public string Filename;

        /// <summary>
        /// Assumed behaviour: if this note plays, then the notes in the following cells cannot play.
        /// Encoded in the file after the normal values, as "(x,y)" separated by tabs.
        /// </summary>
        public FSCExclusionCell[] ExclusionCells;
    }
}
