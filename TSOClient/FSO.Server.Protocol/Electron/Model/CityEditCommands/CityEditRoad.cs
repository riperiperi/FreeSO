using FSO.Common.Serialization;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Electron.Model.CityEditCommands
{
    public class CityEditRoad : CityEditBase
    {
        public int StartX;
        public int StartY;
        public int Length;
        public int Direction;
        public bool Delete;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            base.Deserialize(input, context);
            StartX = input.GetInt32();
            StartY = input.GetInt32();

            Length = input.GetInt32();
            Direction = input.GetInt32();
            Delete = input.GetBool();

            if (Direction < 0 && Direction > 3)
            {
                throw new Exception($"Road direction {Direction} out of range.");
            }
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            base.Serialize(output, context);
            output.PutInt32(StartX);
            output.PutInt32(StartY);

            output.PutInt32(Length);
            output.PutInt32(Direction);
            output.PutBool(Delete);
        }
    }
}
