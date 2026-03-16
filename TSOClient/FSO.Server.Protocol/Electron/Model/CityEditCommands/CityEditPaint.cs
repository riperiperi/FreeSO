using FSO.Common.Serialization;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Electron.Model.CityEditCommands
{
    public enum CityEditPaintType : byte
    {
        TerrainType,
        ForestDensity,
        ForestType,
    }

    public class CityEditPaint : CityEditBase
    {
        public CityEditPaintType Type;
        public byte Value;
        public CityEditBitmap Bitmap;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            base.Deserialize(input, context);
            Type = (CityEditPaintType)input.Get();
            Value = input.Get();
            Bitmap = new CityEditBitmap(input);
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            base.Serialize(output, context);

            output.Put((byte)Type);
            output.Put(Value);
            Bitmap.Serialize(output);
        }

        public void Trim()
        {
            Bitmap = Bitmap.Trim();
        }
    }
}
