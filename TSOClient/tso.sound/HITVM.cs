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

namespace FSO.HIT
{
    public class HITVM
    {
        private static HITVM INSTANCE;

        public static HITVM Get()
        {
            return INSTANCE; //there can be only one!
        }

        public static void Init()
        {
            INSTANCE = new HITVM();
        }

        //non static stuff

        private HITResourceGroup newmain;
        private HITResourceGroup relationships;
        private HITResourceGroup tsoep5;
        private HITResourceGroup tsov2;
        private HITResourceGroup tsov3;
        private HITResourceGroup turkey;

        private Dictionary<string, HITThread> ActiveEvents; //events that are active are reused for all objects calling that event.
        private List<HITThread> Threads;
        private int[] Globals; //SimSpeed 0x64 to CampfireSize 0x87.

        private List<FSCPlayer> FSCPlayers;

        private Dictionary<string, HITEventRegistration> Events;

        public HITVM()
        {
            var content = FSO.Content.Content.Get();
            Events = new Dictionary<string, HITEventRegistration>();

            newmain = LoadHitGroup(content.GetPath("sounddata/newmain.hit"), content.GetPath("sounddata/eventlist.txt"), content.GetPath("sounddata/newmain.hsm"));
            relationships = LoadHitGroup(content.GetPath("sounddata/relationships.hit"), content.GetPath("sounddata/relationships.evt"), content.GetPath("sounddata/relationships.hsm"));
            tsoep5 = LoadHitGroup(content.GetPath("sounddata/tsoep5.hit"), content.GetPath("sounddata/tsoep5.evt"), content.GetPath("sounddata/tsoep5.hsm"));
            tsov2 = LoadHitGroup(content.GetPath("sounddata/tsov2.hit"), content.GetPath("sounddata/tsov2.evt"), null); //tsov2 has no hsm file
            tsov3 = LoadHitGroup(content.GetPath("sounddata/tsov3.hit"), content.GetPath("sounddata/tsov3.evt"), content.GetPath("sounddata/tsov3.hsm"));
            turkey = LoadHitGroup(content.GetPath("sounddata/turkey.hit"), content.GetPath("sounddata/turkey.evt"), content.GetPath("sounddata/turkey.hsm"));

            RegisterEvents(newmain);
            RegisterEvents(relationships);
            RegisterEvents(tsoep5);
            RegisterEvents(tsov2);
            RegisterEvents(tsov3);
            RegisterEvents(turkey);

            Globals = new int[36];
            Threads = new List<HITThread>();
            ActiveEvents = new Dictionary<string, HITThread>();
            FSCPlayers = new List<FSCPlayer>();
        }

        public void WriteGlobal(int num, int value)
        {
            Globals[num] = value;
        }

        public int ReadGlobal(int num) 
        {
            return Globals[num];
        }

        private void RegisterEvents(HITResourceGroup group)
        {
            var events = group.evt;
            for (int i = 0; i < events.Entries.Count; i++)
            {
                var entry = events.Entries[i];
                if (!Events.ContainsKey(entry.Name))
                {
                    Events.Add(entry.Name, new HITEventRegistration()
                    {
                        Name = entry.Name,
                        EventType = (FSO.Files.HIT.HITEvents)entry.EventType,
                        TrackID = entry.TrackID,
                        ResGroup = group
                    });
                }
            }
        }

        private HITResourceGroup LoadHitGroup(string HITPath, string EVTPath, string HSMPath)
        {
            var events = new EVT(EVTPath);
            var hitfile = new HITFile(HITPath);
            HSM hsmfile = null;
            if (HSMPath != null) hsmfile = new HSM(HSMPath);

            return new HITResourceGroup()
            {
                evt = events,
                hit = hitfile,
                hsm = hsmfile
            };
        }

        public void Tick()
        {
            for (int i = 0; i < Threads.Count; i++)
            {
                if (!Threads[i].Tick()) Threads.RemoveAt(i--);
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
            var dir = Path.GetDirectoryName(path)+"\\";
            FSC fsc = new FSC(path);
            var player = new FSCPlayer(fsc, dir);
            FSCPlayers.Add(player);

            return player;
        }

        public HITThread PlaySoundEvent(string evt)
        {
            evt = evt.ToLower();
            if (ActiveEvents.ContainsKey(evt))
            {
                if (ActiveEvents[evt].Dead) ActiveEvents.Remove(evt); //if the last event is dead, remove and make a new one
                else return ActiveEvents[evt]; //an event of this type is already alive - here, take it.
            }

            var content = FSO.Content.Content.Get();
            if (Events.ContainsKey(evt))
            {
                var evtent = Events[evt];

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
                    if (entPoints.ContainsKey(evtent.TrackID)) SubroutinePointer = entPoints[evtent.TrackID];
                }

                if (SubroutinePointer != 0)
                {
                    var thread = new HITThread(evtent.ResGroup.hit, this);
                    thread.PC = SubroutinePointer;
                    if (TrackID != 0) thread.SetTrack(TrackID);

                    Threads.Add(thread);
                    ActiveEvents.Add(evt, thread);
                    return thread;
                }
                else if (TrackID != 0 && content.Audio.TracksById.ContainsKey(TrackID))
                {
                    var thread = new HITThread(TrackID);
                    Threads.Add(thread);
                    ActiveEvents.Add(evt, thread);
                    return thread;
                }
            }

            return null;
        }

        /// <summary>
        /// Ducks all sounds with a priority lower than the one passed.
        /// </summary>
        /// <param name="DuckPri">The ducking priority under which to duck other sounds.</param>
        public void Duck(HITDuckingPriorities DuckPri)
        {
            for (int i = 0; i < Threads.Count; i++)
            {
                //0 means least importance, so it gets ducked.
                if (Threads[i].DuckPriority < DuckPri)
                {
                    switch (DuckPri)
                    {
                        case HITDuckingPriorities.duckpri_low:
                            Threads[i].SetVolume(0.15f, Threads[i].Pan);
                            break;
                        case HITDuckingPriorities.duckpri_normal:
                            Threads[i].SetVolume(0.25f, Threads[i].Pan);
                            break;
                        case HITDuckingPriorities.duckpri_high:
                            Threads[i].SetVolume(0.45f, Threads[i].Pan);
                            break;
                        case HITDuckingPriorities.duckpri_higher:
                            Threads[i].SetVolume(0.65f, Threads[i].Pan);
                            break;
                        case HITDuckingPriorities.duckpri_evenhigher:
                            //Threads[i].SetVolume(0.85f, Threads[i].Pan);
                            //If ducking priority is duckpri_never, it shouldn't be ducked!
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Unducks all threads, I.E sets their volume back to what it was before Duck() was called.
        /// </summary>
        public void Unduck()
        {
            for (int i = 0; i < Threads.Count; i++)
                Threads[i].SetVolume(Threads[i].PreviousVolume, Threads[i].Pan);
        }
    }
}
