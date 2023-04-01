using FSO.SimAntics.Engine;
using System.IO;

namespace FSO.SimAntics.Marshals.Threads
{
    internal class VMDirectControlFrameMarshal : VMStackFrameMarshal
    {
        public VMDirectControlState State;

        public VMDirectControlFrameMarshal() { }
        public VMDirectControlFrameMarshal(int version) : base(version) { }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);

            State.X = reader.ReadInt32();
            State.Z = reader.ReadInt32();

            // ...read input
            ref var input = ref State.Input;

            input.ID = reader.ReadInt32();
            input.InputIntensity = reader.ReadInt32();
            input.Direction = reader.ReadInt16();
            input.LookDirectionInt = reader.ReadInt16();
            input.LookDirectionReal = new Microsoft.Xna.Framework.Vector3(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
                );
            input.Sprint = reader.ReadBoolean();

            // Input end

            State.XVelocity = reader.ReadInt32();
            State.ZVelocity = reader.ReadInt32();
            State.IdleFrames = reader.ReadInt32();
            State.Reserved = reader.ReadInt32();
            State.Reserved2 = reader.ReadInt32();
        }

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);

            writer.Write(State.X);
            writer.Write(State.Z);

            // ...write input
            ref var input = ref State.Input;

            writer.Write(input.ID);
            writer.Write(input.InputIntensity);
            writer.Write(input.Direction);
            writer.Write(input.LookDirectionInt);

            writer.Write(input.LookDirectionReal.X);
            writer.Write(input.LookDirectionReal.Y);
            writer.Write(input.LookDirectionReal.Z);
            writer.Write(input.Sprint);

            // Input end

            writer.Write(State.XVelocity);
            writer.Write(State.ZVelocity);
            writer.Write(State.IdleFrames);
            writer.Write(State.Reserved);
            writer.Write(State.Reserved2);
        }
    }
}
