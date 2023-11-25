using Mp3Sharp;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;
using System.Threading;
using System.Threading.Tasks;

namespace FSO.Common.Audio
{
    public class MP3Player : ISFXInstanceLike
    {
        public static bool NewMode = true;

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
        private bool Active = true;
        private Thread MainThread; //keep track of this, terminate when it closes.

        private SoundState _State = SoundState.Stopped;
        private bool Disposed = false;

        private float _Volume = 1f;
        private float _Pan;

        private object ControlLock = new object();
        private string Path;
        public int SendExtra = 2;

        private static byte[] Blank = new byte[65536];

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
            var freq = Stream.Frequency;
            lock (ControlLock)
            {
                if (Disposed) return;
                Inst = new DynamicSoundEffectInstance(freq, AudioChannels.Stereo);
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
                Requests = 2;
            }

            //SubmitBuffer(null, null);
            //SubmitBuffer(null, null);

            DecoderThread = new Thread(() =>
            {
                try
                {
                    while (Active && MainThread.IsAlive)
                    {
                        DecodeNext.WaitOne(128);
                        bool go;
                        lock (this) go = Requests > 0;
                        while (go)
                        {
                            var buf = new byte[262144];// 524288];
                            var read = Stream.Read(buf, 0, buf.Length);
                            lock (this)
                            {
                                Requests--;
                                NextBuffers.Add(buf);
                                NextSizes.Add(read);
                                if (read == 0)
                                {
                                    EndOfStream = true;
                                    BufferDone.Set();
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
            DecodeNext.Set();
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

                Active = false;
                DecodeNext.Set(); //end the mp3 thread immediately

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
                var gotData = false;
                lock (this)
                {
                    if (NextBuffers.Count > 0)
                    {
                        if (NextSizes[0] > 0) Inst.SubmitBuffer(NextBuffers[0], 0, NextSizes[0]);
                        gotData = true;
                        NextBuffers.RemoveAt(0);
                        NextSizes.RemoveAt(0);
                        Requests++;
                        DecodeNext.Set();
                        if (SendExtra > 0)
                        {
                            SendExtra--;
                            continue;
                        }
                        return;
                    }

                    if (EndOfStream) return;
                }
                if (!gotData)
                {
                    Inst.SubmitBuffer(Blank, 0, Blank.Length);
                    Requests++;
                    DecodeNext.Set();
                    return;
                    //if (NewMode) BufferDone.WaitOne(128);
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
