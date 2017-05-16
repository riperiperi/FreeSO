/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using FSO.Files.XA;
using Microsoft.Xna.Framework.Audio;
using System.IO;
using FSO.HIT.Model;

namespace FSO.HIT
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
                byte[] data = new XAFile(FSO.Content.Content.Get().GetPath(amb.Path)).DecompressedData;
                var stream = new MemoryStream(data);
                sfx = SoundEffect.FromStream(stream);
                stream.Close();

                inst = sfx.CreateInstance();
                inst.IsLooped = true;
                inst.Volume = HITVM.Get().GetMasterVolume(HITVolumeGroup.AMBIENCE);
                inst.Play();
                HITVM.Get().AmbLoops.Add(inst);

                fscMode = false;
            }
            else
            {
                fsc = HITVM.Get().PlayFSC(FSO.Content.Content.Get().GetPath(amb.Path));
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
                HITVM.Get().AmbLoops.Remove(inst);
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
