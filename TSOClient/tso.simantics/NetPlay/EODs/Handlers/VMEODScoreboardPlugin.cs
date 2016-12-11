using FSO.SimAntics.NetPlay.EODs.Archetypes;
using FSO.SimAntics.NetPlay.EODs.Utils;
using FSO.SimAntics.NetPlay.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace FSO.SimAntics.NetPlay.EODs.Handlers
{
    public class VMEODScoreboardPlugin : VMBasicEOD<object>
    {
        private EODPersist<VMEODScoreboardData> Persist;

        public VMEODScoreboardPlugin(VMEODServer server) : base(server, "scoreboard")
        {
            Persist = new EODPersist<VMEODScoreboardData>(server);

            PlaintextHandlers["scoreboard_updatecolor"] = UpdateColor;
            PlaintextHandlers["scoreboard_updatescore"] = UpdateScore;
            PlaintextHandlers["scoreboard_setscore"] = SetScore;
        }

        private void SetScore(string evt, string body, VMEODClient client)
        {
            var parts = body.Split(',');
            if (parts.Length != 2) { return; }

            VMEODScoreboardTeam team;
            short score;

            if (!Enum.TryParse<VMEODScoreboardTeam>(parts[0], out team) ||
                !short.TryParse(parts[1], out score))
            {
                return;
            }

            if (score < 0) { score = 0; }
            if (score > 999) { score = 999; }

            Persist.Patch(current =>
            {
                var result = current.Clone();
                switch (team)
                {
                    case VMEODScoreboardTeam.LHS:
                        result.LHSScore = score;
                        client.SendOBJEvent(new Model.VMEODEvent((short)VMEODScoreboardEvent.SetLHSScore, result.LHSScore));
                        break;
                    case VMEODScoreboardTeam.RHS:
                        result.RHSScore = score;
                        client.SendOBJEvent(new Model.VMEODEvent((short)VMEODScoreboardEvent.SetRHSScore, result.RHSScore));
                        break;
                }

                client.Send("scoreboard_state", result);
                return result;
            });
        }

        private void UpdateScore(string evt, string body, VMEODClient client)
        {
            var parts = body.Split(',');
            if (parts.Length != 2) { return; }

            VMEODScoreboardTeam team;
            short difference;

            if (!Enum.TryParse<VMEODScoreboardTeam>(parts[0], out team) ||
                !short.TryParse(parts[1], out difference))
            {
                return;
            }

            Persist.Patch(current =>
            {
                var result = current.Clone();
                switch (team)
                {
                    case VMEODScoreboardTeam.LHS:
                        result.LHSScore += difference;
                        if (result.LHSScore < 0) { result.LHSScore = 0; }
                        if (result.LHSScore > 999) { result.LHSScore = 999; }

                        client.SendOBJEvent(new Model.VMEODEvent((short)VMEODScoreboardEvent.SetLHSScore, result.LHSScore));
                        break;
                    case VMEODScoreboardTeam.RHS:
                        result.RHSScore += difference;
                        if(result.RHSScore < 0) { result.RHSScore = 0; }
                        if (result.RHSScore > 999) { result.RHSScore = 999; }

                        client.SendOBJEvent(new Model.VMEODEvent((short)VMEODScoreboardEvent.SetRHSScore, result.RHSScore));
                        break;
                }

                client.Send("scoreboard_state", result);
                return result;
            });
        }

        private void UpdateColor(string evt, string body, VMEODClient client)
        {
            var parts = body.Split(',');
            if (parts.Length != 2) { return; }

            VMEODScoreboardTeam team;
            VMEODScoreboardColor color;

            if(!Enum.TryParse<VMEODScoreboardTeam>(parts[0], out team) ||
                !Enum.TryParse<VMEODScoreboardColor>(parts[1], out color)){
                return;
            }
            

            Persist.Patch(current =>
            {
                var result = current.Clone();
                switch (team)
                {
                    case VMEODScoreboardTeam.LHS:
                        result.LHSColor = color;
                        client.SendOBJEvent(new Model.VMEODEvent((short)VMEODScoreboardEvent.SetLHSColor, (short)color));
                        break;
                    case VMEODScoreboardTeam.RHS:
                        result.RHSColor = color;
                        client.SendOBJEvent(new Model.VMEODEvent((short)VMEODScoreboardEvent.SetRHSColor, (short)color));
                        break;
                }
                return result;
            });
        }

        protected override void OnConnected(VMEODClient client)
        {
            //Send current state to the user
            Persist.GetData().ContinueWith(x =>
            {
                if(!x.IsFaulted && x.Result != null){
                    client.Send("scoreboard_state", x.Result);
                }
            });
        }
    }

    public enum VMEODScoreboardEvent : short
    {
        SetLHSScore = 1,
        SetRHSScore = 2,
        SetLHSColor = 3,
        SetRHSColor = 4
    }

    public enum VMEODScoreboardTeam : byte
    {
        LHS = 1,
        RHS = 2
    }

    public enum VMEODScoreboardColor : byte
    {
        Red = 0,
        Blue = 1,
        Yellow = 2,
        Green = 3,
        Orange = 4,
        Purple = 5,
        White = 6,
        Black = 7
    }

    public class VMEODScoreboardData : VMSerializable
    {
        public VMEODScoreboardColor LHSColor { get; set; }
        public VMEODScoreboardColor RHSColor { get; set; }

        public short LHSScore { get; set; }
        public short RHSScore { get; set; }

        public VMEODScoreboardData()
        {
            //Defaults
            LHSColor = VMEODScoreboardColor.Red;
            RHSColor = VMEODScoreboardColor.Blue;
        }

        public VMEODScoreboardData(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                Deserialize(new BinaryReader(stream));
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            LHSColor = (VMEODScoreboardColor)reader.ReadByte();
            RHSColor = (VMEODScoreboardColor)reader.ReadByte();
            LHSScore = reader.ReadInt16();
            RHSScore = reader.ReadInt16();
        }

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write((byte)LHSColor);
            writer.Write((byte)RHSColor);
            writer.Write((short)LHSScore);
            writer.Write((short)RHSScore);
        }

        public VMEODScoreboardData Clone()
        {
            return new VMEODScoreboardData {
                LHSColor = LHSColor,
                RHSColor = RHSColor,
                LHSScore = LHSScore,
                RHSScore = RHSScore
            };
        }
    }
}
