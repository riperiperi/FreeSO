using Mp3Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Audio;
using System.IO;
using Microsoft.Xna.Framework.Media;
using System.Threading;

namespace FSO.Common.Audio
{
    public class MP3Player
    {
        private Mp3Stream Stream;
        public DynamicSoundEffectInstance Inst;
        private int LastChunkSize = 1; //don't die immediately..
        private Thread DecoderThread;

        private List<byte[]> NextBuffers;
        private List<int> NextSizes;
        private int Requests;
        private AutoResetEvent DecodeNext;
        private AutoResetEvent BufferDone;
        private bool EndOfStream;

        public MP3Player(string path)
        {
            Stream = new Mp3Stream(path);
            Stream.DecodeFrames(1); //let's get started...

            DecodeNext = new AutoResetEvent(true);
            BufferDone = new AutoResetEvent(false);

            Inst = new DynamicSoundEffectInstance(Stream.Frequency, AudioChannels.Stereo);
            Inst.IsLooped = false;
            Inst.BufferNeeded += SubmitBufferAsync;
            SubmitBuffer(null, null);
            SubmitBuffer(null, null);

            NextBuffers = new List<byte[]>();
            NextSizes = new List<int>();
            Requests = 1;
            DecoderThread = new Thread(() =>
            {
                try
                {
                    while (true)
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
            Inst.Play();
        }

        public void Stop()
        {
            Inst.Stop();
        }

        public void Dispose()
        {
            lock (this)
            {
                Inst.Dispose();
                Stream.Dispose();
                DecoderThread.Abort();
                EndOfStream = true;
            }
        }
        
        public bool IsEnded()
        {
            return EndOfStream && Inst.PendingBufferCount == 0;
        }

        public void SetVolume(float volume)
        {
            Inst.Volume = volume;
        }

        public void SetPan(float pan)
        {
            Inst.Pan = pan;
        }

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
                BufferDone.WaitOne(128);
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
}
