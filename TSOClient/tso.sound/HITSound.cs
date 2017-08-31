/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using FSO.HIT.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.HIT
{
    public abstract class HITSound
    {
        protected bool VolumeSet;
        protected float Volume = 1;
        protected float InstVolume = 1;
        protected float Pan;
        protected Microsoft.Xna.Framework.Audio.AudioEmitter Emitter3D;

        protected bool EverHadOwners; //if we never had owners, don't kill the thread. (ui sounds)
        public int LastMainOwner = -1;
        protected List<int> Owners;

        public bool Dead;
        public HITVM VM;
        public HITVolumeGroup VolGroup;

        public HITSound()
        {
            Owners = new List<int>();
        }

        public abstract bool Tick();

        public bool SetVolume(float volume, float pan, int ownerID)
        {
            bool ownerChange = false;
            if (VolumeSet)
            {
                if (volume > InstVolume)
                {
                    if (LastMainOwner != ownerID) { LastMainOwner = ownerID; ownerChange = true; }
                    InstVolume = volume;
                    RecalculateVolume();
                    Pan = pan;
                    return true;
                }
                return false;
            }
            else
            {
                if (LastMainOwner != ownerID) { LastMainOwner = ownerID; ownerChange = true; }
                InstVolume = volume;
                RecalculateVolume();
                Pan = pan;
                return true;
            }
        }

        public void Set3D(Microsoft.Xna.Framework.Vector3 Position)
        {
            Microsoft.Xna.Framework.Audio.SoundEffect.DistanceScale = 100f;
            if (Emitter3D == null) Emitter3D = new Microsoft.Xna.Framework.Audio.AudioEmitter();
            Emitter3D.Position = Position;
        }

        public void Apply3D(Microsoft.Xna.Framework.Audio.SoundEffectInstance inst)
        {
            Emitter3D.Forward = VM.Listener.Forward;
            inst.Volume = 1f;
            inst.Apply3D(VM.Listener, Emitter3D);
        }

        public void RecalculateVolume()
        {
            VolumeSet = true;
            Volume = InstVolume * GetVolFactor();
        }

        public float GetVolFactor()
        {
            return VM?.GetMasterVolume(VolGroup) ?? 1f;
        }

        public void AddOwner(int id)
        {
            EverHadOwners = true;
            Owners.Add(id);
        }

        public void RemoveOwner(int id)
        {
            Owners.Remove(id);
        }

        public bool AlreadyOwns(int id)
        {
            return Owners.Contains(id);
        }
    }
}
