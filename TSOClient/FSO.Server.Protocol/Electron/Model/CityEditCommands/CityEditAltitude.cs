using FSO.Common.Serialization;
using Mina.Core.Buffer;
using System.Runtime.InteropServices;

namespace FSO.Server.Protocol.Electron.Model.CityEditCommands
{
    public class CityEditAltitude : CityEditBase
    {
        public CityEditBitmap Bitmap;
        public short[] AltitudeDeltas;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            base.Deserialize(input, context);
            Bitmap = new CityEditBitmap(input);
            var altBytes = input.GetSlice(Bitmap.Width * Bitmap.Height * sizeof(ushort)).GetBytes();
            AltitudeDeltas = MemoryMarshal.Cast<byte, short>(altBytes).ToArray();
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            base.Serialize(output, context);

            Bitmap.Serialize(output);
            output.Put(MemoryMarshal.Cast<short, byte>(AltitudeDeltas).ToArray());
        }

        public void Trim()
        {
            var before = Bitmap;
            var deltas = AltitudeDeltas;
            var trimmed = Bitmap.Trim();
            Bitmap = trimmed;

            int width = before.Width;
            int twidth = trimmed?.Width ?? 0;
            int theight = trimmed?.Height ?? 0;
            AltitudeDeltas = new short[twidth * theight];

            foreach (var line in before.GetSetLines())
            {
                var from = deltas.AsSpan(line.y * width + line.x, line.count);
                var to = AltitudeDeltas.AsSpan((line.y - trimmed.Y) * twidth + line.x - trimmed.X, line.count);

                from.CopyTo(to);
            }
        }
    }
}
