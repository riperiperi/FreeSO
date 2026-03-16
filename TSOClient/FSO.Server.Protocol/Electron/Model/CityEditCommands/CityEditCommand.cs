using FSO.Common.Serialization;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Electron.Model.CityEditCommands
{
    public struct CityEditCommand
    {
        private static Dictionary<Type, CityUpdateCommandType> TypeToEnum = new()
        {
            {
                typeof(CityEditAltitude), CityUpdateCommandType.Altitude
            },
            {
                typeof(CityEditPaint), CityUpdateCommandType.Paint
            },
            {
                typeof(CityEditRoad), CityUpdateCommandType.Road
            }
        };

        public CityEditBase Command;

        public CityEditCommand(CityEditBase command)
        {
            Command = command;
        }

        public CityEditCommand(IoBuffer input, ISerializationContext context)
        {
            Deserialize(input, context);
        }

        public void Deserialize(IoBuffer input, ISerializationContext context)
        {
            var eType = (CityUpdateCommandType)input.Get();

            Command = eType switch
            {
                CityUpdateCommandType.Altitude => new CityEditAltitude(),
                CityUpdateCommandType.Paint => new CityEditPaint(),
                CityUpdateCommandType.Road => new CityEditRoad(),
                _ => throw new NotSupportedException($"Unknown city command type: {eType}")
            };

            Command.Deserialize(input, context);
        }

        public void Serialize(IoBuffer output, ISerializationContext context)
        {
            if (!TypeToEnum.TryGetValue(Command.GetType(), out var type))
            {
                throw new NotSupportedException($"Unknown city command type: {Command.GetType()}");
            }

            output.Put((byte)type);
            Command.Serialize(output, context);
        }
    }
}
