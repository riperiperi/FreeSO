using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Common.content;
using TSO.Content.model;
using TSO.Files.formats.iff;
using TSO.Files.formats.iff.chunks;
using TSO.Files.FAR1;
using System.IO;

namespace TSO.Content
{
    /// <summary>
    /// Provides access to wall (*.wll) data in FAR3 archives.
    /// </summary>
    public class WorldWallProvider : IContentProvider<Wall>
    {
        private Content ContentManager;
        private List<Wall> Walls;
        public Wall Junctions;
        private List<WallStyle> WallStyles;
        private Dictionary<ushort, Wall> ById;
        private Dictionary<ushort, WallStyle> StyleById;

        public WorldWallProvider(Content contentManager)
        {
            this.ContentManager = contentManager;
        }

        /// <summary>
        /// Initiates loading of walls.
        /// </summary>
        public void Init()
        {
            /**
             * See floor for suggestions for implementation that doesn't load everything.
             */

            this.ById = new Dictionary<ushort, Wall>();
            this.Walls = new List<Wall>();
            this.StyleById = new Dictionary<ushort, WallStyle>();
            this.WallStyles = new List<WallStyle>();

            var wallGlobalsPath = ContentManager.GetPath("objectdata/globals/walls.iff");
            var wallGlobals = new Iff(wallGlobalsPath);

            /** Get wall styles from globals file **/
            ushort wallID = 1;
            for (ushort i = 2; i < 512; i+=2)
            {
                var far = wallGlobals.Get<SPR>((ushort)(i));
                var medium = wallGlobals.Get<SPR>((ushort)(i + 512));
                var near = wallGlobals.Get<SPR>((ushort)(i + 1024));

                var fard = wallGlobals.Get<SPR>((ushort)(i + 1));
                var mediumd = wallGlobals.Get<SPR>((ushort)(i + 513));
                var neard = wallGlobals.Get<SPR>((ushort)(i + 1025));

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

            wallID = 0;
            for (ushort i = 0; i < 256; i++)
            {
                var far = wallGlobals.Get<SPR>((ushort)(i+1536));
                var medium = wallGlobals.Get<SPR>((ushort)(i + 1536 + 256));
                var near = wallGlobals.Get<SPR>((ushort)(i + 1536 + 512));

                this.AddWall(new Wall
                {
                    ID = wallID,
                    Far = far,
                    Medium = medium,
                    Near = near,
                });

                wallID++;
            }

            Junctions = new Wall
                {
                    ID = wallID,
                    Far = wallGlobals.Get<SPR>(4096),
                    Medium = wallGlobals.Get<SPR>(4097),
                    Near = wallGlobals.Get<SPR>(4098),
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
                    var iff = new Iff();
                    var bytes = archive.GetEntry(entry);
                    using(var stream = new MemoryStream(bytes))
                    {
                        iff.Read(stream);
                    }

                    var far = iff.Get<SPR>(1);
                    var medium = iff.Get<SPR>(1793);
                    var near = iff.Get<SPR>(2049);

                    AddWall(new Wall {
                        ID = wallID,
                        Near = near,
                        Medium = medium,
                        Far = far
                    });
                    wallID++;
                }
            }
        }

        private ushort DynamicStyleID;

        public ushort AddDynamicWallStyle(WallStyle input) //adds a new wall and returns its id
        {
            input.ID = DynamicStyleID++;
            AddWallStyle(input);
            return (ushort)(DynamicStyleID - 1);
        }

        private void AddWall(Wall wall)
        {
            Walls.Add(wall);
            ById.Add(wall.ID, wall);
        }

        private void AddWallStyle(WallStyle wall)
        {
            WallStyles.Add(wall);
            StyleById.Add(wall.ID, wall);
        }

        public WallStyle GetWallStyle(ulong id)
        {
            if (StyleById.ContainsKey((ushort)id))
            {
                return StyleById[(ushort)id];
            }
            return null;
        }

        #region IContentProvider<Floor> Members

        public Wall Get(ulong id)
        {
            if (ById.ContainsKey((ushort)id))
            {
                return ById[(ushort)id];
            }
            return null;
        }

        public Wall Get(uint type, uint fileID)
        {
            return null;
        }

        public List<IContentReference<Wall>> List()
        {
            return null;
        }

        #endregion
    }
}
