/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using FSO.Common.Audio;
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
    /// Also supports streaming MP3 audio.
    /// </summary>
    public class HITTVOn : HITSound
    {
        private string Station;
        private Dictionary<string, SoundEffect> SFXCache = new Dictionary<string, SoundEffect>();
        private List<string> Sounds = new List<string>();

        private SoundEffectInstance Instance;
        private MP3Player MusicInstance;
        private bool IsMusic;

        private int FadeOut = -1;

        public void Kill()
        {
            if (Instance != null)
            {
                KillLast();
                foreach (var sound in SFXCache.Values)
                {
                    sound.Dispose();
                }
            }
            Dead = true;
        }

        public override bool Tick()
        {
            if (Dead) return false;
            if (EverHadOwners && Owners.Count == 0)
            {
                Kill();
                return false;
            }

            if (Instance != null)
            {
                if (!IsMusic && Pan != Instance.Pan) Instance.Pan = Pan;
                Instance.Volume = InstVolume * GetVolFactor();
            }

            if (FadeOut > 0)
            {
                FadeOut--;
                if (Instance != null) Instance.Volume = Math.Max(0f, ((FadeOut - 60) / 120f)) * GetVolFactor();
                if (FadeOut == 0)
                {
                    Kill();
                    return false;
                }
            }

            VolumeSet = false;

            if (Instance == null || (IsMusic && MusicInstance.IsEnded()) || Instance.State == SoundState.Stopped)
            {
                if (PlayNext()) return true;
                else {
                    Dead = true;
                    return false;
                }
            }
            return true;
        }

        public HITTVOn(uint TrackID, HITVM vm)
        {
            this.VM = vm;
            Station = new string(new char[] { (char)(TrackID & 0xFF), (char)((TrackID>>8) & 0xFF), (char)((TrackID>>16) & 0xFF), (char)((TrackID>>24) & 0xFF) });
            var paths = Content.Content.Get().Audio.StationPaths;
            if (paths.ContainsKey(Station)) LoadStation(CleanPath(paths[Station]));
        }

        public HITTVOn(uint id, HITVM vm, bool IDMode)
        {
            this.VM = vm;
            Station = "";

            if (id == 5)
            {
                //loadloop. load direct sound...
                var sfx = Content.Content.Get().Audio.GetSFX(new Patch(0x00004f85));
                if (sfx == null) return;
                SFXCache.Add("loadloop", sfx);
                Instance = sfx.CreateInstance();
                Instance.Volume = GetVolFactor();
                Instance.IsLooped = true;
                Instance.Play();
            }
            else
            {
                var aud = Content.Content.Get().Audio;
                if (aud.MusicModes.TryGetValue((int)id, out Station))
                {
                    if (aud.StationPaths.ContainsKey(Station)) LoadStation(CleanPath(aud.StationPaths[Station]));
                }
            }
        }

        private string CleanPath(string path)
        {
            return (Content.Content.Get().TS1) ? path : path.ToLowerInvariant();
        }

        public void Fade()
        {
            if (FadeOut == -1) FadeOut = 60*3;
        }

        private void LoadStation(string path)
        {
            var content = Content.Content.Get();
            var statBase = (content.TS1)?Path.Combine(content.TS1BasePath, path):content.GetPath(path);
            var files = Directory.GetFiles(statBase, "*.xa", SearchOption.AllDirectories);
            var rand = new Random();

            if (files.Length > 0)
            {
                //tv.
                //include commercials from previous directory

                var bs = Path.GetDirectoryName(statBase);

                var index = bs.LastIndexOf('/');
                if (index == -1) index = bs.LastIndexOf('\\');

                var files2 = Directory.GetFiles(bs.Substring(0, index), "*.xa", SearchOption.TopDirectoryOnly);
                foreach (var file in files2)
                {
                    Sounds.Insert(rand.Next(Sounds.Count + 1), file);
                }
            }
            else
            {
                //mp3 music
                files = Directory.GetFiles(statBase, "*.mp3", SearchOption.AllDirectories);
                IsMusic = true;
            }

            foreach (var file in files)
            {
                Sounds.Insert(rand.Next(Sounds.Count + 1), file);
            }
        }

        private void KillLast()
        {
            if (Instance == null) return;
            if (IsMusic)
            {
                MusicInstance.Stop();
                MusicInstance.Dispose();
            } else
            {
                Instance.Stop();
                Instance.Dispose();
            }
            Instance = null;
            MusicInstance = null;
        }

        public bool PlayNext()
        {
            if (Sounds.Count == 0) return false;
            KillLast();
            var sound = Sounds[0];
            Sounds.RemoveAt(0);
            Sounds.Insert(Sounds.Count + (new Random()).Next(1), sound); //put back at end, with a little bit of shuffle.

            if (IsMusic)
            {
                MusicInstance = new MP3Player(sound);
                Instance = MusicInstance.Inst;
            }
            else
            {
                if (!SFXCache.ContainsKey(sound))
                    SFXCache[sound] = SoundEffect.FromStream(new XAFile(sound).DecompressedStream);
                var sfx = SFXCache[sound];

                Instance = sfx.CreateInstance();
            }
            Instance.Volume = InstVolume * GetVolFactor();
            if (!IsMusic && Pan != 0) Instance.Pan = Pan;
            Instance.Play();
            return true;
        }

        public override void Pause()
        {
            Instance.Pause();
        }

        public override void Resume()
        {
            Instance.Resume();
        }

        public override void Dispose()
        {
        }
    }
}
