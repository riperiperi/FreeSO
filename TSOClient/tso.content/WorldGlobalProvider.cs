using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.content.framework;
using tso.common.content;
using System.Xml;
using tso.content.codecs;
using System.Text.RegularExpressions;
using System.IO;
using tso.files.formats.iff;
using tso.files.formats.iff.chunks;
using tso.files.formats.otf;

namespace tso.content
{
    public class WorldGlobalProvider
    {
        private Dictionary<string, GameGlobal> Cache; //indexed by lowercase filename, minus directory and extension.
        private Content ContentManager;

        public WorldGlobalProvider(Content contentManager)
        {
            this.ContentManager = contentManager;
        }

        public void Init()
        {
            Cache = new Dictionary<string, GameGlobal>();
        }

        public GameGlobal Get(string filename)
        {
            lock (Cache)
            {
                if (Cache.ContainsKey(filename))
                {
                    return Cache[filename];
                }

                var iff = new Iff(Path.Combine(Content.Get().BasePath, "objectdata\\globals\\" + filename + ".iff")); //if we can't load this let it throw an exception...
                                                                                                                      //probably sanity check this when we add user objects.
                OTF otf = null;
                try
                {
                    otf = new OTF(Path.Combine(Content.Get().BasePath, "objectdata\\globals\\" + filename + ".otf"));
                }
                catch (IOException e)
                {
                    //if we can't load an otf, it probably doesn't exist.
                }
                var resource = new GameGlobalResource(iff, otf);

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

    public class GameGlobalResource : GameIffResource
    {
        public Iff Iff;
        public OTF Tuning;

        public GameGlobalResource(Iff iff, OTF tuning)
        {
            this.Iff = iff;
            this.Tuning = tuning;
        }

        public override T Get<T>(ushort id)
        {
            var type = typeof(T);
            if (type == typeof(OTFTable))
            {
                if (Tuning != null)
                {
                    return (T)(object)Tuning.GetTable(id);
                }
            }

            T item1 = this.Iff.Get<T>(id);
            if (item1 != null)
            {
                return item1;
            }

            return default(T);
        }

        public override List<T> List<T>()
        {
            return this.Iff.List<T>();
        }
    }
}
