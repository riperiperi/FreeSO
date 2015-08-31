using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Protocol.Voltron.DataService
{
    public class Avatar
    {
        public bool Avatar_IsFounder { get; set; }
        public string Avatar_Name { get; set; }
        public string Avatar_Description { get; set; }

        public bool Avatar_IsOnline { get; set; }
        public uint Avatar_LotGridXY { get; set; }
        public AvatarAppearance Avatar_Appearance { get; set; }

        public List<Bookmark> Avatar_BookmarksVec { get; set; }
    }
}
