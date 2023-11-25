using FSO.Common.DataService.Framework;

namespace FSO.Common.DataService.Model
{
    public class Relationship : AbstractModel
    {
        private ushort _Relationship_Days;
        public ushort Relationship_Days
        {
            get { return _Relationship_Days; }
            set
            {
                _Relationship_Days = value;
                NotifyPropertyChanged("Relationship_Days");
            }
        }

        private uint _Relationship_TargetID;
        public uint Relationship_TargetID
        {
            get { return _Relationship_TargetID; }
            set
            {
                _Relationship_TargetID = value;
                NotifyPropertyChanged("Relationship_TargetID");
            }
        }

        private sbyte _Relationship_STR;
        public sbyte Relationship_STR
        {
            get { return _Relationship_STR; }
            set
            {
                _Relationship_STR = value;
                NotifyPropertyChanged("Relationship_STR");
            }
        }

        private sbyte _Relationship_LTR;
        public sbyte Relationship_LTR
        {
            get { return _Relationship_LTR; }
            set
            {
                _Relationship_LTR = value;
                NotifyPropertyChanged("Relationship_LTR");
            }
        }

        private bool _Relationship_IsOutgoing;
        public bool Relationship_IsOutgoing
        {
            get { return _Relationship_IsOutgoing; }
            set
            {
                _Relationship_IsOutgoing = value;
                NotifyPropertyChanged("Relationship_IsOutgoing");
            }
        }

        private uint _Relationship_CommentID;
        public uint Relationship_CommentID
        {
            get { return _Relationship_CommentID; }
            set
            {
                _Relationship_CommentID = value;
                NotifyPropertyChanged("Relationship_CommentID");
            }
        }
    }
}
