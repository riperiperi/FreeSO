/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using FSO.Files.HIT;
using Microsoft.Xna.Framework.Audio;
using FSO.Content.Interfaces;
using FSO.Content.Model;

namespace FSO.HIT
{
    public class HITThread : HITSound
    {
        public uint PC; //program counter
        public HITFile Src;
        public HITResourceGroup ResGroup;
        private Hitlist Hitlist;
        private int[] Registers; //includes args, vars, whatever "h" is up to 0xf
        private int[] LocalVar; //the sims online set, 0x10 "argstyle" up to 0x45 orientz. are half of these even used? no. but even in the test files? no
        public int[] ObjectVar; //IsInsideViewFrustrum 0x271a to Neatness 0x2736. Set by object on thread invocation.

        private Track ActiveTrack;
        public int LoopPointer = -1;
        public int WaitRemain = -1;

        public bool SimpleMode; //certain sounds play with no HIT.
        public bool Loop;
        private bool PlaySimple;

        private Patch Patch; //sound id
        public bool HasSetLoop;

        private List<HITNoteEntry> Notes;
        private Dictionary<SoundEffectInstance, HITNoteEntry> NotesByChannel;
        public int LastNote
        {
            get { return Notes.Count - 1; }
        }

        public HITDuckingPriorities DuckPriority
        {
            get
            {
                if (ActiveTrack != null)
                    return ActiveTrack.DuckingPriority;
                else
                    return HITDuckingPriorities.duckpri_normal;
            }
        }

        public bool ZeroFlag; //flags set by instructions
        public bool SignFlag;

        public Stack<int> Stack;

        private IAudioProvider audContent;

