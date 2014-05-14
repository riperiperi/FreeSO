using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Files.HIT;
using Un4seen.Bass;

namespace TSO.HIT
{
    public class HITThread
    {
        public uint PC; //program counter
        public HITFile Src;
        public HITVM VM;
        private Hitlist Hitlist;
        private int[] Registers; //includes args, vars, whatever "h" is up to 0xf
        private int[] LocalVar; //the sims online set, 0x10 "argstyle" up to 0x45 orientz. are half of these even used? no. but even in the test files? no
        private int[] ObjectVar; //IsInsideViewFrustrum 0x271a to Neatness 0x2736. Set by object on thread invocation.
        private Track ActiveTrack;
        public int LoopPointer = -1;
        public int WaitRemain = -1;

        public bool Dead;

        private bool EverHadOwners; //if we never had owners, don't kill the thread. (ui sounds)
        private List<int> Owners;

        private bool SimpleMode; //certain sounds play with no HIT.
        private bool PlaySimple;
        private bool VolumeSet;
        private float Volume = 1;
        private float Pan;

        private uint Patch; //sound id

        private List<HITNoteEntry> Notes;
        private Dictionary<int, HITNoteEntry> NotesByChannel;
        public int LastNote
        {
            get { return Notes.Count - 1; }
        }

        public bool ZeroFlag; //flags set by instructions
        public bool SignFlag;

        public Stack<int> Stack;

        private TSO.Content.Audio audContent;

        public bool Tick() //true if continue, false if kill
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
                    Bass.BASS_ChannelSetAttribute(Notes[i].channel, BASSAttribute.BASS_ATTRIB_VOL, Volume);
                    Bass.BASS_ChannelSetAttribute(Notes[i].channel, BASSAttribute.BASS_ATTRIB_PAN, Pan);
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
                while (true)
                {
                    var opcode = Src.Data[PC++];
                    if (opcode > HITInterpreter.Instructions.Length) opcode = 0;
                    var result = HITInterpreter.Instructions[opcode](this);
                    if (result == HITResult.HALT) return true;
                    else if (result == HITResult.KILL)
                    {
                        Dead = true;
                        return false;
                    }
                }
            }
        }

        private void KillVocals()
        { //kill all playing sounds
            for (int i = 0; i < Notes.Count; i++)
            {
                if (NoteActive(i)) Bass.BASS_ChannelStop(Notes[i].channel);
            }
        }

        public HITThread(HITFile Src, HITVM VM)
        {
            this.Src = Src;
            this.VM = VM;
            Registers = new int[16];
            LocalVar = new int[54];
            ObjectVar = new int[29];

            Notes = new List<HITNoteEntry>();
            NotesByChannel = new Dictionary<int, HITNoteEntry>();
            Owners = new List<int>();

            Stack = new Stack<int>();
            audContent = Content.Content.Get().Audio;
        }

        public HITThread(uint TrackID)
        {
            Owners = new List<int>();
            Notes = new List<HITNoteEntry>();
            NotesByChannel = new Dictionary<int, HITNoteEntry>();

            audContent = Content.Content.Get().Audio;
            SetTrack(TrackID);
            Patch = ActiveTrack.SoundID;
            SimpleMode = true;
            PlaySimple = true; //play next frame, so we have time to set volumes.
        }

        public void SetVolume(float volume, float pan)
        {
            if (VolumeSet)
            {
                if (volume > Volume)
                {
                    Volume = volume;
                    Pan = pan;
                }
            }
            else
            {
                Volume = volume;
                Pan = pan;
            }
            VolumeSet = true;
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

        public void LoadHitlist(uint id)
        {
            Hitlist = audContent.GetHitlist(id);
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
            if (audContent.TracksById.ContainsKey(value))
            {
                ActiveTrack = audContent.TracksById[value];
                Patch = ActiveTrack.SoundID;
            }
        }

        public int NoteOn()
        {
            var sound = audContent.GetSFX(Patch);
            int length = ((byte[])sound.Target).Length;
            if (length != 1) //1 byte length array is returned when no sound is found
            {
                IntPtr pointer = sound.AddrOfPinnedObject();
                int channel = Bass.BASS_StreamCreateFile(pointer, 0, length, BASSFlag.BASS_DEFAULT | BASSFlag.BASS_STREAM_AUTOFREE);
                Bass.BASS_ChannelSetAttribute(channel, BASSAttribute.BASS_ATTRIB_VOL, Volume);
                Bass.BASS_ChannelSetAttribute(channel, BASSAttribute.BASS_ATTRIB_PAN, Pan);
                Bass.BASS_ChannelPlay(channel, false);

                var entry = new HITNoteEntry(channel);
                Notes.Add(entry);
                NotesByChannel.Add(channel, entry);
                return Notes.Count-1;
            }
            return -1;
        }

        public bool NoteActive(int note)
        {
            if (note == -1 || note >= Notes.Count) return false;
            return (Bass.BASS_ChannelIsActive(Notes[note].channel) == BASSActive.BASS_ACTIVE_PLAYING || Bass.BASS_ChannelIsActive(Notes[note].channel) == BASSActive.BASS_ACTIVE_STALLED);
        }

        private void LocalVarSet(int location, int value)
        {
            switch (location)
            {
                case 0x12: //patch, switch active track
                    Patch = (uint)value;
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
                return ObjectVar[location - 0x271a];
            }
            return 0;
        }

        public void JumpToEntryPoint(int TrackID) {
            PC = (uint)Src.EntryPointByTrackID[(uint)TrackID];
        }


    }

    public struct HITNoteEntry 
    {
        public int channel;
        public bool ended;

        public HITNoteEntry(int channel)
        {
            this.channel = channel;
            this.ended = false;
        }
    }
}
