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
using FSO.Content.Framework;
using FSO.Content.Model;
using System.Text.RegularExpressions;
using FSO.Common.Content;
using System.IO;
using System.Runtime.InteropServices;
using FSO.Files.Formats.DBPF;
using FSO.Files.XA;
using FSO.Files.UTK;
using FSO.Files.HIT;
using Microsoft.Xna.Framework.Audio;
using FSO.Content.Interfaces;

namespace FSO.Content
{
    /// <summary>
    /// Manager for the audio content.
    /// </summary>
    public class Audio : IAudioProvider
    {
        private Content ContentManager;
        public bool Initialized;

        /** Stations **/
        private List<AudioReference> Stations;
        private Dictionary<uint, AudioReference> StationsById;
        private List<AudioReference> Modes;

        /** Audio DBPFs **/
        public DBPFFile TSOAudio; //TSOAudio.dat
        public DBPFFile tsov2; //tsov2.dat
        public DBPFFile Stings; //Stings.dat
        public DBPFFile EP5Samps; //EP5Samps.dat
        public DBPFFile EP2; //EP2.dat
        public DBPFFile Hitlists; //HitListsTemp.dat
        public Dictionary<uint, string> NightclubSounds = new Dictionary<uint, string>();

        public Dictionary<uint, Track> TracksById;
        public Dictionary<uint, Track> TracksByBackupId;
        private Dictionary<uint, Hitlist> HitlistsById;


        /** Audio Cache **/
        public Dictionary<uint, SoundEffect> SFXCache;

        private Dictionary<string, HITEventRegistration> _Events;
        public Dictionary<string, HITEventRegistration> Events
        {
            get
            {
                return _Events;
            }
        }

        // from radio.ini, should probably load from there in future
        private Dictionary<string, string> _StationPaths = new Dictionary<string, string>
        {
            {"KBEA", "Music/Stations/Beach/"},
            {"KCLA", "Music/Stations/Classica/"},
            {"KCOU", "Music/Stations/Country/"},
            {"KCDA", "Music/Stations/CountryD/"},
            {"KDIS", "Music/Stations/Disco/"},
            {"KEZE", "Music/Stations/EZ/"},
            {"KEZX", "Music/Stations/EZX/"},
            {"KLAT", "Music/Stations/Latin/"},
            {"KRAP", "Music/Stations/Rap/"},
            {"KRAV", "Music/Stations/Rave/"},
            {"KROC", "Music/Stations/Rock/"},
// These ones aren't radio stations - they're UI music
            {"KMAP", "Music/Modes/Map/"},
            {"KSEL", "Music/Modes/Select/"},
            {"KCRE", "Music/Modes/Create/"},
//tv
            { "KACT", "sounddata/tvstations/tv_action/" },
            { "KCOM", "sounddata/tvstations/tv_comedy_cartoon/" },
            { "KMYS", "sounddata/tvstations/tv_mystery/" },
            { "KROM", "sounddata/tvstations/tv_romance/" },
// More music
            {"KHOR", "Music/Stations/Horror/"},
            {"KOLD", "Music/Stations/OldWorld/"},
            {"KSCI", "Music/Stations/SciFi/"},
        };

        private Dictionary<int, string> _MusicModes = new Dictionary<int, string>
        {
            { 11, "KSEL" },
            { 12, "KCRE" },
            { 13, "KMAP" },
            { 9, "" }
        };

        public Dictionary<string, string> StationPaths
        {
            get
            {
                return _StationPaths;
            }
        }

        public Dictionary<int, string> MusicModes
        {
            get
            {
                return _MusicModes;
            }
        }

        public Audio(Content contentManager)
        {
            this.ContentManager = contentManager;
        }

