using FSO.Common;
using FSO.Content;
using FSO.Content.Model;
using FSO.HIT;
using FSO.LotView;
using FSO.LotView.Model;
using FSO.SimAntics.Model.TSOPlatform;
using System.Diagnostics;

namespace FSO.SimAntics.Engine
{
    [Flags]
    public enum VMAmbientSoundType: ulong
    {
        AnimalsSongBirds = 1 << 0,
        MechanicalExplosions = 1 << 1,
        AnimalsFarm = 1 << 2,
        MechanicalGunshot = 1 << 3,
        MechanicalPlanes = 1 << 4,
        WeatherLightingThunder = 1 << 5,

        LoopBrook = 1 << 6,
        LoopCrowd = 1 << 7,
        LoopHeartbeat = 1 << 8,
        LoopIndoor = 1 << 9,
        LoopInsects = 1 << 10,
        LoopOcean = 1 << 11,
        LoopOutdoor = 1 << 12,
        LoopRain = 1 << 13,
        LoopTechno = 1 << 14,
        LoopStorm = 1 << 15,
        LoopTraffic = 1 << 16,
        LoopWind = 1 << 17,

        WeatherBreeze = 1 << 18,
        MechanicalConstruction = 1 << 19,
        AnimalsDog = 1 << 20,
        MechanicalDriveBy = 1 << 21,
        WeatherHowlingWind = 1 << 22,
        MechanicalIndustrial = 1 << 23,
        AnimalsInsects = 1 << 24,
        AnimalsJungle = 1 << 25,
        PeopleOffice = 1 << 26,
        PeopleRestaurant = 1 << 27,
        MechanicalSciBleeps = 1 << 28,
        MechanicalSirens = 1 << 29,
        AnimalsWolf = 1 << 30,
        AnimalsSeaBirds = 1ul << 31,
        WeatherRainDrops = 1ul << 32,
        PeopleMagic = 1ul << 33,
        MechanicalSmallMachines = 1ul << 34,
        PeopleScreams = 1ul << 35,
        AnimalsNightBirds = 1ul << 36,
        PeopleGym = 1ul << 37,
        PeopleGhost = 1ul << 38,

        TS1Ambience = 1 << 0,
        TS1NightLoop = 1 << 1
    }

    public class VMAmbientSound
    {
        private const VMAmbientSoundType AutoWeatherTypes = VMAmbientSoundType.LoopRain | VMAmbientSoundType.WeatherLightingThunder;
        private const VMAmbientSoundType AutoLocationTypes =
            VMAmbientSoundType.LoopOcean |
            VMAmbientSoundType.LoopBrook |
            VMAmbientSoundType.LoopInsects |
            VMAmbientSoundType.LoopOutdoor |
            VMAmbientSoundType.WeatherHowlingWind |
            VMAmbientSoundType.AnimalsSongBirds |
            VMAmbientSoundType.AnimalsSeaBirds |
            VMAmbientSoundType.AnimalsNightBirds;
        private const VMAmbientSoundType AllLoops =
            VMAmbientSoundType.LoopBrook |
            VMAmbientSoundType.LoopRain |
            VMAmbientSoundType.LoopCrowd |
            VMAmbientSoundType.LoopHeartbeat |
            VMAmbientSoundType.LoopIndoor |
            VMAmbientSoundType.LoopOutdoor |
            VMAmbientSoundType.LoopInsects |
            VMAmbientSoundType.LoopStorm |
            VMAmbientSoundType.LoopOcean |
            VMAmbientSoundType.LoopTechno |
            VMAmbientSoundType.LoopTraffic;

        private const float AmbienceTransitionTime = 6;
        private const float AmbienceDuckTime = 0.33f;
        private const float IndoorsVolumeDuck = 0.4f;
        private static VMAmbientSound ToTransition;

