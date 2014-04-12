using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Files.HIT;
using System.Runtime.InteropServices;
using SimsLib.XA;
using Un4seen.Bass;

namespace TSO.HIT
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

        private Dictionary<string, GCHandle> SoundCache;

        public FSCPlayer(FSC fsc, string basedir)
        {
            this.fsc = fsc;
            this.BaseDir = basedir;
            SoundCache = new Dictionary<string, GCHandle>();

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
            TimeDiff += time;
            while (TimeDiff > BeatLength)
            {
                TimeDiff -= BeatLength;
                NextNote();
            }
        }

        private GCHandle LoadSound(string filename)
        {
            if (SoundCache.ContainsKey(filename)) return SoundCache[filename];
            byte[] data = new XAFile(BaseDir+filename).DecompressedData;

            GCHandle pinnedArray = GCHandle.Alloc(data, GCHandleType.Pinned);
            SoundCache.Add(filename, pinnedArray);
            return pinnedArray;
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

                    IntPtr pointer = sound.AddrOfPinnedObject();
                    int channel = Bass.BASS_StreamCreateFile(pointer, 0, ((byte[])sound.Target).Length, BASSFlag.BASS_DEFAULT | BASSFlag.BASS_STREAM_AUTOFREE);
                    Bass.BASS_ChannelSetAttribute(channel, BASSAttribute.BASS_ATTRIB_VOL, volume);
                    Bass.BASS_ChannelSetAttribute(channel, BASSAttribute.BASS_ATTRIB_PAN, (note.LRPan / 512.0f) - 1);
                    Bass.BASS_ChannelPlay(channel, false);
                }
                LoopCount = (short)(note.Loop - 1);
            }
            else LoopCount--;
        }
    }
}