        public override bool Tick() //true if continue, false if kill
        {
            if (EverHadOwners && Owners.Count == 0)
            {
                KillVocals();
                Dead = true;
                return false;
            }

            if (VolumeSet)
            {
                for (int i = 0; i < Notes.Count; i++)
                {
                    var inst = Notes[i].instance;
                    inst.Pan = Pan;
                    inst.Volume = Math.Min(1.0f, Volume);
                }
            }

            VolumeSet = false;

            if (SimpleMode)
            {
                if (PlaySimple)
                {
                    NoteOn();
                    PlaySimple = false;
                }
                if (NoteActive(LastNote)) return true;
                else
                {
                    Dead = true;
                    return false;
                }
            }
            else
            {
                try {
                    while (true)
                    {
                        var opcode = Src.Data[PC++];
                        if (opcode >= HITInterpreter.Instructions.Length) opcode = 0;
                        var result = HITInterpreter.Instructions[opcode](this);
                        if (result == HITResult.HALT) return true;
                        else if (result == HITResult.KILL)
                        {
                            Dead = true;
                            return false;
                        }
                    }
                } catch (Exception)
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Kills all playing sounds.
        /// </summary>
        public void KillVocals()
        {
            for (int i = 0; i < Notes.Count; i++)
            {
                if (NoteActive(i))
                {
                    Notes[i].instance.Stop();
                    Notes[i].instance.Dispose();
                }
            }
        }

        public HITThread(HITResourceGroup Src, HITVM VM)
        {
            this.ResGroup = Src;
            this.Src = Src.hit;
            this.VM = VM;
            Registers = new int[16];
            Registers[1] = 12; //gender (offset into object var table)
            LocalVar = new int[54];
            ObjectVar = new int[29];

            Notes = new List<HITNoteEntry>();
            NotesByChannel = new Dictionary<SoundEffectInstance, HITNoteEntry>();
            Owners = new List<int>();

            Stack = new Stack<int>();
            audContent = Content.Content.Get().Audio;
        }

        public HITThread(uint TrackID, HITVM VM, HITResourceGroup Src)
        {
            this.VM = VM;
            ResGroup = Src;
            Owners = new List<int>();
            Notes = new List<HITNoteEntry>();
            NotesByChannel = new Dictionary<SoundEffectInstance, HITNoteEntry>();

            audContent = Content.Content.Get().Audio;
            SetTrack(TrackID);

            SimpleMode = true;
            PlaySimple = true; //play next frame, so we have time to set volumes.
        }

        public void LoadHitlist(uint id)
        {
            Hitlist = audContent.GetHitlist(id, ResGroup);
        }

        public uint HitlistChoose() //returns a random id from the hitlist
        {
            Random rand = new Random();
            if (Hitlist != null) return Hitlist.IDs[rand.Next(Hitlist.IDs.Count)];
            else return 0;
        }

        public byte ReadByte()
        {
            return Src.Data[PC++];
        }

        public uint ReadUInt32()
        {
            uint result = 0;
            result |= ReadByte();
            result |= ((uint)ReadByte() << 8);
            result |= ((uint)ReadByte() << 16);
            result |= ((uint)ReadByte() << 24);
            return result;
        }

        public int ReadInt32()
        {
            return (int)ReadUInt32();
        }

        public void SetTrack(uint value)
        {
            SetTrack(value, 0);
        }

        public void SetTrack(uint value, uint fallback)
        {
            ActiveTrack = audContent.GetTrack(value, fallback, ResGroup);
            if (ActiveTrack != null)
            {
                if (ActiveTrack.HitlistID != 0)
                {
                    LoadHitlist(ActiveTrack.HitlistID);
                    if (ActiveTrack.SoundID == 0) ActiveTrack.SoundID = HitlistChoose();
                }
                Patch = audContent.GetPatch(ActiveTrack.SoundID, ResGroup);
            }
        }

        /// <summary>
        /// Loads a track from the current HitList.
        /// </summary>
        /// <param name="value">ID of track to load.</param>
        public uint LoadTrack(int value)
        {
            SetTrack(Hitlist.IDs[value]);
            return Hitlist.IDs[value];
        }

        /// <summary>
        /// Plays the current patch.
        /// </summary>
        /// <returns>-1 if unsuccessful, or the id of the note played.</returns>
        public int NoteOn()
        {
            var sound = audContent.GetSFX(Patch);

            if (sound != null)
            {
                switch (sound.Name)
                {
                    case "FX":
                        VolGroup = Model.HITVolumeGroup.FX; break;
                    case "MUSIC":
                        VolGroup = Model.HITVolumeGroup.MUSIC; break;
                    case "VOX":
                        VolGroup = Model.HITVolumeGroup.VOX; break;
                }
                RecalculateVolume();   

                var instance = sound.CreateInstance();
                instance.Volume = Volume;
                instance.Pan = Pan;
                instance.Play();

                var entry = new HITNoteEntry(instance, Patch);
                Notes.Add(entry);
                NotesByChannel.Add(instance, entry);
                return Notes.Count - 1;
            }
            else
            {
                Debug.WriteLine("HITThread: Couldn't find sound");
            }

            return -1;
        }

        /// <summary>
        /// Plays the current patch and loops it.
        /// </summary>
        /// <returns>-1 if unsuccessful, or the id of the note played.</returns>
        public int NoteLoop() //todo, make loop again.
        {
            var sound = audContent.GetSFX(Patch);

            if (sound != null)
            {
                var instance = sound.CreateInstance();
                instance.Volume = Volume;
                instance.Pan = Pan;
                instance.Play();

                var entry = new HITNoteEntry(instance, Patch);
                Notes.Add(entry);
                NotesByChannel.Add(instance, entry);
                return Notes.Count - 1;
            }
            else
            {
                Debug.WriteLine("HITThread: Couldn't find sound");
            }
            return -1;
        }

        /// <summary>
        /// Is a note active?
        /// </summary>
        /// <param name="note">The note to check.</param>
        /// <returns>True if active, false if not.</returns>
        public bool NoteActive(int note)
        {
            if (note == -1 || note >= Notes.Count) return false;
            return (Notes[note].instance.State != SoundState.Stopped);
        }

        /// <summary>
        /// Signals the VM to duck all threads with a higher ducking priority than this one.
        /// </summary>
        public void Duck()
        {
            //VM.Duck(this.DuckPriority);
        }

        /// <summary>
        /// Signals to the VM to unduck all threads that are currently ducked.
        /// </summary>
        public void Unduck()
        {
            //VM.Unduck();
        }

        private void LocalVarSet(int location, int value)
        {
            switch (location)
            {
                case 0x12: //patch, switch active track
                case 50:
                    var replacePatch = audContent.GetPatch((uint)value, ResGroup);
                    if (replacePatch != null) Patch = replacePatch; //river loop seems to try to load an invalid patch?
                    break;
            }
        }

        public void SetFlags(int value)
        {
            ZeroFlag = (value == 0);
            SignFlag = (value < 0);
        }

        public void WriteVar(int location, int value)
        {
            if (location < 0x10)
            {
                Registers[location] = value;
            }
            else if (location < 0x46)
            {
                LocalVarSet(location, value); //invoke any special behaviours, like track switch for setting patch
                LocalVar[location - 0x10] = value;
            }
            else if (location < 0x64)
            {
                return; //not mapped
            }
            else if (location < 0x88)
            {
                VM.WriteGlobal(location - 0x64, value);
            }
            else if (location < 0x271a)
            {
                return; //not mapped
            }
            else if (location < 0x2737)
            {
                ObjectVar[location - 0x271a] = value; //this probably should not be valid... but if it is used it may require some reworking to get this to sync across object threads.
            }
        }

        public int ReadVar(int location)
        {
            if (location < 0x10)
            {
                return Registers[location];
            } 
            else if (location < 0x46) 
            {
                return LocalVar[location - 0x10];
            }
            else if (location < 0x64)
            {
                return 0; //not mapped
            }
            else if (location < 0x88)
            {
                return VM.ReadGlobal(location - 0x64);
            }
            else if (location < 0x271a)
            {
                return 0; //not mapped
            }
            else if (location < 0x2737)
            {
                var loc = location - 0x271a;
                return ObjectVar[loc];
            }
            return 0;
        }

        public void JumpToEntryPoint(int TrackID)
        {
            PC = (uint)Src.EntryPointByTrackID[(uint)TrackID];
        }
    }

    public struct HITNoteEntry 
    {
        public SoundEffectInstance instance;
        public Patch Sound; //This is for killing specific sounds, see HITInterpreter.SeqGroupKill.
        public bool ended;

        public HITNoteEntry(SoundEffectInstance instance, Patch sound)
        {
            this.instance = instance;
            this.Sound = sound;
            this.ended = false;
        }
    }
}