        public static bool ForceDisable;
        private static Dictionary<uint, Ambience> TSOAmbienceByGUID = new Dictionary<uint, Ambience>() //may want to load this from ambience.ini in future...
        {
            {0x3dd887a6, new Ambience("sounddata/ambience/daybirds/daybirds.fsc", false)},
            {0x3dd887aa, new Ambience("sounddata/ambience/explosions/explosions.fsc", false)},
            {0x7dd887ad, new Ambience("sounddata/ambience/farmanimals/farmanimals.fsc", false)},
            {0x9dd887af, new Ambience("sounddata/ambience/gunshots/gunshots.fsc", false)},
            {0xddd887b3, new Ambience("sounddata/ambience/planes/planes.fsc", false)},
            {0xfdd887b5, new Ambience("sounddata/ambience/thunder/thunder.fsc", false)},
            {0x1e128187, new Ambience("sounddata/ambience/breeze/breeze.fsc", false)},
            {0xfe128189, new Ambience("sounddata/ambience/construction/construction.fsc", false)},
            {0x5e12818c, new Ambience("sounddata/ambience/dog/dog.fsc", false)},
            {0xbe12818d, new Ambience("sounddata/ambience/driveby/driveby.fsc", false)},
            {0xde12818f, new Ambience("sounddata/ambience/howlingwind/howlingwind.fsc", false)},
            {0x1e128190, new Ambience("sounddata/ambience/indust/indust.fsc", false)},
            {0x3e128192, new Ambience("sounddata/ambience/insect/insect.fsc", false)},
            {0xbe128196, new Ambience("sounddata/ambience/jungle/jungle.fsc", false)},
            {0xde128198, new Ambience("sounddata/ambience/office/office.fsc", false)},
            {0x3e12819a, new Ambience("sounddata/ambience/restaurant/restaurant.fsc", false)},
            {0xbe12819c, new Ambience("sounddata/ambience/scibleeps/scibleeps.fsc", false)},
            {0x1e1281ac, new Ambience("sounddata/ambience/siren/siren.fsc", false)},
            {0x1e1281ad, new Ambience("sounddata/ambience/wolf/wolf.fsc", false)},
            {0xbe19bb2d, new Ambience("sounddata/ambience/seabirds/seabirds.fsc", false)},
            {0xde19bb31, new Ambience("sounddata/ambience/raindrops/raindrops.fsc", false)},
            {0xbe1a033e, new Ambience("sounddata/ambience/magic/magic.fsc", false)},
            {0xa9b9652a, new Ambience("sounddata/ambience/smallmachines/smallmachines.fsc", false)},
            {0xa9b96536, new Ambience("sounddata/ambience/screams/screams.fsc", false)},
            {0xa9b96539, new Ambience("sounddata/ambience/nightbirds/nightbirds.fsc", false)},
            {0xa9b9653c, new Ambience("sounddata/ambience/gym/gym.fsc", false)},
            {0xa9b9653e, new Ambience("sounddata/ambience/ghost/ghost.fsc", false)},

            //Loops

            {0x9e0bc19a, new Ambience("sounddata/ambience/loops/brook_lp.xa", true)},
            {0xfe0bc1a1, new Ambience("sounddata/ambience/loops/crowd_lp.xa", true)},
            {0x1e0bc1a3, new Ambience("sounddata/ambience/loops/heartbeat_lp.xa", true)},
            {0x5e0bc1a4, new Ambience("sounddata/ambience/loops/indoor_lp.xa", true)},
            {0x5e0bc1a6, new Ambience("sounddata/ambience/loops/insect_lp.xa", true)},
            {0xbe0bc1a9, new Ambience("sounddata/ambience/loops/ocean_lp.xa", true)},
            {0x1e0bc1ab, new Ambience("sounddata/ambience/loops/outdoor_lp.xa", true)},
            {0xde0bc1ad, new Ambience("sounddata/ambience/loops/rain_lp.xa", true)},
            {0x3e0bc2af, new Ambience("sounddata/ambience/loops/scifi_lp.xa", true)},
            {0x1e0bc2b2, new Ambience("sounddata/ambience/loops/storm_lp.xa", true)},
            {0x3e0bc2b4, new Ambience("sounddata/ambience/loops/traffic_lp.xa", true)},
            {0x1e0bc2b5, new Ambience("sounddata/ambience/loops/wind_lp.xa", true)}
        };

