/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Content.Framework;
using FSO.Common.Content;
using System.Xml;
using FSO.Content.Codecs;
using System.Text.RegularExpressions;
using System.IO;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using FSO.Files.Formats.OTF;
using FSO.Files.FAR1;

namespace FSO.Content
{
    /// <summary>
    /// Provides access to global (*.otf, *.iff) data in FAR3 archives.
    /// </summary>
    public class WorldGlobalProvider
    {
        private Dictionary<string, GameGlobal> Cache; //indexed by lowercase filename, minus directory and extension.
        private Content ContentManager;
        private FAR1Archive TS1Provider;

        public WorldGlobalProvider(Content contentManager)
        {
            this.ContentManager = contentManager;
        }

        /// <summary>
        /// Creates a new cache for loading of globals.
        /// </summary>
        public void Init()
        {
            Cache = new Dictionary<string, GameGlobal>();
            if (ContentManager.TS1)
            {
                TS1Provider = new FAR1Archive(Path.Combine(ContentManager.TS1BasePath, "GameData/Global/Global.far"), false);
            }
        }

        /// <summary>
        /// Gets a resource.
        /// </summary>
        /// <param name="filename">The filename of the resource to get.</param>
        /// <returns>A GameGlobal instance containing the resource.</returns>
        public GameGlobal Get(string filename)
        {
            filename = filename.ToLowerInvariant();
            lock (Cache)
            {
                if (Cache.ContainsKey(filename))
                {
                    return Cache[filename];
                }

                //if we can't load this let it throw an exception...
                //probably sanity check this when we add user objects.

                GameGlobalResource resource = null;

                if (TS1Provider != null)
                {
                    var data = TS1Provider.GetEntry(
                        TS1Provider.GetAllEntries().FirstOrDefault(x => x.Key.ToLowerInvariant() == (filename + ".iff").ToLowerInvariant()));

                    if (data != null)
                    {
                        using (var stream = new MemoryStream(data))
                        {
                            var iff = new IffFile();
                            iff.Read(stream);
                            iff.InitHash();
                            iff.SetFilename(filename + ".iff");
                            resource = new GameGlobalResource(iff, null);
                        }
                    }
                    
                }
                else
                { 
                    var iff = new IffFile(Path.Combine(Content.Get().BasePath, "objectdata/globals/" + filename + ".iff"));
                    iff.InitHash();
                    OTFFile otf = null;
                    try
                    {
                        var rewrite = PIFFRegistry.GetOTFRewrite(filename + ".otf");
                        otf = new OTFFile(rewrite ?? Path.Combine(Content.Get().BasePath, ("objectdata/globals/" + filename + ".otf")));
                    }
                    catch (IOException)
                    {
                        //if we can't load an otf, it probably doesn't exist.
                    }
                    resource = new GameGlobalResource(iff, otf);
                }

                var item = new GameGlobal
                {
                    Resource = resource
                };

                Cache.Add(filename, item);

                return item;
            }
        }
    }

    public class GameGlobal
    {
        public GameGlobalResource Resource;
    }

    /// <summary>
    /// A global can be an OTF (Object Tuning File) or an IFF.
    /// </summary>
    public class GameGlobalResource : GameIffResource
    {
        public IffFile Iff;
        public OTFFile Tuning;

        public override IffFile MainIff
        {
            get { return Iff; }
        }

        public GameGlobalResource(IffFile iff, OTFFile tuning)
        {
            this.Iff = iff;
            this.Tuning = tuning;
            Recache();
        }

        public override void Recache()
        {
            base.Recache();
            if (Tuning != null)
            {
                foreach (var table in Tuning.Tables)
                {
                    if (table == null) continue;
                    foreach (var item in table.Keys)
                    {
                        if (item == null) continue;
                        TuningCache[((uint)table.ID << 16) | (uint)(item.ID)] = (short)item.Value;
                    }
                }
            }
        }

        public override T Get<T>(ushort id)
        {
            var type = typeof(T);

            T item1 = this.Iff.Get<T>(id);
            if (item1 != null)
            {
                return item1;
            }

            if (type == typeof(OTFTable))
            {
                if (Tuning != null)
                {
                    return (T)(object)Tuning.GetTable(id);
                }
            }

            return default(T);
        }

        public override List<T> List<T>()
        {
            return this.Iff.List<T>();
        }
    }
}
