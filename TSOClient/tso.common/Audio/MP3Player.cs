using Mp3Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Audio;
using System.IO;
using Microsoft.Xna.Framework.Media;
using System.Threading;
using System.Threading.Tasks;

namespace FSO.Common.Audio
{
    public class MP3Player : ISFXInstanceLike
    {
        private Mp3Stream Stream;
        public DynamicSoundEffectInstance Inst;
        private int LastChunkSize = 1; //don't die immediately..
        private Thread DecoderThread;

        private List<byte[]> NextBuffers = new List<byte[]>();
        private List<int> NextSizes = new List<int>();
        private int Requests;
        private AutoResetEvent DecodeNext;
        private AutoResetEvent BufferDone;
        private bool EndOfStream;
        private Thread MainThread; //keep track of this, terminate when it closes.

        private SoundState _State = SoundState.Stopped;
        private bool Disposed = false;

        private float _Volume = 1f;
        private float _Pan;

        private object ControlLock = new object();
        private string Path;

        public MP3Player(string path)
        {
            Path = path;
            // //let's get started...

            DecodeNext = new AutoResetEvent(true);
            BufferDone = new AutoResetEvent(false);
            MainThread = Thread.CurrentThread;

            Task.Run((Action)Start);
        }

        public void Start()
        {
            Stream = new Mp3Stream(Path);
            Stream.DecodeFrames(1);
            lock (ControlLock)
            {
                if (Disposed) return;
                Inst = new DynamicSoundEffectInstance(Stream.Frequency, AudioChannels.Stereo);
                Inst.IsLooped = false;
                Inst.BufferNeeded += SubmitBufferAsync;
                if (_State == SoundState.Playing) Inst.Play();
                else if (_State == SoundState.Paused)
                {
                    Inst.Play();
                    Inst.Pause();
                }
                Inst.Volume = _Volume;
                Inst.Pan = _Pan;
                Requests = 1;
            }

            //SubmitBuffer(null, null);
            //SubmitBuffer(null, null);

            DecoderThread = new Thread(() =>
            {
                try
                {
                    while (MainThread.IsAlive)
                    {
                        DecodeNext.WaitOne(128);
                        bool go;
                        lock (this) go = Requests > 0;
                        while (go)
                        {
                            var buf = new byte[524288];
                            var read = Stream.Read(buf, 0, buf.Length);
                            lock (this)
                            {
                                Requests--;
                                NextBuffers.Add(buf);
                                NextSizes.Add(read);
                                if (read == 0)
                                {
                                    EndOfStream = true;
                                    return;
                                }
                                BufferDone.Set();
                            }
                            lock (this) go = Requests > 0;
                        }
                    }
                }
                catch (Exception e) { }
            });
            DecoderThread.Start();
        }

        public void Play()
        {
            lock (ControlLock)
            {
                _State = SoundState.Playing;
                Inst?.Play();
            }
        }

        public void Stop()
        {
            lock (ControlLock)
            {
                _State = SoundState.Stopped;
                Inst?.Stop();
            }
        }

        public void Pause()
        {
            lock (ControlLock)
            {
                _State = SoundState.Paused;
                Inst?.Pause();
            }
        }

        public void Resume()
        {
            lock (ControlLock)
            {
                _State = SoundState.Playing;
                Inst?.Resume();
            }
        }

        public void Dispose()
        {
            lock (ControlLock)
            {
                Disposed = true;
                Inst?.Dispose();
                Stream?.Dispose();
                DecoderThread?.Abort();
                EndOfStream = true;
            }
        }

        public bool IsEnded()
        {
            return EndOfStream && Inst.PendingBufferCount == 0;
        }

        public float Volume
        {
            get
            {
                lock (ControlLock)
                {
                    if (Inst != null) return Inst.Volume;
                    else return _Volume;
                }
            }
            set
            {
                lock (ControlLock)
                {
                    _Volume = value;
                    if (Inst != null) Inst.Volume = value;
                }
            }
        }

        public float Pan
        {
            get
            {
                lock (ControlLock)
                {
                    if (Inst != null) return Inst.Pan;
                    else return _Pan;
                }
            }
            set
            {
                lock (ControlLock)
                {
                    _Pan = value;
                    if (Inst != null) Inst.Pan = value;
                }
            }
        }

        public SoundState State
        {
            get
            {
                lock (ControlLock)
                {
                    if (Inst != null) return Inst.State;
                    else return _State;
                }
            }
        }

        public bool IsLooped { get; set; }

        private void SubmitBuffer(object sender, EventArgs e)
        {
            byte[] buffer = new byte[524288];
            lock (this)
            {
                var read = Stream.Read(buffer, 0, buffer.Length);
                LastChunkSize = read;
                if (read == 0)
                {
                    return;
                }
                Inst.SubmitBuffer(buffer, 0, read);
            }
        }

        private void SubmitBufferAsync(object sender, EventArgs e)
        {
            while (true)
            {
                if (EndOfStream) return;
                //BufferDone.WaitOne(128);
                lock (this)
                {
                    if (NextBuffers.Count > 0)
                    {
                        if (NextSizes[0] > 0) Inst.SubmitBuffer(NextBuffers[0], 0, NextSizes[0]);
                        NextBuffers.RemoveAt(0);
                        NextSizes.RemoveAt(0);
                        Requests++;
                        DecodeNext.Set();
                        return;
                    }

                    if (EndOfStream) return;
                }
            }
        }
    }

    public interface ISFXInstanceLike
    {
        float Volume { get; set; }
        float Pan { get; set; }
        SoundState State { get; }
        bool IsLooped { get; set; }
        void Play();
        void Stop();
        void Pause();
        void Resume();
        void Dispose();
    }
}