        private static List<VMCategorisedAmb> TSOSoundByBitField = new List<VMCategorisedAmb>() {
            new VMCategorisedAmb(0x3dd887a6, 0, "AnimalsSongBirds"),
            new VMCategorisedAmb(0x3dd887aa, 1, "MechanicalExplosions"),
            new VMCategorisedAmb(0x7dd887ad, 0, "AnimalsFarm"),
            new VMCategorisedAmb(0x9dd887af, 1, "MechanicalGunshot"),
            new VMCategorisedAmb(0xddd887b3, 1, "MechanicalPlanes"),
            new VMCategorisedAmb(0xfdd887b5, 2, "WeatherLightingThunder"),

            new VMCategorisedAmb(0x9e0bc19a, 4, "LoopBrook"),
            new VMCategorisedAmb(0xfe0bc1a1, 4, "LoopCrowd"),
            new VMCategorisedAmb(0x1e0bc1a3, 4, "LoopHeartbeat"),
            new VMCategorisedAmb(0x5e0bc1a4, 4, "LoopIndoor"),
            new VMCategorisedAmb(0x5e0bc1a6, 4, "LoopInsects"),
            new VMCategorisedAmb(0xbe0bc1a9, 4, "LoopOcean"),
            new VMCategorisedAmb(0x1e0bc1ab, 4, "LoopOutdoor"),
            new VMCategorisedAmb(0xde0bc1ad, 4, "LoopRain"),
            new VMCategorisedAmb(0x3e0bc2af, 4, "LoopTechno"),
            new VMCategorisedAmb(0x1e0bc2b2, 4, "LoopStorm"),
            new VMCategorisedAmb(0x3e0bc2b4, 4, "LoopTraffic"),
            new VMCategorisedAmb(0x1e0bc2b5, 4, "LoopWind"),

            new VMCategorisedAmb(0x1e128187, 2, "WeatherBreeze"),
            new VMCategorisedAmb(0xfe128189, 1, "MechanicalConstruction"),
            new VMCategorisedAmb(0x5e12818c, 0, "AnimalsDog"),
            new VMCategorisedAmb(0xbe12818d, 1, "MechanicalDriveBy"),
            new VMCategorisedAmb(0xde12818f, 2, "WeatherHowlingWind"),
            new VMCategorisedAmb(0x1e128190, 1, "MechanicalIndustrial"),
            new VMCategorisedAmb(0x3e128192, 0, "AnimalsInsects"),
            new VMCategorisedAmb(0xbe128196, 0, "AnimalsJungle"),
            new VMCategorisedAmb(0xde128198, 3, "PeopleOffice"),
            new VMCategorisedAmb(0x3e12819a, 3, "PeopleRestaurant"),
            new VMCategorisedAmb(0xbe12819c, 1, "MechanicalSciBleeps"),
            new VMCategorisedAmb(0x1e1281ac, 1, "MechanicalSirens"),
            new VMCategorisedAmb(0x1e1281ad, 0, "AnimalsWolf"),
            new VMCategorisedAmb(0xbe19bb2d, 0, "AnimalsSeaBirds"),
            new VMCategorisedAmb(0xde19bb31, 2, "WeatherRainDrops"),
            new VMCategorisedAmb(0xbe1a033e, 3, "PeopleMagic"),
            new VMCategorisedAmb(0xa9b9652a, 1, "MechanicalSmallMachines"),
            new VMCategorisedAmb(0xa9b96536, 3, "PeopleScreams"),
            new VMCategorisedAmb(0xa9b96539, 0, "AnimalsNightBirds"),
            new VMCategorisedAmb(0xa9b9653c, 3, "PeopleGym"),
            new VMCategorisedAmb(0xa9b9653e, 3, "PeopleGhost")
        };

