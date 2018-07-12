/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.HIT.Model;
using FSO.Files.HIT;
using FSO.Content;
using System.IO;
using FSO.HIT.Events;
using Microsoft.Xna.Framework.Audio;
using FSO.Common;

namespace FSO.HIT
{
    public class HITVM
    {
        public static bool DISABLE_SOUND = false;
        private static HITVM INSTANCE;

        public static HITVM Get()
        {
            return INSTANCE; //there can be only one!
        }

        public static void Init()
        {
            DISABLE_SOUND = FSOEnvironment.NoSound;
            INSTANCE = new HITVM();
        }

        //non static stuff

        private Dictionary<string, HITSound> ActiveEvents; //events that are active are reused for all objects calling that event.
        private List<HITSound> Sounds;
        private int[] Globals; //SimSpeed 0x64 to CampfireSize 0x87.
        private HITTVOn TVEvent;
        private HITTVOn MusicEvent;
        private HITTVOn NextMusic;
        public AudioListener Listener = new AudioListener();

        private List<FSCPlayer> FSCPlayers;
        public List<SoundEffectInstance> AmbLoops;
        public List<HITNoteEntry> PlayQueue = new List<HITNoteEntry>();
        private float[] GroupMasterVolumes = new float[]
        {
            1.0f, 1.0f, 1.0f, 1.0f
        };

        public HITVM()
        {
            var content = FSO.Content.Content.Get();

            Globals = new int[36];
            Sounds = new List<HITSound>();
            ActiveEvents = new Dictionary<string, HITSound>();
            FSCPlayers = new List<FSCPlayer>();
            AmbLoops = new List<SoundEffectInstance>();
        }

        public void SetMasterVolume(HITVolumeGroup group, float volume)
        {
            GroupMasterVolumes[(int)group] = volume;
            foreach (var sound in Sounds)
            {
                if (sound.VolGroup == group) sound.RecalculateVolume();
            }

            foreach (var amb in AmbLoops)
            {
                amb.Volume = GetMasterVolume(HITVolumeGroup.AMBIENCE);
            }
        }

        public float GetMasterVolume(HITVolumeGroup group)
        {
            return GroupMasterVolumes[(int)group];
        }

        public void WriteGlobal(int num, int value)
        {
            Globals[num] = value;
        }

        public int ReadGlobal(int num) 
        {
            return Globals[num];
        }

        public void QueuePlay(HITNoteEntry note)
        {
            PlayQueue.Add(note);
        }

        public bool NightclubMode;

        public void Tick()
        {
            if (NightclubMode)
            {
                //find the loudest nightclub sound
                var nc = Sounds.Where(x => x.Name?.StartsWith("nc_") == true);
                if (nc.Count() == 0) NightclubMode = false;
                else
                {
                    var max = nc.OrderBy(x => x.GetVolume()).Last();
                    var bestID = max.Name.Last();
                    foreach (var sound in nc)
                    {
                        if (sound.Name.Last() != bestID) sound.Mute();
                    }
                }
            }

            for (int i = 0; i < Sounds.Count; i++)
            {
                if (!Sounds[i].Tick())
                {
                    Sounds[i].Dispose();
                    Sounds.RemoveAt(i--);
                }
            }

            foreach (var item in PlayQueue)
            {
                item.started = true;
                item.instance.Play();
            }
            if (PlayQueue.Count > 0) PlayQueue.Clear();

            if (NextMusic != null)
            {
                if (MusicEvent == null || MusicEvent.Dead)
                {
                    MusicEvent = NextMusic;
                    Sounds.Add(NextMusic);
                    NextMusic = null;
                }
            }

            for (int i = 0; i < FSCPlayers.Count; i++)
            {
                FSCPlayers[i].Tick(1/60f);
            }
        }

        public void StopFSC(FSCPlayer input)
        {
            FSCPlayers.Remove(input);
        }

        public FSCPlayer PlayFSC(string path)
        {
            var dir = Path.GetDirectoryName(path)+"/";
            FSC fsc = new FSC(path);
            var player = new FSCPlayer(fsc, dir);
            FSCPlayers.Add(player);

            return player;
        }

