using FSO.SimAntics.Engine.Scopes;

namespace FSO.SimAntics.Entities
{
    public static class VMFootsteps
    {
        private struct VMFootstepType
        {
            public readonly string Shoes;
            public readonly string NoShoes;

            public VMFootstepType(string shoes, string noShoes = null)
            {
                Shoes = shoes;
                NoShoes = noShoes;
            }

            public string GetEvent(bool shoes)
            {
                return shoes || NoShoes == null ? Shoes : NoShoes;
            }
        }

        private static VMFootstepType[] FootstepByHardness = [
            new VMFootstepType("footstep_soft", "footstep_soft_noshoe"),
            new VMFootstepType("footstep_medium", "footstep_medium_noshoe"),
            new VMFootstepType("footstep_hard", "footstep_hard_noshoe"),
        ];

        private static VMFootstepType TerrainFootstep = new VMFootstepType("footstep_terrain");
        private static VMFootstepType SnowFootstep = new VMFootstepType("footstep_snow");
        private static VMFootstepType SwimFootstep = new VMFootstepType("footstep_swim_stroke");

        // These are notably the same GUID in TSO and TS1.
        private static Dictionary<uint, VMFootstepType> SoundByGUID = new()
        {
            { 0x63416BA1, new VMFootstepType("footstep_ash") },
            { 0x3E7470F6, new VMFootstepType("footstep_puddle") },
            { 0x7F907075, new VMFootstepType("footstep_trash") },
            { 0x4415A98E, new VMFootstepType("footstep_roach") },
        };

        public static string GetFootstepEvent(VM vm, VMAvatar ava)
        {
            var suit = (VMPersonSuits)ava.GetPersonData(Model.VMPersonDataVariable.CurrentOutfit);
            bool shoes = !(suit == VMPersonSuits.Naked ||
                suit == VMPersonSuits.DefaultSleepwear ||
                suit == VMPersonSuits.DefaultSwimwear ||
                suit == VMPersonSuits.DynamicSleepwear ||
                suit == VMPersonSuits.DynamicSwimwear);

            VMFootstepType footstep;
            var pos = ava.Position;
            var arch = vm.Context.Architecture;

            ushort floorTileId = 0;

            if (pos.TileX >= 0 && pos.TileY >= 0 && pos.TileX < arch.Width && pos.TileY < arch.Height)
            {
                // Check for any objects sharing the tile that have special sounds
                var objs = vm.Context.ObjectQueries.GetObjectsAt(ava.Position);
                if (objs != null)
                {
                    var lookup = SoundByGUID;
                    foreach (var obj in objs)
                    {
                        if (lookup.TryGetValue(obj.Object.OBJ.GUID, out footstep))
                        {
                            return footstep.GetEvent(shoes);
                        }
                    }
                }

                floorTileId = arch.GetPreciseFloor(pos);
            }


            // Check the floor tile

            if (floorTileId == 0)
            {
                // Walking on terrain
                if (vm.Context.Architecture.Terrain.LightType == Content.Model.TerrainType.SNOW && vm.TS1)
                {
                    footstep = SnowFootstep;
                }
                else
                {
                    footstep = TerrainFootstep;
                }
            }
            else
            {
                int hardness = 2;

                if (Content.Content.Get().WorldFloors.Entries.TryGetValue(floorTileId, out var floor))
                {
                    hardness = floor.Hardness;
                }

                footstep = FootstepByHardness[hardness];
            }

            return footstep.GetEvent(shoes);
        }
    }
}
