/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Common.Content;
using FSO.Content.Model;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using FSO.Files.FAR1;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using FSO.Content.Framework;
using FSO.Content.Codecs;
using System.Text.RegularExpressions;

namespace FSO.Content
{
    /// <summary>
    /// Provides access to wall (*.wll) data in FAR3 archives.
    /// </summary>
    public class WorldWallProvider : IContentProvider<Wall>
    {
        private Content ContentManager;
        public Wall Junctions;
        private List<WallStyle> WallStyles;
        private Dictionary<ushort, Wall> ById;
        private Dictionary<ushort, WallStyle> StyleById;
        private IffFile WallGlobals;

        public Dictionary<ushort, WallReference> Entries;

        public FAR1Provider<IffFile> Walls;

        public int NumWalls;

        public WorldWallProvider(Content contentManager)
        {
            this.ContentManager = contentManager;
        }

        /// <summary>
        /// Initiates loading of walls.
        /// </summary>
        public void Init()
        {

            this.Entries = new Dictionary<ushort, WallReference>();
            this.ById = new Dictionary<ushort, Wall>();
            this.StyleById = new Dictionary<ushort, WallStyle>();
            this.WallStyles = new List<WallStyle>();

            var wallGlobalsPath = ContentManager.GetPath("objectdata/globals/walls.iff");
            WallGlobals = new IffFile(wallGlobalsPath);

            var buildGlobalsPath = ContentManager.GetPath("objectdata/globals/build.iff");
            var buildGlobals = new IffFile(buildGlobalsPath); //todo: centralize?

            /** Get wall styles from globals file **/
            ushort wallID = 1;
            for (ushort i = 2; i < 512; i+=2)
            {
                var far = WallGlobals.Get<SPR>((ushort)(i));
                var medium = WallGlobals.Get<SPR>((ushort)(i + 512));
                var near = WallGlobals.Get<SPR>((ushort)(i + 1024));

                var fard = WallGlobals.Get<SPR>((ushort)(i + 1));
                var mediumd = WallGlobals.Get<SPR>((ushort)(i + 513));
                var neard = WallGlobals.Get<SPR>((ushort)(i + 1025));

                if (fard == null)
                { //no walls down, just render exactly the same
                    fard = far;
                    mediumd = medium;
                    neard = near;
                }

                this.AddWallStyle(new WallStyle
                {
                    ID = wallID,
                    WallsUpFar = far,
                    WallsUpMedium = medium,
                    WallsUpNear = near,
                    WallsDownFar = fard,
                    WallsDownMedium = mediumd,
                    WallsDownNear = neard
                });

                wallID++;
            }

            DynamicStyleID = 256; //styles loaded from objects start at 256. The objd reference is dynamically altered to reference this new id, 
            //so only refresh wall cache at same time as obj cache! (do this on lot unload)

            /** Get wall patterns from globals file **/
            var wallStrs = buildGlobals.Get<STR>(0x83);

            wallID = 0;
            for (ushort i = 0; i < 256; i++)
            {
                var far = WallGlobals.Get<SPR>((ushort)(i+1536));
                var medium = WallGlobals.Get<SPR>((ushort)(i + 1536 + 256));
                var near = WallGlobals.Get<SPR>((ushort)(i + 1536 + 512));

                this.AddWall(new Wall
                {
                    ID = wallID,
                    Far = far,
                    Medium = medium,
                    Near = near,
                });

                if (i > 0 && i < (wallStrs.Length / 3) + 1)
                {
                    Entries.Add(wallID, new WallReference(this)
                    {
                        ID = wallID,
                        FileName = "global",

                        Name = wallStrs.GetString((i-1)*3+1),
                        Price = int.Parse(wallStrs.GetString((i - 1) * 3 + 0)),
                        Description = wallStrs.GetString((i - 1) * 3 + 2)
                    });
                }

                wallID++;
            }

            Junctions = new Wall
                {
                    ID = wallID,
                    Far = WallGlobals.Get<SPR>(4096),
                    Medium = WallGlobals.Get<SPR>(4097),
                    Near = WallGlobals.Get<SPR>(4098),
                };

            wallID = 256;

            var archives = new string[]
            {
                "housedata/walls/walls.far",
                "housedata/walls2/walls2.far",
                "housedata/walls3/walls3.far",
                "housedata/walls4/walls4.far"
            };

            for (var i = 0; i < archives.Length; i++)
            {
                var archivePath = ContentManager.GetPath(archives[i]);
                var archive = new FAR1Archive(archivePath);
                var entries = archive.GetAllEntries();

                foreach (var entry in entries)
                {

                    var iff = new IffFile();
                    var bytes = archive.GetEntry(entry);
                    using(var stream = new MemoryStream(bytes))
                    {
                        iff.Read(stream);
                    }

                    var catStrings = iff.Get<STR>(0);

                    Entries.Add(wallID, new WallReference(this)
                    {
                        ID = wallID,
                        FileName = entry.Key,

                        Name = catStrings.GetString(0),
                        Price = int.Parse(catStrings.GetString(1)),
                        Description = catStrings.GetString(2)
                    });

                    wallID++;
                }
                archive.Close();
            }

            this.Walls = new FAR1Provider<IffFile>(ContentManager, new IffCodec(), new Regex(".*/walls.*\\.far"));
            Walls.Init();
            NumWalls = wallID;
        }

