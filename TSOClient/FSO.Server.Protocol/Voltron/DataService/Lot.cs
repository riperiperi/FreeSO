using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Protocol.Voltron.DataService
{
    public class Lot
    {
        public byte Lot_NumOccupants { get; set; }
        public bool Lot_IsOnline { get; set; }
        public string Lot_Name { get; set; }
        public uint Lot_Price { get; set; }


        public List<uint> Lot_OwnerVec { get; set; }
        public List<uint> Lot_RoommateVec { get; set; }

        public Location Lot_Location { get; set; }
    }
}
