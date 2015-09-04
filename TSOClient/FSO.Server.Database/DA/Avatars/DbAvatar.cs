using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.Avatars
{
    public class DbAvatar
    {
        public uint avatar_id { get; set; }
        public int shard_id { get; set; }
        public uint user_id { get; set; }
        public string name { get; set; }
        public DbAvatarGender gender { get; set; }
        public uint date { get; set; }
        public byte skin_tone { get; set; }
        public ulong head { get; set; }
        public ulong body { get; set; }
        public string description { get; set; }
    }

    public enum DbAvatarGender
    {
        male,
        female
    }
}
