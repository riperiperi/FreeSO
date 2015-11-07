using FSO.Common.DataService.Framework;
using FSO.Common.DataService.Framework.Attributes;
using FSO.Server.Common;
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

        public uint _Lot_LeaderID;
        public uint Lot_LeaderID
        {
            get { return _Lot_LeaderID; }
            set
            {
                _Lot_LeaderID = value;
                NotifyPropertyChanged("Lot_LeaderID");
            }
        }

        public string _Lot_Description;

        [Persist]
        public string Lot_Description
        {
            get { return _Lot_Description; }
            set
            {
                _Lot_Description = value;
                NotifyPropertyChanged("Lot_Description");
            }
        }


        private uint _Lot_LastCatChange;
        public uint Lot_LastCatChange
        {
            get { return _Lot_LastCatChange; }
            set
            {
                _Lot_LastCatChange = value;
                NotifyPropertyChanged("Lot_HoursSinceLastLotCatChange");
            }
        }

        private byte _Lot_Category;

        [Persist]
        public byte Lot_Category
        {
            get { return _Lot_Category; }
            set
            {
                _Lot_Category = value;
                NotifyPropertyChanged("Lot_Category");
            }
        }

        public uint Lot_HoursSinceLastLotCatChange
        {
            get{
                var diff = Epoch.Now - Lot_LastCatChange;
                return (uint)TimeSpan.FromMilliseconds(diff).TotalMilliseconds;
            }
            set{
                throw new Exception("You cannot set this member");
            }
        }

        private List<uint> _Lot_OwnerVec;
        public List<uint> Lot_OwnerVec
        {
            get { return _Lot_OwnerVec; }
            set
            {
                _Lot_OwnerVec = value;
                NotifyPropertyChanged("Lot_OwnerVec");
            }
        }

        private List<uint> _Lot_RoommateVec;
        public List<uint> Lot_RoommateVec
        {
            get { return _Lot_RoommateVec; }
            set { _Lot_RoommateVec = value; NotifyPropertyChanged("Lot_RoommateVec"); }
        }

        public Location Lot_Location { get; set; }
    }
}
