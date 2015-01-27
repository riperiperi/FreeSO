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
    /// Provides access to floor (*.flr) data in FAR3 archives.
    /// </summary>
    public class WorldFloorProvider : IContentProvider<Floor>
    {
        private Content ContentManager;
        private List<Floor> Floors;
        private Dictionary<ushort, Floor> ById;

        public WorldFloorProvider(Content contentManager)
        {
            this.ContentManager = contentManager;
        }

        /// <summary>
        /// Initiates loading of floors.
        /// </summary>
        public void Init()
        {
            /**
             * TODO: We can make this lazy load a bit better. Inside the far archives we 
             * could just keep far entry pointers rather than processing them. Assuming each file
             * in the far only contains 1 floor style. If its variable this may not be possible
             */

            this.ById = new Dictionary<ushort, Floor>();
            this.Floors = new List<Floor>();

            var floorGlobalsPath = ContentManager.GetPath("objectdata/globals/floors.iff");
            var floorGlobals = new Iff(floorGlobalsPath);

            /** There is a small handful of floors in a global file for some reason **/
            ushort floorID = 1;
            for (ushort i = 1; i < 30; i++)
            {
                var far = floorGlobals.Get<SPR2>(i);
                var medium = floorGlobals.Get<SPR2>((ushort)(i + 256));
                var near = floorGlobals.Get<SPR2>((ushort)(i + 512)); //2048 is water tile

                this.AddFloor(new Floor
                {
                    ID = floorID,
                    Far = far,
                    Medium = medium,
                    Near = near
                });
                floorID++;
            }

            floorID = 256;

            var archives = new string[]
            {
                "housedata/floors/floors.far",
                "housedata/floors2/floors2.far",
                "housedata/floors3/floors3.far",
                "housedata/floors4/floors4.far"
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

                    var far = iff.Get<SPR2>(1);
                    var medium = iff.Get<SPR2>(257);
                    var near = iff.Get<SPR2>(513);

                    AddFloor(new Floor {
                        ID = floorID,
                        Near = near,
                        Medium = medium,
                        Far = far
                    });
                    floorID++;
                }
            }
        }

        private void AddFloor(Floor floor)
        {
            Floors.Add(floor);
            ById.Add(floor.ID, floor);
        }

        #region IContentProvider<Floor> Members

        public Floor Get(ulong id)
        {
            if (ById.ContainsKey((ushort)id))
            {
                return ById[(ushort)id];
            }
            return null;
        }

        public Floor Get(uint type, uint fileID)
        {
            return null;
        }

        public List<IContentReference<Floor>> List()
        {
            return null;
        }

        #endregion
    }
}
