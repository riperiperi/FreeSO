using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using TSO.Content.framework;
using TSO.Content.model;
using System.Text.RegularExpressions;
using TSO.Common.content;
using System.IO;
using System.Runtime.InteropServices;
using TSO.Files.formats.dbpf;
using TSO.Files.XA;
using TSO.Files.UTK;
using TSO.Files.HIT;
using Microsoft.Xna.Framework.Audio;

namespace TSO.Content
{
    /// <summary>
    /// Manager for the audio content.
    /// </summary>
    public class Audio
    {
        private Content ContentManager;

        /** Stations **/
        private List<AudioReference> Stations;
        private Dictionary<uint, AudioReference> StationsById;
        private List<AudioReference> Modes;

        /** Audio DBPFs **/
        public DBPF TSOAudio; //TSOAudio.dat
        public DBPF tsov2; //tsov2.dat
        public DBPF Stings; //Stings.dat
        public DBPF EP5Samps; //EP5Samps.dat
        public DBPF EP2; //EP2.dat
        public DBPF Hitlists; //HitListsTemp.dat

        public Dictionary<uint, Track> TracksById;
        private Dictionary<uint, Hitlist> HitlistsById;


        /** Audio Cache **/
        public Dictionary<uint, SoundEffect> SFXCache;

        public Audio(Content contentManager)
        {
            this.ContentManager = contentManager;
        }

        /// <summary>
        /// Initializes the audio manager.
        /// </summary>
        public void Init()
        {
            this.Stations = new List<AudioReference>();
            this.StationsById = new Dictionary<uint, AudioReference>();
            this.Modes = new List<AudioReference>();

            var stationsRegEx = new Regex(@"music\\stations\\.*\.mp3");

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

            TSOAudio = new DBPF(ContentManager.GetPath("TSOAudio.dat"));
            tsov2 = new DBPF(ContentManager.GetPath("tsov2.dat"));
            Stings = new DBPF(ContentManager.GetPath("Stings.dat"));
            EP5Samps = new DBPF(ContentManager.GetPath("EP5Samps.dat"));
            EP2 = new DBPF(ContentManager.GetPath("EP2.dat"));
            Hitlists = new DBPF(ContentManager.GetPath("HitListsTemp.dat"));

            SFXCache = new Dictionary<uint, SoundEffect>();
            TracksById = new Dictionary<uint, Track>();
            HitlistsById = new Dictionary<uint, Hitlist>();

            AddTracksFrom(TSOAudio);
        }

        /// <summary>
        /// Gets a track from a DBPF using its InstanceID.
        /// </summary>
        /// <param name="dbpf">The DBPF to search.</param>
        private void AddTracksFrom(DBPF dbpf)
        {
            var tracks = dbpf.GetItemsByType(DBPFTypeID.TRK);
            for (var i=0; i<tracks.Count; i++) 
            {
                TracksById.Add(tracks[i].Key, new Track(tracks[i].Value));
            }
        }

        /// <summary>
        /// Gets a audio file from a DBPF using its InstanceID.
        /// </summary>
        /// <param name="InstanceID">The InstanceID of the audio.</param>
        /// <param name="dbpf">The DBPF to search.</param>
        /// <returns>The audio as a stream of bytes.</returns>
        private byte[] GetAudioFrom(uint InstanceID, DBPF dbpf) 
        {
            if (InstanceID == 0)
                return null;

            //all game sfx has type id 0x2026960B
            byte[] dat = dbpf.GetItemByID((ulong)DBPFTypeID.SoundFX + (((ulong)InstanceID)<<32));

            if (dat != null)
            {
                string head = new string(new char[] { (char)dat[0], (char)dat[1], (char)dat[2], (char)dat[3] });
                if (head.StartsWith("XA"))
                    return new XAFile(dat).DecompressedData;
                else if (head.StartsWith("UTM0"))
                {
                    var utk = new UTKFile2(dat);
                    utk.UTKDecode();
                    return utk.DecompressedWav;
                }
                else
                    return dat; //either wav or mp3, bass.net can explicitly read these.
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
        private Hitlist GetHitlistFrom(uint InstanceID, DBPF dbpf)
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
        public Hitlist GetHitlist(uint InstanceID)
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
        /// Gets a sound effect from the sound effects cache.
        /// </summary>
        /// <param name="InstanceID">The InstanceID of the sound effect.</param>
        /// <returns>The sound effect as a GCHandle instance.</returns>
        public SoundEffect GetSFX(uint InstanceID)
        {
            if (SFXCache.ContainsKey(InstanceID)) return SFXCache[InstanceID];

            byte[] data = GetAudioFrom(InstanceID, TSOAudio);
            if (data == null) data = GetAudioFrom(InstanceID, tsov2);
            if (data == null) data = GetAudioFrom(InstanceID, Stings);
            if (data == null) data = GetAudioFrom(InstanceID, EP5Samps);
            if (data == null) data = GetAudioFrom(InstanceID, EP2);

            if (data != null)
            {
                var stream = new MemoryStream(data);
                var sfx = SoundEffect.FromStream(stream);
                stream.Close();
                SFXCache.Add(InstanceID, sfx);
                return sfx; //remember to clear the sfx cache between lots!
            }
            else
            {
                //GCHandle pinnedArray = GCHandle.Alloc(new byte[1], GCHandleType.Weak);
                return null;// pinnedArray; //we couldn't find anything! can't return null so do this... not the best idea tbh
            }
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
    }
}
