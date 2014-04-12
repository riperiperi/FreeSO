using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.HIT.model;
using TSO.Files.HIT;
using TSO.Content;
using Un4seen.Bass;

namespace TSO.HIT
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

        private Dictionary<string, HITEventRegistration> Events;

        public HITVM()
        {
            var content = TSO.Content.Content.Get();
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
        }

        public void WriteGlobal(int num, int value)
        {
            Globals[num] = value;
        }

        public int ReadGlobal(int num) 
        {
            return Globals[num];
        }

        private void RegisterEvents(HITResourceGroup group) {
            var events = group.evt;
            for (int i = 0; i < events.Entries.Count; i++)
            {
                var entry = events.Entries[i];
                if (!Events.ContainsKey(entry.Name))
                {
                    Events.Add(entry.Name, new HITEventRegistration()
                    {
                        Name = entry.Name,
                        EventType = entry.EventType,
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
        }

        public HITThread PlaySoundEvent(string evt)
        {
            if (ActiveEvents.ContainsKey(evt))
            {
                if (ActiveEvents[evt].Dead) ActiveEvents.Remove(evt); //if the last event is dead, remove and make a new one
                else return ActiveEvents[evt]; //an event of this type is already alive - here, take it.
            }

            var content = TSO.Content.Content.Get();
            if (Events.ContainsKey(evt))
            {
                var evtent = Events[evt];

                uint TrackID = 0;
                uint SubroutinePointer = 0;
                if (evtent.ResGroup.hsm != null)
                {
                    var c = evtent.ResGroup.hsm.Constants;
                    if (c.ContainsKey(evt)) SubroutinePointer = (uint)c[evt];
                    var trackIdName = "guid_tkd_"+evt;
                    if (c.ContainsKey(trackIdName)) TrackID = (uint)c[trackIdName];
                    else TrackID = evtent.TrackID;
                } else { //no hsm, fallback to eent and event track ids (tsov2)
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
    }
}