        public HITSound PlaySoundEvent(string evt)
        {
            if (DISABLE_SOUND) return null;
            evt = evt.ToLowerInvariant();
            if (evt.StartsWith("nc_")) NightclubMode = true;
            HITThread InterruptBlocker = null; //the thread we have to wait for to finish before we begin.
            if (ActiveEvents.ContainsKey(evt))
            {
                var aevt = ActiveEvents[evt];
                if (aevt.Dead) ActiveEvents.Remove(evt); //if the last event is dead, remove and make a new one
                else
                {
                    if ((aevt as HITThread)?.InterruptBlocker != null)
                    {
                        //we can stop this thread - steal its waiter
                        (aevt as HITThread).Dead = true;
                        InterruptBlocker = (aevt as HITThread).InterruptBlocker;
                    } else if ((aevt as HITThread)?.Interruptable == true)
                    {
                        InterruptBlocker = (aevt as HITThread);
                    }
                    else return aevt; //an event of this type is already alive - here, take it.
                }
            }

            var content = FSO.Content.Content.Get();
            var evts = content.Audio.Events;

            if (evts.ContainsKey(evt))
            {
                var evtent = evts[evt];

                //objects call the wrong event for piano playing
                //there is literally no file or evidence that this is not hard code mapped to PlayPiano in TSO, so it's hardcoded here.
                //the track and HSM associated with the piano_play event, however, are correct. it's just the subroutine that is renamed.
                if (evt.Equals("piano_play", StringComparison.InvariantCultureIgnoreCase))
                {
                    evt = "playpiano";
                    if (ActiveEvents.ContainsKey(evt))
                    {
                        if (ActiveEvents[evt].Dead) ActiveEvents.Remove(evt); //if the last event is dead, remove and make a new one
                        else return ActiveEvents[evt]; //an event of this type is already alive - here, take it.
                    }
                }

                uint TrackID = 0;
                uint SubroutinePointer = 0;

                if (content.TS1)
                {
                    TrackID = evtent.TrackID;
                    var track = content.Audio.GetTrack(TrackID, 0, evtent.ResGroup);
                    if (track != null && track.SubroutineID != 0) SubroutinePointer = track.SubroutineID;
                }
                else
                {
                    if (evtent.ResGroup.hsm != null)
                    {
                        var c = evtent.ResGroup.hsm.Constants;
                        if (c.ContainsKey(evt)) SubroutinePointer = (uint)c[evt];
                        var trackIdName = "guid_tkd_" + evt;
                        if (c.ContainsKey(trackIdName)) TrackID = (uint)c[trackIdName];
                        else TrackID = evtent.TrackID;
                    }
                    else
                    { //no hsm, fallback to eent and event track ids (tsov2)
                        var entPoints = evtent.ResGroup.hit.EntryPointByTrackID;
                        TrackID = evtent.TrackID;
                        if (entPoints != null && entPoints.ContainsKey(evtent.TrackID)) SubroutinePointer = entPoints[evtent.TrackID];
                    }
                }

                if (evtent.EventType == HITEvents.kTurnOnTV)
                {
                    var thread = new HITTVOn(evtent.TrackID, this);
                    thread.VolGroup = HITVolumeGroup.FX;
                    Sounds.Add(thread);
                    ActiveEvents.Add(evt, thread);
                    return thread;
                }
                else if (evtent.EventType == HITEvents.kSetMusicMode)
                {
                    var thread = new HITTVOn(evtent.TrackID, this, true);
                    thread.VolGroup = HITVolumeGroup.MUSIC;
                    ActiveEvents.Add(evt, thread);
                    if (NextMusic != null) NextMusic.Kill();
                    if (MusicEvent != null) MusicEvent.Fade();
                    NextMusic = thread;
                    return thread;
                }
                else if (SubroutinePointer != 0)
                {
                    var thread = new HITThread(evtent.ResGroup, this);
                    thread.PC = SubroutinePointer;
                    thread.LoopPointer = (int)thread.PC;
                    if (TrackID != 0) thread.SetTrack(TrackID, evtent.TrackID);
                    Sounds.Add(thread);
                    ActiveEvents[evt] = thread;
                    if (InterruptBlocker != null)
                    {
                        InterruptBlocker.Interrupt(thread);
                        if (!InterruptBlocker.Name.StartsWith("nc_")) InterruptBlocker.KillVocals();
                    }
                    thread.Name = evt;
                    return thread;
                }
                else if (TrackID != 0 && content.Audio.GetTrack(TrackID, 0, evtent.ResGroup) != null)
                {
                    var thread = new HITThread(TrackID, this, evtent.ResGroup);
                    Sounds.Add(thread);
                    ActiveEvents[evt] = thread;
                    if (InterruptBlocker != null)
                    {
                        InterruptBlocker.Interrupt(thread);
                        if (!InterruptBlocker.Name.StartsWith("nc_")) InterruptBlocker.KillVocals();
                    }
                    thread.Name = evt;
                    return thread;
                }
            }

            return null;
        }
    }
}
