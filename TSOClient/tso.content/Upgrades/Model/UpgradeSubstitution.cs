using System.IO;

namespace FSO.Content.Upgrades.Model
{
    public class UpgradeSubstitution
    {
        /// <summary>
        /// Tuning value to replace. Format: table:id... eg 4097:0. Can also point to a group: "G0".
        /// </summary>
        public string Old = "0:0";

        /// <summary>
        /// Target value. Constants are prefixed with C, eg C4097:1... and Values are prefixed with V, eg V25, V0, V-600.
        /// </summary>
        public string New = "V0";

        public void Load(int version, BinaryReader reader)
        {
            Old = reader.ReadString();
            New = reader.ReadString();
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(Old);
            writer.Write(New);
        }
    }
}
