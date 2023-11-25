using FSO.Common.Serialization;
using Mina.Core.Buffer;
using System.Collections.Generic;

namespace FSO.Server.Protocol.Electron.Packets
{
    public class NhoodCandidateList : AbstractElectronPacket
    {
        public bool NominationMode;
        public List<NhoodCandidate> Candidates;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            NominationMode = input.GetBool();
            int candCount = input.GetInt32();

            Candidates = new List<NhoodCandidate>();
            for (int i=0; i<candCount; i++)
            {
                var candidate = new NhoodCandidate()
                {
                    ID = input.GetUInt32(),
                    Name = input.GetPascalVLCString(),
                    Rating = input.GetUInt32()
                };

                if (!NominationMode)
                {
                    candidate.LastNhoodName = input.GetPascalVLCString();
                    candidate.LastNhoodID = input.GetUInt32();
                    candidate.TermNumber = input.GetUInt32();
                    candidate.Message = input.GetPascalVLCString();
                }
                Candidates.Add(candidate);
            }
        }

        public override ElectronPacketType GetPacketType()
        {
            return ElectronPacketType.NhoodCandidateList;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutBool(NominationMode);
            output.PutInt32(Candidates.Count);

            foreach (var candidate in Candidates)
            {
                output.PutUInt32(candidate.ID);
                output.PutPascalVLCString(candidate.Name);
                output.PutUInt32(candidate.Rating);

                if (!NominationMode)
                {
                    output.PutPascalVLCString(candidate.LastNhoodName);
                    output.PutUInt32(candidate.LastNhoodID);
                    output.PutUInt32(candidate.TermNumber);
                    output.PutPascalVLCString(candidate.Message);
                }
            }
        }
    }

    public class NhoodCandidate
    {
        public uint ID;
        public string Name;
        public uint Rating;

        public string LastNhoodName = "";
        public uint LastNhoodID;
        public uint TermNumber;
        public string Message = "";
    }
}
