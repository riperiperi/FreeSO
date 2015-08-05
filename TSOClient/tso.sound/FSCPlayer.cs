/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Files.HIT;
using System.Runtime.InteropServices;
using FSO.Files.XA;
using Microsoft.Xna.Framework.Audio;
using System.IO;

namespace FSO.HIT
{
    public class FSCPlayer
    {
        /// <summary>
        /// A Class to play FSC sequences. Bundled in with the HIT engine because it wouldn't really go anywhere else. :I
        /// </summary>
        /// 

        public int CurrentPosition;
        public short LoopCount = -1;
        public float TimeDiff;
        private FSC fsc;
        private string BaseDir;
        private float BeatLength;
        private float Volume = 1;
        private List<SoundEffectInstance> SoundEffects;

        private Dictionary<string, SoundEffect> SoundCache;

        public FSCPlayer(FSC fsc, string basedir)
        {
            this.fsc = fsc;
            this.BaseDir = basedir;
            SoundCache = new Dictionary<string, SoundEffect>();
            SoundEffects = new List<SoundEffectInstance>();

            BeatLength = 60.0f / fsc.Tempo;
        }

        public void SetManualTempo(int tempo)
        {
            BeatLength = 60.0f / fsc.Tempo;
        }

        public void SetVolume(float volume)
        {
            Volume = volume;
        }

        public void Tick(float time) {
            for (int i = 0; i < SoundEffects.Count; i++) //dispose and remove sound effect instances that are finished
            {
                if (SoundEffects[i].State != SoundState.Playing)
                {
                    SoundEffects[i].Dispose();
                    SoundEffects.RemoveAt(i--);
                }
            }

            TimeDiff += time;
            while (TimeDiff > BeatLength)
            {
                TimeDiff -= BeatLength;
                NextNote();
            }
        }

        private SoundEffect LoadSound(string filename)
        {
            if (SoundCache.ContainsKey(filename)) return SoundCache[filename];
            byte[] data = new XAFile(BaseDir+filename).DecompressedData;

            var stream = new MemoryStream(data);
            var sfx = SoundEffect.FromStream(stream);
            stream.Close();
            SoundCache.Add(filename, sfx);
            return sfx;
        }

        private void NextNote()
        {
            if (LoopCount == -1)
            {
                var note = fsc.Notes[CurrentPosition++];
                if (CurrentPosition >= fsc.Notes.Count) CurrentPosition = 0;
                if (note.Filename != "NONE")
                {
                    bool play;
                    if (note.Rand) play = (new Random().Next(16) < note.Prob);
                    else play = true;

                    float volume = (note.Volume / 1024.0f) * (fsc.MasterVolume / 1024.0f) * Volume;
                    var sound = LoadSound(note.Filename);

                    var instance = sound.CreateInstance();
                    instance.Volume = volume;
                    instance.Pan = (note.LRPan / 512.0f) - 1;
                    instance.Play();
                    SoundEffects.Add(instance);
                }
                LoopCount = (short)(note.Loop - 1);
            }
            else LoopCount--;
        }
    }
}
