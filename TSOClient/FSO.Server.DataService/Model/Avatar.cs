
using FSO.Common.DataService.Framework;
using FSO.Common.DataService.Framework.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.DataService.Model
{
    public class Avatar : AbstractModel
    {
        [Key]
        public uint Avatar_Id { get; set; }

        private bool _Avatar_IsFounder;
        public bool Avatar_IsFounder {
            get { return _Avatar_IsFounder; }
            set { _Avatar_IsFounder = value; NotifyPropertyChanged("Avatar_IsFounder"); }
        }

        private string _Avatar_Name;
        public string Avatar_Name
        {
            get { return _Avatar_Name; }
            set { _Avatar_Name = value;  NotifyPropertyChanged("Avatar_Name"); }
        }

        private string _Avatar_Description { get; set; }
        public string Avatar_Description
        {
            get { return _Avatar_Description; }
            set { _Avatar_Description = value; NotifyPropertyChanged("Avatar_Description"); }
        }

        private bool _Avatar_IsParentalControlLocked { get; set; }
        public bool Avatar_IsParentalControlLocked
        {
            get { return _Avatar_IsParentalControlLocked; }
            set { _Avatar_IsParentalControlLocked = value;  NotifyPropertyChanged("Avatar_IsParentalControlLocked"); }
        }

        private bool _Avatar_IsOnline { get; set; }
        public bool Avatar_IsOnline
        {
            get { return _Avatar_IsOnline; }
            set { _Avatar_IsOnline = value; NotifyPropertyChanged("Avatar_IsOnline"); }
        }

        private uint _Avatar_LotGridXY;
        public uint Avatar_LotGridXY
        {
            get { return _Avatar_LotGridXY; }
            set { _Avatar_LotGridXY = value; NotifyPropertyChanged("Avatar_LotGridXY"); }
        }

        private uint _Avatar_Age;
        public uint Avatar_Age
        {
            get { return _Avatar_Age; }
            set { _Avatar_Age = value; NotifyPropertyChanged("Avatar_Age"); }
        }

        private ushort _Avatar_SkillsLockPoints;
        public ushort Avatar_SkillsLockPoints
        {
            get { return _Avatar_SkillsLockPoints; }
            set { _Avatar_SkillsLockPoints = value; NotifyPropertyChanged("Avatar_SkillsLockPoints"); }
        }

        private AvatarAppearance _Avatar_Appearance;
        public AvatarAppearance Avatar_Appearance
        {
            get { return _Avatar_Appearance; }
            set
            {
                _Avatar_Appearance = value;
                NotifyPropertyChanged("Avatar_Appearance");
            }
        }

        private AvatarSkills _Avatar_Skills;
        public AvatarSkills Avatar_Skills
        {
            get { return _Avatar_Skills; }
            set
            {
                _Avatar_Skills = value;
                NotifyPropertyChanged("Avatar_Skills");
            }
        }

        private List<Bookmark> _Avatar_BookmarksVec;
        public List<Bookmark> Avatar_BookmarksVec
        {
            get { return _Avatar_BookmarksVec; }
            set
            {
                _Avatar_BookmarksVec = value;
                NotifyPropertyChanged("Avatar_BookmarksVec");
            }
        }
    }
}
