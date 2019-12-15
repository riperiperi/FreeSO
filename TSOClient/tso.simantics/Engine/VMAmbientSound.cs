/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.HIT;
using System.Runtime.InteropServices;

namespace FSO.SimAntics.Engine
{
    public class VMAmbientSound
    {
        public static bool ForceDisable;
        public static Dictionary<uint, Ambience> AmbienceByGUID = new Dictionary<uint, Ambience>() //may want to load this from ambience.ini in future...
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
        public ulong ActiveBits;
        public VMCategorisedAmb? ActiveLoop;

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

        public VMCategorisedAmb? GetAmbienceFromName(string name)
        {
            for (byte i = 0; i < SoundByBitField.Count; i++)
            {
                if (SoundByBitField[i].Name == name) return SoundByBitField[i];
            }
            return null;
        }

        public void SetAmbience(byte id, bool active)
        {
            if (ForceDisable || HITVM.DISABLE_SOUND) return;
            if (id > SoundByBitField.Count) return;
            if (active)
            {
                ActiveBits |= ((ulong)1 << id);
                if (!ActiveSounds.ContainsKey(id))
                {
                    var cat = SoundByBitField[id];
                    var amb = AmbienceByGUID[cat.GUID];
                    if (cat.Category == 4 && ActiveLoop != null) SetAmbience(GetAmbienceFromGUID(ActiveLoop.Value.GUID), false); //cancel previous loop
                    if (VM.UseWorld) ActiveSounds.Add(id, new AmbiencePlayer(amb));
                    if (cat.Category == 4) ActiveLoop = cat;
                }
            }
            else
            {
                ActiveBits &= ~(((ulong)1 << id));
                if (ActiveSounds.ContainsKey(id))
                {
                    var cat = SoundByBitField[id];
                    ActiveSounds[id].Kill();
                    ActiveSounds.Remove(id);
                    if (cat.Category == 4) ActiveLoop = null;
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
