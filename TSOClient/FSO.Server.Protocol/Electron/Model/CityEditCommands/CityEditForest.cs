using FSO.Common.Serialization;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Electron.Model.CityEditCommands
{
    public class CityEditForest : CityEditBase
    {
        public bool Erasing;
        public byte ForestType;
        public CityEditBitmap Bitmap;
        public byte[] Intensities;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            base.Deserialize(input, context);
            Erasing = input.GetBool();
            ForestType = input.Get();
            Bitmap = new CityEditBitmap(input);
            Intensities = input.GetSlice(Bitmap.Width * Bitmap.Height).GetBytes();
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            base.Serialize(output, context);

            output.PutBool(Erasing);
            output.Put(ForestType);
            Bitmap.Serialize(output);
            output.Put(Intensities);
        }

        public void Trim()
        {
            var before = Bitmap;
            var intensities = Intensities;
            var trimmed = Bitmap.Trim();
            Bitmap = trimmed;

            int width = before.Width;
            int twidth = trimmed?.Width ?? 0;
            int theight = trimmed?.Height ?? 0;
            Intensities = new byte[twidth * theight];

            foreach (var line in before.GetSetLines())
            {
                var from = intensities.AsSpan(line.y * width + line.x, line.count);
                var to = Intensities.AsSpan((line.y - trimmed.Y) * twidth + line.x - trimmed.X, line.count);

                from.CopyTo(to);
            }
        }
    }
}
