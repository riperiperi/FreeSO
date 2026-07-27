using FSO.Files.HIT;
using FSO.Files.XA;
using Microsoft.Xna.Framework.Audio;

namespace FSO.HIT
{
    public class FSCPlayer : IDisposable
    {
        private struct FSCNoteInstance : IDisposable
        {
            public SoundState State => Instance.State;
            public readonly SoundEffectInstance Instance;
            public readonly float Volume;

            public FSCNoteInstance(SoundEffectInstance instance, float volume)
            {
                Instance = instance;
                Volume = volume;
            }

            public void Pause()
            {
                Instance.Pause();
            }

            public void Resume()
            {
                Instance.Resume();
            }

            public void Dispose()
            {
                Instance.Stop();
                Instance.Dispose();
            }
        }

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
        private List<FSCNoteInstance> SoundEffects;
        private bool Paused;
        private int? LoopingNote;
        private bool DisposeCache;

        private Dictionary<string, SoundEffect> SoundCache;

        public FSCPlayer(FSC fsc, string basedir)
        {
            this.fsc = fsc;
            this.BaseDir = basedir;
            SoundCache = new Dictionary<string, SoundEffect>();
            SoundEffects = new List<FSCNoteInstance>();

            BeatLength = 60.0f / fsc.Tempo;
            RestartFSC();
        }

        public void SetManualTempo(int tempo)
        {
            BeatLength = 60.0f / fsc.Tempo;
        }

        public void SetVolume(float volume)
        {
            Volume = volume;
            RecalculateVolume();
        }

        public void RecalculateVolume()
        {
            foreach (var inst in SoundEffects)
            {
                inst.Instance.Volume = GetFinalVolume(inst.Volume);
            }
        }

        public void Pause()
        {
            Paused = true;

            foreach (var inst in SoundEffects)
            {
                inst.Pause();
            }
        }

        public void Resume()
        {
            Paused = false;

            foreach (var inst in SoundEffects)
            {
                inst.Resume();
            }
        }

        public void SetLoopingNote(float note)
        {
            LoopingNote = Math.Clamp((int)Math.Floor(note * fsc.NoteColumns.Length), 0, fsc.NoteColumns.Length);
        }

        public void Tick(float time)
        {
            if (Paused)
            {
                return;
            }

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
            if (SoundCache.TryGetValue(filename, out var cached)) return cached;
            try
            {
                var content = Content.Content.Get();
                SoundEffect sfx;
                if (content.TS1)
                {
                    sfx = content.Audio.GetSFX(new Patch() { Filename = Path.GetFileName(filename) });
                }
                else
                {
                    DisposeCache = true;
                    byte[] data = new XAFile(BaseDir + filename).DecompressedData;
                    var stream = new MemoryStream(data);
                    sfx = SoundEffect.FromStream(stream);
                    stream.Close();
                }

                SoundCache.Add(filename, sfx);
                return sfx;
            }
            catch (Exception)
            {
                SoundCache[filename] = null;
                return null;
            }
        }

        private void RestartFSC()
        {
            CurrentPosition = 0;
            LoopCount = -1;
        }

        private float GetFinalVolume(float volume)
        {
            return Math.Min(1, volume * Volume * HITVM.Get().GetMasterVolume(Model.HITVolumeGroup.AMBIENCE));
        }

        private void NextNote()
        {
            if (LoopCount == -1)
            {
                if (LoopingNote != null)
                {
                    CurrentPosition = LoopingNote.Value;
                }

                var noteColumn = fsc.NoteColumns[CurrentPosition++];

                if (CurrentPosition >= fsc.NoteColumns.Length)
                {
                    // Loops back to the start.
                    CurrentPosition = 0;
                }

                LoopCount = (short)(Math.Max(-1, noteColumn.Max(x => x.Loop) - 2));

                // Evaluate all of the notes, and see which we can play.

                int y = 0;
                foreach (var note in noteColumn)
                {
                    if (note.Filename != null && note.Filename != "NONE")
                    {
                        bool play;
                        if (note.Prob > 0) play = (Random.Shared.Next(100) < note.Prob);
                        else play = true;

                        bool exceedsMax = SoundEffects.Count >= fsc.Max;

                        if (play && !exceedsMax)
                        {
                            float noteVolume = (note.Volume / 1024.0f);
                            if (note.RandomVolume)
                            {
                                // Maybe this should allow for volumes closer to 0, but that didn't feel right.
                                noteVolume *= Random.Shared.NextSingle() * 0.66f + 0.33f;
                            }
                            float volume = noteVolume * (fsc.MasterVolume / 1024.0f);
                            var sound = LoadSound(note.Filename);

                            if (sound != null)
                            {
                                var instance = sound.CreateInstance();
                                instance.Volume = GetFinalVolume(volume);

                                float notePan = note.RandomPan ? (Random.Shared.Next(1000) / 500f - 1) : (note.LRPan / 512.0f - 1);
                                float pitchRange = note.pitchH - note.pitchL;
                                float pitch = (pitchRange != 0) ? Random.Shared.NextSingle() * pitchRange + note.pitchL : 0f;
                                instance.Pitch = pitch / 12f;
                                instance.Pan = notePan;
                                instance.Play();

                                SoundEffects.Add(new FSCNoteInstance(instance, volume));
                            }
                        }
                    }

                    y++;
                }
            }
            else LoopCount--;
        }

        public void Dispose()
        {
            foreach (var sound in SoundEffects)
            {
                sound.Dispose();
            }

            if (DisposeCache)
            {
                foreach (var sound in SoundCache.Values)
                {
                    sound?.Dispose();
                }
            }
        }
    }
}
