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


        private Dictionary<string, HITEventRegistration> Events;

        public HITVM()
        {
            var content = TSO.Content.Content.Get();
            Events = new Dictionary<string, HITEventRegistration>();

            newmain = LoadHitGroup(content.GetPath("sounddata/newmain.hit"), content.GetPath("sounddata/eventlist.txt"), content.GetPath("sounddata/newmain.hot"));
            relationships = LoadHitGroup(content.GetPath("sounddata/relationships.hit"), content.GetPath("sounddata/relationships.evt"), content.GetPath("sounddata/relationships.hot"));
            tsoep5 = LoadHitGroup(content.GetPath("sounddata/tsoep5.hit"), content.GetPath("sounddata/tsoep5.evt"), content.GetPath("sounddata/tsoep5.hot"));
            tsov2 = LoadHitGroup(content.GetPath("sounddata/tsov2.hit"), content.GetPath("sounddata/tsov2.evt"), null); //tsov2 has no hot file
            tsov3 = LoadHitGroup(content.GetPath("sounddata/tsov3.hit"), content.GetPath("sounddata/tsov3.evt"), content.GetPath("sounddata/tsov3.hot"));
            turkey = LoadHitGroup(content.GetPath("sounddata/turkey.hit"), content.GetPath("sounddata/turkey.evt"), content.GetPath("sounddata/turkey.hot"));

            RegisterEvents(newmain);
            RegisterEvents(relationships);
            RegisterEvents(tsoep5);
            RegisterEvents(tsov2);
            RegisterEvents(tsov3);
            RegisterEvents(turkey);
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

        private HITResourceGroup LoadHitGroup(string HITPath, string EVTPath, string HOTPath)
        {
            var events = new EVT(EVTPath);
            var hitfile = new HITFile(HITPath);

            return new HITResourceGroup()
            {
                evt = events,
                hit = hitfile
            };
        }

        public void PlaySoundEvent(string evt) {
            var content = TSO.Content.Content.Get();
            if (Events.ContainsKey(evt))
            {
                var evtent = Events[evt];
                if (evtent.TrackID != 0 && content.Audio.TracksById.ContainsKey(evtent.TrackID))
                {
                    var track = content.Audio.TracksById[evtent.TrackID];

                    //temporary from here onwards. when vm works, we will run the HIT thread.
                    var sound = content.Audio.GetSFX(track.SoundID);
                    int length = ((byte[])sound.Target).Length;
                    if (length != 1) //1 byte length array is returned when no sound is found
                    {
                        IntPtr pointer = sound.AddrOfPinnedObject();
                        int channel = Bass.BASS_StreamCreateFile(pointer, 0, length, BASSFlag.BASS_DEFAULT);
                        Bass.BASS_ChannelPlay(channel, false);
                    }
                }
            }
            System.Diagnostics.Debug.WriteLine(evt);
        }
    }
}
