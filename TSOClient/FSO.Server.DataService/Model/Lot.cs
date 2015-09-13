using FSO.Common.DataService.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.DataService.Model
{
    public class Lot : AbstractModel
    {
        private byte _Lot_NumOccupants;
        public byte Lot_NumOccupants
        {
            get { return _Lot_NumOccupants; }
            set
            {
                _Lot_NumOccupants = value;
                NotifyPropertyChanged("Lot_NumOccupants");
            }
        }

        private bool _Lot_IsOnline;
        public bool Lot_IsOnline
        {
            get { return _Lot_IsOnline; }
            set
            {
                _Lot_IsOnline = value;
                NotifyPropertyChanged("Lot_IsOnline");
            }
        }

        private string _Lot_Name;
        public string Lot_Name
        {
            get { return _Lot_Name; }
            set
            {
                _Lot_Name = value;
                NotifyPropertyChanged("Lot_Name");
            }
        }

        private uint _Lot_Price;
        public uint Lot_Price
        {
            get { return _Lot_Price; }
            set
            {
                _Lot_Price = value;
                NotifyPropertyChanged("Lot_Price");
            }
        }

        public List<uint> Lot_OwnerVec { get; set; }
        public List<uint> Lot_RoommateVec { get; set; }
        public Location Lot_Location { get; set; }
    }
}
