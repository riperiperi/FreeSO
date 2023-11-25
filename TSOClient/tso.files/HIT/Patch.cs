namespace FSO.Files.HIT
{
    public class Patch
    {
        public string Name;
        public string Filename;
        public bool Looped;
        public bool Piano;

        public uint FileID; //patches are stubbed out in TSO.
        public bool TSO;

        public Patch(uint id)
        {
            FileID = id;
            TSO = true;
        }

        public Patch(string patchString)
        {
            var elems = patchString.Split(',');
            if (elems.Length > 1) Name = elems[1];
            if (elems.Length > 2) Filename = elems[2].Substring(1, elems[2].Length-2).Replace('\\', '/');
            if (elems.Length > 3) Looped = elems[3] != "0";
            if (elems.Length > 4) Piano = elems[4] != "0";
        }
    }
}
