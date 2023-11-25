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
        public int budget { get; set; }
        public byte privacy_mode { get; set; }

        //lot persist state beyond this point (very mutable)

        public byte[] motive_data { get; set; }
        //locks
        
        public byte skilllock { get; set; } //number of skill locks we had at last avatar save. this is usually handled by data service.
        public ushort lock_mechanical { get; set; }
        public ushort lock_cooking { get; set; }
        public ushort lock_charisma { get; set; }
        public ushort lock_logic { get; set; }
        public ushort lock_body { get; set; }
        public ushort lock_creativity { get; set; }
        //skills
        public ushort skill_mechanical { get; set; }
        public ushort skill_cooking { get; set; }
        public ushort skill_charisma { get; set; }
        public ushort skill_logic { get; set; }
        public ushort skill_body { get; set; }
        public ushort skill_creativity { get; set; }

        public ulong body_swimwear { get; set; }
        public ulong body_sleepwear { get; set; }
        public ulong body_current { get; set; }

        public ushort current_job { get; set; }
        public ushort is_ghost { get; set; }
        public ushort ticker_death { get; set; }
        public ushort ticker_gardener { get; set; }
        public ushort ticker_maid { get; set; }
        public ushort ticker_repairman { get; set; }

        public byte moderation_level { get; set; }
        public uint? custom_guid { get; set; }
        public uint move_date { get; set; }
        public uint name_date { get; set; }
        public int? mayor_nhood { get; set; }
    }


    public class DbTransactionResult
    {
        public bool success { get; set; }
        public int source_budget { get; set; }
        public int dest_budget { get; set; }
        public int amount { get; set; }
    }

    public enum DbAvatarGender
    {
        male,
        female
    }
}