        private static Dictionary<uint, Ambience> TS1AmbienceByGUID = new Dictionary<uint, Ambience>()
        {
            {0x00000001, new Ambience("sounddata/outdoors/sim_amb.fsc", false)},

            //Loops

            {0x00000002, new Ambience("sounddata/outdoors/nite_loop.xa", true)},
        };

        private static List<VMCategorisedAmb> TS1SoundByBitField = new List<VMCategorisedAmb>() {
            new VMCategorisedAmb(0x00000001, 0, "TS1Ambience", 2f),
            new VMCategorisedAmb(0x00000002, 1, "TS1NightLoop", 0.6f),
        };

        public Dictionary<uint, Ambience> AmbienceByGUID => TS1 ? TS1AmbienceByGUID : TSOAmbienceByGUID;
        public List<VMCategorisedAmb> SoundByBitField => TS1 ? TS1SoundByBitField : TSOSoundByBitField;


        public Dictionary<byte, AmbiencePlayer> ActiveSounds;
        public VMAmbientSoundType UserBits;
        public VMAmbientSoundType ActiveBits;
        public float Volume = 0;
        public long LastTimestamp = Stopwatch.GetTimestamp();

        private VMAmbientSoundType AutoBaseBits;
        private TerrainType BaseTerrain = TerrainType.GRASS;
        private bool Paused;
        private int UserCount;

        private float VolumeDuck = 1f;
        private float TargetVolumeDuck = 1f;
        private bool TS1;

        /// <summary>
        /// Handles ambient sound in lots.
        /// </summary>
        /// 
        public VMAmbientSound()
        {
            UserCount = 1;
            ActiveSounds = new Dictionary<byte, AmbiencePlayer>();
            TS1 = Content.Content.Get().TS1;
        }

        public static VMAmbientSound TryTransition()
        {
            if (VM.UseWorld && ToTransition != null)
            {
                var trans = ToTransition;

                ToTransition = null;
                
                return trans;
            }

            return new VMAmbientSound();
        }

        public void InitAutoBase(VM vm)
        {
            AutoBaseBits = TS1 ? VMAmbientSoundType.TS1Ambience : 0;

            if (vm.PlatformState is VMTSOLotState lot)
            {
                BaseTerrain = lot.Terrain.BlendN[1, 1].Base;

                var height = (lot.Terrain.Height[1, 1] + lot.Terrain.Height[1, 2] + lot.Terrain.Height[2, 1] + lot.Terrain.Height[2, 2]) / 4;

                bool hasWaterSurround = false;
                foreach (var blend in lot.Terrain.BlendN)
                {
                    if (blend.Base == TerrainType.WATER)
                    {
                        hasWaterSurround = true;
                    }
                }

                if (hasWaterSurround)
                {
                    if ((BaseTerrain == TerrainType.SAND && height < 20) || (BaseTerrain == TerrainType.WATER && height < 10))
                    {
                        AutoBaseBits |= VMAmbientSoundType.LoopOcean;
                    }
                    else
                    {
                        AutoBaseBits |= VMAmbientSoundType.LoopBrook;
                    }
                }
                else
                {
                    AutoBaseBits |= VMAmbientSoundType.LoopOutdoor;
                }

                if (height > 128)
                {
                    AutoBaseBits |= VMAmbientSoundType.WeatherHowlingWind;
                }
            }
        }