        /// <summary>
        /// Initializes the audio manager.
        /// </summary>
        public void Init()
        {
            if (Initialized) return;
            this.Stations = new List<AudioReference>();
            this.StationsById = new Dictionary<uint, AudioReference>();
            this.Modes = new List<AudioReference>();

            var stationsRegEx = new Regex(@"music/stations/.*\.mp3");

            foreach (var file in ContentManager.AllFiles)
            {
                if (stationsRegEx.IsMatch(file))
                {
                    var reference = new AudioReference { Type = AudioType.RADIO_STATION, FilePath = ContentManager.GetPath(file) };
                    Stations.Add(reference);
                    var idString = Path.GetFileNameWithoutExtension(file);
                    idString = idString.Substring(idString.LastIndexOf("_") + 1);
                    var id = Convert.ToUInt32(idString, 16);
                    reference.ID = id;
                    StationsById.Add(id, reference);
                }
            }

            TSOAudio = new DBPFFile(ContentManager.GetPath("TSOAudio.dat"));
            tsov2 = new DBPFFile(ContentManager.GetPath("tsov2.dat"));
            Stings = new DBPFFile(ContentManager.GetPath("Stings.dat"));
            EP5Samps = new DBPFFile(ContentManager.GetPath("EP5Samps.dat"));
            EP2 = new DBPFFile(ContentManager.GetPath("EP2.dat"));
            Hitlists = new DBPFFile(ContentManager.GetPath("HitListsTemp.dat"));

            SFXCache = new Dictionary<uint, SoundEffect>();
            TracksById = new Dictionary<uint, Track>();
            TracksByBackupId = new Dictionary<uint, Track>();
            HitlistsById = new Dictionary<uint, Hitlist>();

            AddTracksFrom(TSOAudio);

            //load events
            _Events = new Dictionary<string, HITEventRegistration>();
            var content = ContentManager;
            var newmain = LoadHitGroup(content.GetPath("sounddata/newmain.hit"), content.GetPath("sounddata/eventlist.txt"), content.GetPath("sounddata/newmain.hsm"));
            var relationships = LoadHitGroup(content.GetPath("sounddata/relationships.hit"), content.GetPath("sounddata/relationships.evt"), content.GetPath("sounddata/relationships.hsm"));
            var tsoep5 = LoadHitGroup(content.GetPath("sounddata/tsoep5.hit"), content.GetPath("sounddata/tsoep5.evt"), content.GetPath("sounddata/tsoep5.hsm"));
            var tsoV2 = LoadHitGroup(content.GetPath("sounddata/tsov2.hit"), content.GetPath("sounddata/tsov2.evt"), null); //tsov2 has no hsm file
            var tsov3 = LoadHitGroup(content.GetPath("sounddata/tsov3.hit"), content.GetPath("sounddata/tsov3.evt"), content.GetPath("sounddata/tsov3.hsm"));
            var turkey = LoadHitGroup(content.GetPath("sounddata/turkey.hit"), content.GetPath("sounddata/turkey.evt"), content.GetPath("sounddata/turkey.hsm"));

            RegisterEvents(newmain);
            RegisterEvents(relationships);
            RegisterEvents(tsoep5);
            RegisterEvents(tsoV2);
            RegisterEvents(tsov3);
            RegisterEvents(turkey);

            //register the .xa files over in the nightclub folders.
            var files = Directory.GetFiles(content.GetPath("sounddata/nightclubsounds/"));
            foreach (var file in files)
            {
                if (!file.EndsWith(".xa")) continue;
                var split = file.Split('_');
                uint id = 0;
                try
                {
                    var endSplit = split[split.Length - 1];
                    id = Convert.ToUInt32("0x"+endSplit.Substring(0, endSplit.Length-3), 16);
                }
                catch { continue; }

                NightclubSounds[id] = file;
            }

            Initialized = true;
        }

        /// <summary>
        /// Gets a track from a DBPF using its InstanceID.
        /// </summary>
        /// <param name="dbpf">The DBPF to search.</param>
        private void AddTracksFrom(DBPFFile dbpf)
        {
            var tracks = dbpf.GetItemsByType(DBPFTypeID.TRK);
            for (var i=0; i<tracks.Count; i++) 
            {
                var track = new Track(tracks[i].Value);
                var realid = tracks[i].Key;
                TracksById.Add(realid, track);
                TracksByBackupId[track.TrackID] = track;
            }
        }

        /// <summary>
        /// Gets a audio file from a DBPF using its InstanceID.
        /// </summary>
        /// <param name="InstanceID">The InstanceID of the audio.</param>
        /// <param name="dbpf">The DBPF to search.</param>
        /// <returns>The audio as a stream of bytes.</returns>
        private byte[] GetAudioFrom(uint InstanceID, DBPFFile dbpf, out byte filetype) 
        {
            filetype = 0;
            if (InstanceID == 0)
                return null;

            //all game sfx has type id 0x2026960B
            byte[] dat = dbpf.GetItemByID((ulong)DBPFTypeID.SoundFX + (((ulong)InstanceID)<<32));

            if (dat != null)
            {
                string head = new string(new char[] { (char)dat[0], (char)dat[1], (char)dat[2], (char)dat[3] });
                if (head.StartsWith("XA"))
                {
                    filetype = 1;
                    return new XAFile(dat).DecompressedData;
                }
                else if (head.StartsWith("UTM0"))
                {
                    filetype = 2;
                    var utk = new UTKFile2(dat);
                    utk.UTKDecode();
                    return utk.DecompressedWav;
                }
                else
                {
                    filetype = 3;
                    return dat; //either wav or mp3.
                }
            }
            else
                Debug.WriteLine("Couldn't find sound!");
            return null;
        }

        /// <summary>
        /// Gets a Hitlist from a DBPF using its InstanceID.
        /// </summary>
        /// <param name="InstanceID">The InstanceID of the Hitlist.</param>
        /// <param name="dbpf">The DBPF to search.</param>
        /// <returns>A Hitlist instance.</returns>
        private Hitlist GetHitlistFrom(uint InstanceID, DBPFFile dbpf)
        {
            var hit = dbpf.GetItemByID((ulong)DBPFTypeID.HIT + (((ulong)InstanceID) << 32));
            if (hit != null) return new Hitlist(hit);

            return null;
        }

