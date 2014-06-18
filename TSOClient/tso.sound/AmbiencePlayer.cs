using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using SimsLib.XA;
using Microsoft.Xna.Framework.Audio;
using System.IO;

namespace TSO.HIT
{
    public class AmbiencePlayer
    {
        private bool fscMode;
        private FSCPlayer fsc;
        private SoundEffect sfx;
        private SoundEffectInstance inst;

        public AmbiencePlayer(Ambience amb)
        {
            if (amb.Loop)
            {
                byte[] data = new XAFile(TSO.Content.Content.Get().GetPath(amb.Path)).DecompressedData;
                var stream = new MemoryStream(data);
                sfx = SoundEffect.FromStream(stream);
                stream.Close();

                inst = sfx.CreateInstance();
                inst.IsLooped = true;
                inst.Play();
                fscMode = false;
            }
            else
            {
                fsc = HITVM.Get().PlayFSC(TSO.Content.Content.Get().GetPath(amb.Path));
                fsc.SetVolume(0.33f); //may need tweaking
                fscMode = true;
            }
        }

        public void Kill()
        {
            if (fscMode) HITVM.Get().StopFSC(fsc);
            else
            {
                inst.Stop();
                inst.Dispose();
                sfx.Dispose();
            }
        }
    }

    public struct Ambience
    {
        public string Path;
        public bool Loop; //certain ambiences are simple xa loops instead of fscs.

        public Ambience(string path, bool loop)
        {
            Path = path;
            Loop = loop;
        }
    }
}