        public VMAmbientSoundType EvaluateAutoAmbience(VM vm)
        {
            var clock = vm.Context.Clock;

            if (TS1)
            {
                float dayPct = (clock.Hours + clock.Minutes / 60f) / 24f;
                bool isNightTS1 = clock.Hours > 18 || clock.Hours < 6;

                if (ActiveSounds.TryGetValue(0, out var basePlayer))
                {
                    basePlayer.SetLoopingNote(dayPct);
                }

                return AutoBaseBits | (isNightTS1 ? VMAmbientSoundType.TS1NightLoop : 0);
            }

            var tempBits = AutoBaseBits;

            bool isNight = clock.Hours > 20 || clock.Hours < 6;

            if (isNight)
            {
                if (BaseTerrain == TerrainType.GRASS)
                {
                    tempBits &= ~AllLoops;
                    tempBits |= VMAmbientSoundType.LoopInsects;
                }
                else if (BaseTerrain == TerrainType.SNOW)
                {
                    tempBits |= VMAmbientSoundType.AnimalsNightBirds;
                }
            }
            else
            {
                if ((AutoBaseBits & VMAmbientSoundType.LoopOcean) != 0)
                {
                    tempBits |= VMAmbientSoundType.AnimalsSeaBirds;
                }
                else
                {
                    tempBits |= VMAmbientSoundType.AnimalsSongBirds;
                }
            }

            var weather = vm.Context.Blueprint?.Weather;
            if (weather != null && weather.ParticleType == LotView.Components.ParticleType.RAIN && weather.WeatherIntensity > 0)
            {
                // Rain only fully replaces the basic outdoor or insects loops
                tempBits &= ~(VMAmbientSoundType.LoopOutdoor | VMAmbientSoundType.LoopInsects);

                tempBits |= VMAmbientSoundType.LoopRain;

                if (weather.WeatherIntensity > 0.5)
                {
                    tempBits |= VMAmbientSoundType.WeatherLightingThunder;
                }
            }

            return tempBits;
        }

        public void Tick(VM vm)
        {
            if (Paused)
            {
                return;
            }

            var tempBits = EvaluateAutoAmbience(vm) | UserBits;

            if (ActiveBits != tempBits)
            {
                SwitchAmbience(tempBits, false);
            }

            long now = Stopwatch.GetTimestamp();
            long deltaLong = now - LastTimestamp;
            float delta = deltaLong / (float)Stopwatch.Frequency;

            if (VolumeDuck != TargetVolumeDuck)
            {
                var diff = TargetVolumeDuck - VolumeDuck;
                var change = delta / AmbienceDuckTime;

                if (Math.Abs(diff) < change)
                {
                    VolumeDuck = TargetVolumeDuck;
                }
                else
                {
                    VolumeDuck += diff > 0 ? change : -change;
                }
            }

            LastTimestamp = now;
            List<byte> toKill = null;

            foreach (var soundPair in ActiveSounds)
            {
                var sound = soundPair.Value;
                if (sound.HasTransition())
                {
                    if (sound.TickVolume(delta) && sound.Volume == 0)
                    {
                        if (toKill == null)
                        {
                            toKill = [];
                        }

                        toKill.Add(soundPair.Key);
                    }
                }
            }

            if (toKill != null)
            {
                foreach (byte id in toKill)
                {
                    var cat = SoundByBitField[id];
                    ActiveSounds[id].Kill();
                    ActiveSounds.Remove(id);
                }
            }
        }

        public bool AmbienceActive(byte id)
        {
            return ActiveSounds.ContainsKey(id);
        }

        public byte GetAmbienceFromGUID(uint GUID)
        {
            for (byte i = 0; i < SoundByBitField.Count; i++)
            {
                if (SoundByBitField[i].GUID == GUID) return i;
            }
            return 0;
        }

        public VMCategorisedAmb? GetAmbienceFromName(string name)
        {
            for (byte i = 0; i < SoundByBitField.Count; i++)
            {
                if (SoundByBitField[i].Name == name) return SoundByBitField[i];
            }
            return null;
        }

        public void SetVolumeWithCameraInfo(WorldStateCameraInfo cameraInfo)
        {
            Volume = Math.Clamp((float)Math.Sqrt(15 / cameraInfo.GroundDistance), 0, 1);

            TargetVolumeDuck = cameraInfo.IsIndoors ? IndoorsVolumeDuck : 1;

            foreach (var sound in ActiveSounds.Values)
            {
                sound.SetPositionalVolume(Volume * VolumeDuck);
            }
        }

