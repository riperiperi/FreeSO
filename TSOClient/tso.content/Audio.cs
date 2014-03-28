using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Content.framework;
using TSO.Content.model;
using System.Text.RegularExpressions;
using TSO.Common.content;
using System.IO;
using System.Runtime.InteropServices;
using TSO.Files.formats.dbpf;
using SimsLib.XA;
using SimsLib.UTK;
using TSO.Files.HIT;

namespace TSO.Content
{
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
        public Dictionary<uint, Track> TracksById;


        /** Audio Cache **/
        public Dictionary<uint, GCHandle> SFXCache;

        public Audio(Content contentManager)
        {
            this.ContentManager = contentManager;
        }

        public void Init()
        {
            this.Stations = new List<AudioReference>();
            this.StationsById = new Dictionary<uint, AudioReference>();
            this.Modes = new List<AudioReference>();

            var stationsRegEx = new Regex(@"music\\stations\\.*\.mp3");

            foreach (var file in ContentManager.AllFiles){
                if (stationsRegEx.IsMatch(file)){
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
            Stings = new DBPF(ContentManager.GetPath("tsov2.dat"));
            EP5Samps = new DBPF(ContentManager.GetPath("tsov2.dat"));
            EP2 = new DBPF(ContentManager.GetPath("tsov2.dat"));

            SFXCache = new Dictionary<uint, GCHandle>();
            TracksById = new Dictionary<uint, Track>();

            AddTracksFrom(TSOAudio);

        }

        private void AddTracksFrom(DBPF dbpf)
        {
            var tracks = dbpf.GetItemsByType(0x5D73A611);
            for (var i=0; i<tracks.Count; i++) {
                TracksById.Add(tracks[i].Key, new Track(tracks[i].Value));
            }
        }

        private byte[] GetAudioFrom(uint id, DBPF dbpf) {
            //all game sfx has type id 0x2026960B
            var dat = TSOAudio.GetItemByID((ulong)0x2026960B+(((ulong)id)<<32));
            if (dat != null)
            {
                string head = new string(new char[] { (char)dat[0], (char)dat[1], (char)dat[2], (char)dat[3] });
                if (head.StartsWith("XA"))
                    return new XAFile(dat).DecompressedData;
                else if (head.StartsWith("UTM0"))
                    return new UTKFile2(dat).DecompressedWav;
                else
                    return dat; //either wav or mp3, bass.net can explicitly read these.
            }
            return null;
        } 

        public GCHandle GetSFX(uint id)
        {
            if (SFXCache.ContainsKey(id)) return SFXCache[id];

            byte[] data = GetAudioFrom(id, TSOAudio);
            if (data == null) data = GetAudioFrom(id, tsov2);
            if (data == null) data = GetAudioFrom(id, Stings);
            if (data == null) data = GetAudioFrom(id, EP5Samps);
            if (data == null) data = GetAudioFrom(id, EP2);

            if (data != null)
            {
                GCHandle pinnedArray = GCHandle.Alloc(data, GCHandleType.Pinned);
                SFXCache.Add(id, pinnedArray);
                return pinnedArray; //remember to clear the sfx cache between lots!
            }
            else
            {
                GCHandle pinnedArray = GCHandle.Alloc(new byte[1], GCHandleType.Weak);
                return pinnedArray; //we couldn't find anything! can't return null so do this... not the best idea tbh
            }
        }

        public List<AudioReference> List()
        {
            var result = new List<AudioReference>();
            result.AddRange(Stations);
            return result;
        }
    }
}
