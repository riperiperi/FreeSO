/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using FSO.Files.HIT;
using FSO.Files.XA;
using Microsoft.Xna.Framework.Audio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FSO.HIT.Events
{
    /// <summary>
    /// A special HITThread replacement to handle the kTurnOnTV event. Used by radio, TV, and (in the original game) the UI music.
    /// 
    /// TODO: special handler for mp3 mode, where we need to play through Bass.NET or other. (radio, music)
    /// </summary>
    public class HITTVOn : HITSound
    {
        // from radio.ini, should probably load from there in future
        private static Dictionary<string, string> StationPaths = new Dictionary<string, string>
        {
            { "KACT", "sounddata/tvstations/tv_action/" },
            { "KCOM", "sounddata/tvstations/tv_comedy_cartoon/" },
            { "KMYS", "sounddata/tvstations/tv_mystery/" },
            { "KROM", "sounddata/tvstations/tv_romance/" }
        };

        private string Station;
        private Dictionary<string, SoundEffect> SFXCache = new Dictionary<string, SoundEffect>();
        private List<string> Sounds = new List<string>();
        private SoundEffectInstance CurrentInstance;

        public override bool Tick()
        {
            if (EverHadOwners && Owners.Count == 0)
            {
                if (CurrentInstance != null)
                {
                    CurrentInstance.Stop();
                    CurrentInstance.Dispose();
                    foreach (var sound in SFXCache.Values)
                    {
                        sound.Dispose();
                    }
                }
                Dead = true;
                return false;
            }

            if (CurrentInstance != null)
            {
                CurrentInstance.Pan = Pan;
                CurrentInstance.Volume = Volume;
            }
            VolumeSet = false;

            if (CurrentInstance == null || CurrentInstance.State != SoundState.Playing)
            {
                PlayNext();
            }
            return true;
        }

        public HITTVOn(uint TrackID)
        {
            Station = new string(new char[] { (char)(TrackID & 0xFF), (char)((TrackID>>8) & 0xFF), (char)((TrackID>>16) & 0xFF), (char)((TrackID>>24) & 0xFF) });
            if (StationPaths.ContainsKey(Station)) LoadStation(StationPaths[Station]);
        }

        private void LoadStation(string path)
        {
            var statBase = Content.Content.Get().GetPath(path);
            var tvFiles = Directory.GetFiles(statBase, "*.xa", SearchOption.AllDirectories);

            var rand = new Random();
            foreach (var file in tvFiles)
            {
                Sounds.Insert(rand.Next(Sounds.Count+1), file);
            }
        }

        public void PlayNext()
        {
            if (CurrentInstance != null) CurrentInstance.Dispose();
            var sound = Sounds[0];
            Sounds.RemoveAt(0);
            Sounds.Insert(Sounds.Count + (new Random()).Next(1), sound); //put back at end, with a little bit of shuffle.

            if (!SFXCache.ContainsKey(sound))
                SFXCache[sound] = SoundEffect.FromStream(new XAFile(sound).DecompressedStream);
            var sfx = SFXCache[sound];

            var instance = sfx.CreateInstance();
            instance.Volume = Volume;
            instance.Pan = Pan;
            instance.Play();
            CurrentInstance = instance;
        }
    }
}
