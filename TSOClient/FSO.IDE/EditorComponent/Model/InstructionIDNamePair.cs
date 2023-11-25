namespace FSO.IDE.EditorComponent.Model
{
    public class InstructionIDNamePair
    {
        public string Name;
        public ushort ID;

        public InstructionIDNamePair(string name, ushort id)
        {
            Name = name;
            ID = id;
        }

        public override string ToString() //used for winforms rendering, eg. listbox
        {
            string prepend = "";
            if (ID>255)
            {
                if (ID>8191)
                {
                    prepend = "Semi-Global: ";
                }
                else if (ID > 4095)
                {
                    prepend = "Private: ";
                }
                else
                {
                    prepend = "Global: ";
                }
            }

            return prepend + Name;
        }
    }
}
