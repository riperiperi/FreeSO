using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.content.framework;
using tso.content.model;
using System.Text.RegularExpressions;
using tso.common.content;
using System.IO;
using tso.files.formats.dbpf;

namespace tso.content
{
    public class Audio
    {
        private Content ContentManager;

        /** Stations **/
        private List<AudioReference> Stations;
        private Dictionary<uint, AudioReference> StationsById;
        private List<AudioReference> Modes;

        /** TSOAudio.dat **/


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

            var tsoAudio = new DBPF(ContentManager.GetPath("TSOAudio.dat"));

        }

        public List<AudioReference> List()
        {
            var result = new List<AudioReference>();
            result.AddRange(Stations);
            return result;
        }
    }
}
