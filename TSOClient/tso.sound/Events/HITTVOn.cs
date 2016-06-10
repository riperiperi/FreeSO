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
        // from radio.ini, should probably load from there in future
        private static Dictionary<string, string> StationPaths = new Dictionary<string, string>
        {
            {"KBEA", "Music/Stations/Beach/"},
            {"KCLA", "Music/Stations/Classica/"},
            {"KCOU", "Music/Stations/Country/"},
            {"KCDA", "Music/Stations/CountryD/"},
            {"KDIS", "Music/Stations/Disco/"},
            {"KEZE", "Music/Stations/EZ/"},
            {"KEZX", "Music/Stations/EZX/"},
            {"KLAT", "Music/Stations/Latin/"},
            {"KRAP", "Music/Stations/Rap/"},
            {"KRAV", "Music/Stations/Rave/"},
            {"KROC", "Music/Stations/Rock/"},
// These ones aren't radio stations - they're UI music
            {"KMAP", "Music/Modes/Map/"},
            {"KSEL", "Music/Modes/Select/"},
            {"KCRE", "Music/Modes/Create/"},
//tv
            { "KACT", "sounddata/tvstations/tv_action/" },
            { "KCOM", "sounddata/tvstations/tv_comedy_cartoon/" },
            { "KMYS", "sounddata/tvstations/tv_mystery/" },
            { "KROM", "sounddata/tvstations/tv_romance/" },
// More music
            {"KHOR", "Music/Stations/Horror/"},
            {"KOLD", "Music/Stations/OldWorld/"},
            {"KSCI", "Music/Stations/SciFi/"},
        };

        private static Dictionary<int, string> MusicModes = new Dictionary<int, string>
        {
            { 11, "KSEL" },
            { 12, "KCRE" },
            { 13, "KMAP" },
            { 9, "" }
        };

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
                Instance.Pan = Pan;
                Instance.Volume = Volume;
            }

            if (FadeOut > 0)
            {
                FadeOut--;
                if (Instance != null) Instance.Volume = Math.Max(0f, ((FadeOut - 60) / 120f));
                if (FadeOut == 0)
                {
                    Kill();
                    return false;
                }
            }

            VolumeSet = false;

            if (Instance == null || (IsMusic && MusicInstance.IsEnded()) || Instance.State != SoundState.Playing)
            {
                if (PlayNext()) return true;
                else {
                    Dead = true;
                    return false;
                }
            }
            return true;
        }

        public HITTVOn(uint TrackID)
        {
            Station = new string(new char[] { (char)(TrackID & 0xFF), (char)((TrackID>>8) & 0xFF), (char)((TrackID>>16) & 0xFF), (char)((TrackID>>24) & 0xFF) });
            if (StationPaths.ContainsKey(Station)) LoadStation(StationPaths[Station].ToLowerInvariant());
        }

        public HITTVOn(uint id, bool IDMode)
        {
            Station = "";

            if (id == 5)
            {
                //loadloop. load direct sound...
                var sfx = Content.Content.Get().Audio.GetSFX(0x00004f85);
                SFXCache.Add("loadloop", sfx);
                Instance = sfx.CreateInstance();
                Instance.IsLooped = true;
                Instance.Play();
            }
            else
            {
                MusicModes.TryGetValue((int)id, out Station);
                if (StationPaths.ContainsKey(Station)) LoadStation(StationPaths[Station].ToLowerInvariant());
            }
        }

        public void Fade()
        {
            if (FadeOut == -1) FadeOut = 60*3;
        }

        private void LoadStation(string path)
        {
            var statBase = Content.Content.Get().GetPath(path);
            var files = Directory.GetFiles(statBase, "*.xa", SearchOption.AllDirectories);

            if (files.Length > 0)
            {
                //tv.
            }
            else
            {
                //mp3 music
                files = Directory.GetFiles(statBase, "*.mp3", SearchOption.AllDirectories);
                IsMusic = true;
            }
            var rand = new Random();
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
            Instance.Volume = Volume;
            Instance.Pan = Pan;
            Instance.Play();
            return true;
        }
    }
}
