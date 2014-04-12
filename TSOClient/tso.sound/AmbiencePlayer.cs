using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using SimsLib.XA;
using Un4seen.Bass;

namespace TSO.HIT
{
    public class AmbiencePlayer
    {
        private bool fscMode;
        private FSCPlayer fsc;
        private int Channel;
        private GCHandle LoopSound;

        public AmbiencePlayer(Ambience amb)
        {
            if (amb.Loop)
            {
                byte[] data = new XAFile(TSO.Content.Content.Get().GetPath(amb.Path)).DecompressedData;

                LoopSound = GCHandle.Alloc(data, GCHandleType.Pinned);

                IntPtr pointer = LoopSound.AddrOfPinnedObject();
                Channel = Bass.BASS_StreamCreateFile(pointer, 0, data.Length, BASSFlag.BASS_DEFAULT | BASSFlag.BASS_STREAM_AUTOFREE);
                //Bass.BASS_ChannelSetAttribute(Channel, BASSAttribute.BASS_ATTRIB_VOL, 0.33f);
                Bass.BASS_ChannelFlags(Channel, BASSFlag.BASS_MUSIC_LOOP, BASSFlag.BASS_MUSIC_LOOP);
                Bass.BASS_ChannelPlay(Channel, false);
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
                Bass.BASS_ChannelStop(Channel);
                LoopSound.Free();
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