        public void Pause()
        {
            Paused = true;
            foreach (var sound in ActiveSounds.Values)
            {
                sound.Pause();
            }
        }

        public void Resume()
        {
            Paused = false;
            foreach (var sound in ActiveSounds.Values)
            {
                sound.Resume();
            }
        }

        public void SwitchAmbience(VMAmbientSoundType newAmbience, bool instant)
        {
            for (int i = 0; i < SoundByBitField.Count; i++)
            {
                bool oldBit = ((ulong)ActiveBits & (1ul << i)) != 0;
                bool newBit = ((ulong)newAmbience & (1ul << i)) != 0;

                if (oldBit != newBit)
                {
                    SetAmbience((byte)i, newBit, instant);
                }
            }
        }

        public void SetUserBits(ulong type)
        {
            UserBits = (VMAmbientSoundType)type;
        }

        public void SetUserAmbience(byte id, bool active)
        {
            if (id > SoundByBitField.Count) return;
            if (active)
            {
                var cat = SoundByBitField[id];
                var newActiveBits = ActiveBits;
                if (cat.Category == 4)
                {
                    // cancel other loops
                    newActiveBits &= ~AllLoops; // TODO keep auto bits?
                    UserBits &= ~AllLoops;

                    if (newActiveBits != ActiveBits)
                    {
                        SwitchAmbience(newActiveBits, true);
                    }
                }

                UserBits |= (VMAmbientSoundType)((ulong)1 << id);
            }
            else
            {
                UserBits &= (VMAmbientSoundType)~(((ulong)1 << id));
            }

            SetAmbience(id, active, true);
        }

        public void SetAmbience(byte id, bool active, bool instant = true)
        {
            if (ForceDisable || HITVM.DISABLE_SOUND) return;
            if (id > SoundByBitField.Count) return;
            instant |= !VM.UseWorld;
            if (active)
            {
                ActiveBits |= (VMAmbientSoundType)((ulong)1 << id);
                if (!ActiveSounds.TryGetValue(id, out AmbiencePlayer player))
                {
                    var cat = SoundByBitField[id];
                    var amb = AmbienceByGUID[cat.GUID];
                    if (VM.UseWorld)
                    {
                        player = new AmbiencePlayer(amb, instant ? Volume : 0);
                        ActiveSounds.Add(id, player);
                        if (!instant)
                        {
                            player.SetVolume(cat.Volume, AmbienceTransitionTime);
                        }
                    }
                }
                else if (VM.UseWorld)
                {
                    var cat = SoundByBitField[id];
                    if (instant)
                    {
                        player.SetVolume(cat.Volume);
                    }
                    else
                    {
                        player.SetVolume(cat.Volume, AmbienceTransitionTime);
                    }
                }
            }
            else
            {
                ActiveBits &= (VMAmbientSoundType)~(((ulong)1 << id));
                if (ActiveSounds.TryGetValue(id, out AmbiencePlayer player))
                {
                    if (instant)
                    {
                        var cat = SoundByBitField[id];
                        ActiveSounds[id].Kill();
                        ActiveSounds.Remove(id);
                    }
                    else
                    {
                        player.SetVolume(0, AmbienceTransitionTime);
                    }
                }
            }
        }

        public void BeginTransition()
        {
            ToTransition = this;
            UserCount++;
        }

        public void Kill()
        {
            if (--UserCount == 0)
            {
                foreach (var sound in ActiveSounds)
                {
                    sound.Value.Kill();
                }
                ActiveSounds.Clear();
            }
        }

    }

    public struct VMCategorisedAmb
    {
        public uint GUID;
        public byte Category;
        public string Name;
        public float Volume;

        public VMCategorisedAmb(uint guid, byte cat, string name, float volume)
        {
            GUID = guid;
            Category = cat;
            Name = name;
            Volume = volume;
        }

        public VMCategorisedAmb(uint guid, byte cat, string name) : this(guid, cat, name, cat == 4 ? 0.8f : 0.33f)
        {
        }
    }
}