        private ushort DynamicStyleID;


        /// <summary>
        /// Adds a dynamic wall style to WorldWallProvider.
        /// </summary>
        /// <param name="input">Wallstyle to add.</param>
        /// <returns>The ID of the wallstyle.</returns>
        public ushort AddDynamicWallStyle(WallStyle input) //adds a new wall and returns its id
        {
            input.ID = DynamicStyleID++;
            AddWallStyle(input);
            return (ushort)(DynamicStyleID - 1);
        }

        private void AddWall(Wall wall)
        {
            //Walls.Add(wall);
            ById.Add(wall.ID, wall);
        }

        private void AddWallStyle(WallStyle wall)
        {
            WallStyles.Add(wall);
            StyleById.Add(wall.ID, wall);
        }

        /// <summary>
        /// Gets a wallstyle instance from WorldWallProvider.
        /// </summary>
        /// <param name="id">The ID of the wallstyle.</param>
        /// <returns>A WallStyle instance.</returns>
        public WallStyle GetWallStyle(ulong id)
        {
            if (StyleById.ContainsKey((ushort)id))
            {
                return StyleById[(ushort)id];
            }
            return null;
        }

        public Texture2D GetWallThumb(ushort id, GraphicsDevice device)
        {
            if (id < 256)
            {
                var spr = ById[id].Medium;
                return (spr == null)?null:spr.Frames[2].GetTexture(device);
            }
            else
            {
                var iff = this.Walls.ThrowawayGet(Entries[(ushort)id].FileName);
                var spr = iff.Get<SPR>(1793);
                return (spr == null)?null:spr.Frames[2].GetTexture(device);
            }
        }


        public BMP GetWallStyleIcon(ushort id)
        {
            return WallGlobals.Get<BMP>(id);
        }

        #region IContentProvider<Wall> Members

        public Wall Get(ulong id)
        {
            if (ById.ContainsKey((ushort)id))
            {
                return ById[(ushort)id];
            }
            else
            {
                //get from iff
                IffFile iff = this.Walls.Get(Entries[(ushort)id].FileName);
                if (iff == null) return null;

                var far = iff.Get<SPR>(1);
                var medium = iff.Get<SPR>(1793);
                var near = iff.Get<SPR>(2049);

                ById[(ushort)id] = new Wall
                {
                    ID = (ushort)id,
                    Near = near,
                    Medium = medium,
                    Far = far
                };
                return ById[(ushort)id];
            }
        }

        public Wall Get(uint type, uint fileID)
        {
            return null;
        }

        public List<IContentReference<Wall>> List()
        {
            return new List<IContentReference<Wall>>(Entries.Values);
        }

        #endregion
    }

    public class WallReference : IContentReference<Wall>
    {
        public ulong ID;
        public string FileName;

        public int Price; //remember these, just in place of a catalog
        public string Name;
        public string Description;

        private WorldWallProvider Provider;

        public WallReference(WorldWallProvider provider)
        {
            this.Provider = provider;
        }

        #region IContentReference<Wall> Members

        public Wall Get()
        {
            return Provider.Get(ID);
        }

        public object GetGeneric()
        {
            return Get();
        }

        #endregion
    }
}
