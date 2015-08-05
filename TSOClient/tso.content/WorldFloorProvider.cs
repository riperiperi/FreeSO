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
using FSO.Content.Framework;
using System.Text.RegularExpressions;
using FSO.Content.Codecs;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Content
{
    /// <summary>
    /// Provides access to floor (*.flr) data in FAR3 archives.
    /// </summary>
    public class WorldFloorProvider : IContentProvider<Floor>
    {
        private Content ContentManager;
        private Dictionary<ushort, Floor> ById;

        public Dictionary<ushort, FloorReference> Entries;
        public FAR1Provider<IffFile> Floors;

        private IffFile FloorGlobals;
        public int NumFloors;

        public WorldFloorProvider(Content contentManager)
        {
            this.ContentManager = contentManager;
        }

        /// <summary>
        /// Initiates loading of floors.
        /// </summary>
        public void Init()
        {

            this.Entries = new Dictionary<ushort, FloorReference>();
            this.ById = new Dictionary<ushort, Floor>();

            var floorGlobalsPath = ContentManager.GetPath("objectdata/globals/floors.iff");
            var floorGlobals = new IffFile(floorGlobalsPath);
            FloorGlobals = floorGlobals;

            var buildGlobalsPath = ContentManager.GetPath("objectdata/globals/build.iff");
            var buildGlobals = new IffFile(buildGlobalsPath); //todo: centralize?

            /** There is a small handful of floors in a global file for some reason **/
            ushort floorID = 1;
            var floorStrs = buildGlobals.Get<STR>(0x83);
            for (ushort i = 1; i < (floorStrs.Length/3); i++)
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

                Entries.Add(floorID, new FloorReference(this)
                {
                    ID = floorID,
                    FileName = "global",

                    Name = floorStrs.GetString((i - 1) * 3 + 1),
                    Price = int.Parse(floorStrs.GetString((i - 1) * 3 + 0)),
                    Description = floorStrs.GetString((i - 1) * 3 + 2)
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
                    var iff = new IffFile();
                    var bytes = archive.GetEntry(entry);
                    using(var stream = new MemoryStream(bytes))
                    {
                        iff.Read(stream);
                    }


                    var catStrings = iff.Get<STR>(0);

                    Entries.Add(floorID, new FloorReference(this)
                    {
                        ID = floorID,
                        FileName = entry.Key,

                        Name = catStrings.GetString(0),
                        Price = int.Parse(catStrings.GetString(1)),
                        Description = catStrings.GetString(2)
                    });

                    floorID++;
                }
                archive.Close();
            }
            NumFloors = floorID;
            this.Floors = new FAR1Provider<IffFile>(ContentManager, new IffCodec(), new Regex(".*\\\\floors.*\\.far"));
            Floors.Init();
        }

        private void AddFloor(Floor floor)
        {
            ById.Add(floor.ID, floor);
        }


        public Texture2D GetFloorThumb(ushort id, GraphicsDevice device)
        {
            if (id < 256)
            {
                return ById[id].Near.Frames[0].GetTexture(device);
            }
            else return this.Floors.ThrowawayGet(Entries[(ushort)id].FileName).Get<SPR2>(513).Frames[0].GetTexture(device);
        }

        #region IContentProvider<Floor> Members

        public Floor Get(ulong id)
        {
            if (ById.ContainsKey((ushort)id))
            {
                return ById[(ushort)id];
            }
            else
            {
                //get from iff
                if (!Entries.ContainsKey((ushort)id)) return null;
                IffFile iff = this.Floors.Get(Entries[(ushort)id].FileName);
                if (iff == null) return null;

                var far = iff.Get<SPR2>(1);
                var medium = iff.Get<SPR2>(257);
                var near = iff.Get<SPR2>(513);

                ById[(ushort)id] = new Floor
                {
                    ID = (ushort)id,
                    Near = near,
                    Medium = medium,
                    Far = far
                };
                return ById[(ushort)id];
            }
        }

        public Floor Get(uint type, uint fileID)
        {
            return null;
        }

        public List<IContentReference<Floor>> List()
        {
            return new List<IContentReference<Floor>>(Entries.Values);
        }

        #endregion
    }

    public class FloorReference : IContentReference<Floor>
    {
        public ulong ID;
        public string FileName;

        public int Price; //remember these, just in place of a catalog
        public string Name;
        public string Description;

        private WorldFloorProvider Provider;

        public FloorReference(WorldFloorProvider provider)
        {
            this.Provider = provider;
        }

        #region IContentReference<Floor> Members

        public Floor Get()
        {
            return Provider.Get(ID);
        }

        #endregion
    }
}
