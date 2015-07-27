using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.HIT;
using System.Runtime.InteropServices;

namespace TSO.Simantics.engine
{
    public class VMAmbientSound
    {
        public static Dictionary<uint, Ambience> AmbienceByGUID = new Dictionary<uint, Ambience>() //may want to load this from ambience.ini in future...
        {
            {0x3dd887a6, new Ambience("SoundData\\Ambience\\Daybirds\\daybirds.fsc", false)},
            {0x3dd887aa, new Ambience("SoundData\\Ambience\\Explosions\\explosions.fsc", false)},
            {0x7dd887ad, new Ambience("SoundData\\Ambience\\FarmAnimals\\farmanimals.fsc", false)},
            {0x9dd887af, new Ambience("SoundData\\Ambience\\GunShots\\gunshots.fsc", false)},
            {0xddd887b3, new Ambience("SoundData\\Ambience\\Planes\\planes.fsc", false)},
            {0xfdd887b5, new Ambience("SoundData\\Ambience\\Thunder\\thunder.fsc", false)},
            {0x1e128187, new Ambience("SoundData\\Ambience\\Breeze\\Breeze.fsc", false)},
            {0xfe128189, new Ambience("SoundData\\Ambience\\Construction\\Construction.fsc", false)},
            {0x5e12818c, new Ambience("SoundData\\Ambience\\Dog\\Dog.fsc", false)},
            {0xbe12818d, new Ambience("SoundData\\Ambience\\DriveBy\\DriveBy.fsc", false)},
            {0xde12818f, new Ambience("SoundData\\Ambience\\HowlingWind\\HowlingWind.fsc", false)},
            {0x1e128190, new Ambience("SoundData\\Ambience\\Indust\\Indust.fsc", false)},
            {0x3e128192, new Ambience("SoundData\\Ambience\\Insect\\Insect.fsc", false)},
            {0xbe128196, new Ambience("SoundData\\Ambience\\Jungle\\Jungle.fsc", false)},
            {0xde128198, new Ambience("SoundData\\Ambience\\Office\\Office.fsc", false)},
            {0x3e12819a, new Ambience("SoundData\\Ambience\\Restaurant\\Restaurant.fsc", false)},
            {0xbe12819c, new Ambience("SoundData\\Ambience\\sciBleeps\\sciBleeps.fsc", false)},
            {0x1e1281ac, new Ambience("SoundData\\Ambience\\Siren\\Siren.fsc", false)},
            {0x1e1281ad, new Ambience("SoundData\\Ambience\\Wolf\\Wolf.fsc", false)},
            {0xbe19bb2d, new Ambience("SoundData\\Ambience\\SeaBirds\\SeaBirds.fsc", false)},
            {0xde19bb31, new Ambience("SoundData\\Ambience\\RainDrops\\RainDrops.fsc", false)},
            {0xbe1a033e, new Ambience("SoundData\\Ambience\\Magic\\Magic.fsc", false)},
            {0xa9b9652a, new Ambience("SoundData\\Ambience\\SmallMachines\\SmallMachines.fsc", false)},
            {0xa9b96536, new Ambience("SoundData\\Ambience\\Screams\\Screams.fsc", false)},
            {0xa9b96539, new Ambience("SoundData\\Ambience\\NightBirds\\NightBirds.fsc", false)},
            {0xa9b9653c, new Ambience("SoundData\\Ambience\\Gym\\Gym.fsc", false)},
            {0xa9b9653e, new Ambience("SoundData\\Ambience\\Ghost\\Ghost.fsc", false)},

            //Loops

            {0x9e0bc19a, new Ambience("SoundData\\Ambience\\Loops\\brook_lp.xa", true)},
            {0xfe0bc1a1, new Ambience("SoundData\\Ambience\\Loops\\crowd_lp.xa", true)},
            {0x1e0bc1a3, new Ambience("SoundData\\Ambience\\Loops\\heartbeat_lp.xa", true)},
            {0x5e0bc1a4, new Ambience("SoundData\\Ambience\\Loops\\indoor_lp.xa", true)},
            {0x5e0bc1a6, new Ambience("SoundData\\Ambience\\Loops\\insect_lp.xa", true)},
            {0xbe0bc1a9, new Ambience("SoundData\\Ambience\\Loops\\ocean_lp.xa", true)},
            {0x1e0bc1ab, new Ambience("SoundData\\Ambience\\Loops\\outdoor_lp.xa", true)},
            {0xde0bc1ad, new Ambience("SoundData\\Ambience\\Loops\\rain_lp.xa", true)},
            {0x3e0bc2af, new Ambience("SoundData\\Ambience\\Loops\\scifi_lp.xa", true)},
            {0x1e0bc2b2, new Ambience("SoundData\\Ambience\\Loops\\storm_lp.xa", true)},
            {0x3e0bc2b4, new Ambience("SoundData\\Ambience\\Loops\\traffic_lp.xa", true)},
            {0x1e0bc2b5, new Ambience("SoundData\\Ambience\\Loops\\wind_lp.xa", true)}
        };

        public static List<VMCategorisedAmb> SoundByBitField = new List<VMCategorisedAmb>() {
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

        public Dictionary<byte, AmbiencePlayer> ActiveSounds;

        /// <summary>
        /// Handles ambient sound in lots.
        /// </summary>
        /// 
        public VMAmbientSound()
        {
            ActiveSounds = new Dictionary<byte, AmbiencePlayer>();
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

        public void SetAmbience(byte id, bool active)
        {
            if (active)
            {
                if (!ActiveSounds.ContainsKey(id))
                {
                    var cat = SoundByBitField[id];
                    var amb = AmbienceByGUID[cat.GUID];
                    ActiveSounds.Add(id, new AmbiencePlayer(amb));
                }
            }
            else
            {
                if (ActiveSounds.ContainsKey(id))
                {
                    ActiveSounds[id].Kill();
                    ActiveSounds.Remove(id);
                }
            }
        }

        public void Kill()
        {
            foreach (var sound in ActiveSounds)
            {
                sound.Value.Kill();
            }
            ActiveSounds.Clear();
        }

    }

    public struct VMCategorisedAmb
    {
        public uint GUID;
        public byte Category;
        public string Name;

        public VMCategorisedAmb(uint guid, byte cat, string name)
        {
            GUID = guid;
            Category = cat;
            Name = name;
        }
    }
}