        /// <summary>
        /// Gets a Hitlist from a DBPF using its InstanceID.
        /// </summary>
        /// <param name="InstanceID">The InstanceID of the Hitlist.</param>
        /// <returns>A Hitlist instance.</returns>
        public Hitlist GetHitlist(uint InstanceID, HITResourceGroup group)
        {
            if (HitlistsById.ContainsKey(InstanceID)) return HitlistsById[InstanceID];

            var hit1 = GetHitlistFrom(InstanceID, Hitlists);
            if (hit1 != null)
            {
                HitlistsById.Add(InstanceID, hit1);
                return HitlistsById[InstanceID];
            }

            var hit2 = GetHitlistFrom(InstanceID, TSOAudio);
            if (hit2 != null)
            {
                HitlistsById.Add(InstanceID, hit2);
                return HitlistsById[InstanceID];
            }

            return null; //found nothing :'(
        }

        /// <summary>
        /// Gets a Track using its ID.
        /// </summary>
        /// <param name="value">Track ID</param>
        /// <param name="fallback">(TSO ONLY) Secondary Track ID lookup</param>
        /// <returns>A Track instance.</returns>
        public Track GetTrack(uint value, uint fallback, HITResourceGroup group)
        {
            if (TracksById.ContainsKey(value))
            {
                return TracksById[value];
            }
            else
            {
                if ((fallback != 0) && TracksById.ContainsKey(fallback))
                {
                    return TracksById[fallback];
                }
                else
                {
                    if (TracksByBackupId.ContainsKey(value))
                    {
                        return TracksByBackupId[value];
                    }
                    else
                    {
                        if (TracksByBackupId.ContainsKey(fallback))
                        {
                            return TracksByBackupId[fallback];
                        }
                        else
                        {
                            Debug.WriteLine("Couldn't find track: " + value + ", with alternative " + fallback);
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Gets a sound effect from the sound effects cache.
        /// </summary>
        /// <param name="Patch">A Patch instance containing the file location or ID.</param>
        /// <returns>The sound effect.</returns>
        public SoundEffect GetSFX(Patch patch)
        {
            if (patch == null) return null;
            var InstanceID = patch.FileID;
            if (SFXCache.ContainsKey(InstanceID)) return SFXCache[InstanceID];
            byte filetype = 0;

            byte[] data = GetAudioFrom(InstanceID, TSOAudio, out filetype);
            if (data == null) data = GetAudioFrom(InstanceID, tsov2, out filetype);
            if (data == null) data = GetAudioFrom(InstanceID, Stings, out filetype);
            if (data == null) data = GetAudioFrom(InstanceID, EP5Samps, out filetype);
            if (data == null) data = GetAudioFrom(InstanceID, EP2, out filetype);
            if (data == null)
            {
                string source;
                if (NightclubSounds.TryGetValue(InstanceID, out source))
                {
                    data = new XAFile(source).DecompressedData;
                }
            }

            if (data != null)
            {
                var stream = new MemoryStream(data);

                var sfx = SoundEffect.FromStream(stream);
                stream.Close();
                SFXCache.Add(InstanceID, sfx);
                switch (filetype)
                {
                    case 2:
                        sfx.Name = "VOX";
                        break;
                    case 3:
                        sfx.Name = "MUSIC";
                        break;
                    default:
                        sfx.Name = "FX";
                        break;
                }
                return sfx; //remember to clear the sfx cache between lots!
            }
            else
            {
                //GCHandle pinnedArray = GCHandle.Alloc(new byte[1], GCHandleType.Weak);
                return null;// pinnedArray; //we couldn't find anything! can't return null so do this... not the best idea tbh
            }
        }

        /// <summary>
        /// Gets a Patch instance for the given patch ID.
        /// TSO: Patch ID directly translates to FileID.
        /// TS1: Patch ID lookup to obtain patch.
        /// </summary>
        /// <param name="id">The Patch ID.</param>
        /// <returns>A Patch instance.</returns>
        public Patch GetPatch(uint id, HITResourceGroup group)
        {
            return new Patch(id);
        }

        /// <summary>
        /// Compiles the radio stations in the game to a list of AudioReference instances.
        /// </summary>
        /// <returns>The radio stations in the game as a list of AudioReference instances.</returns>
        public List<AudioReference> List()
        {
            var result = new List<AudioReference>();
            result.AddRange(Stations);
            return result;
        }

        private void RegisterEvents(HITResourceGroup group)
        {
            var events = group.evt;
            for (int i = 0; i < events.Entries.Count; i++)
            {
                var entry = events.Entries[i];
                if (!_Events.ContainsKey(entry.Name))
                {
                    _Events.Add(entry.Name, new HITEventRegistration()
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
    }
}
