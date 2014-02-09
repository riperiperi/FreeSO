using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimsLib.FAR1;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using TSOClient.Code.Utils;
using TSOClient.Code.Rendering.Lot.Model;
using TSOClient.Code.Data.Model;

namespace TSOClient.Code.Data
{
    public class ArchitectureCatalog
    {
//        private static Dictionary<int, ArchivePointer> FloorPointers;
//        private static Dictionary<int, Iff> Floors = new Dictionary<int, Iff>();
//        private static Dictionary<int, FloorStyle> _Floors = new Dictionary<int, FloorStyle>();

//        private static Dictionary<int, WallStyle> _WallStyles = new Dictionary<int, WallStyle>();
//        private static Dictionary<int, WallStyle> _WallPatterns = new Dictionary<int, WallStyle>();

//        public static FloorStyle GetFloor(int id)
//        {
//            //if (!FloorPointers.ContainsKey(id))
//            //{
//            //    id = 1;
//            //}


//            if (_Floors.ContainsKey(id)) { return _Floors[id]; }
//            return null;
//        }

//        public static WallStyle GetWallPattern(int id)
//        {
//            if (_WallPatterns.ContainsKey(id)) { return _WallPatterns[id]; }
//            return null;
//        }


//        public static void Init()
//        {
//            /** Floors **/
//            var archives = new string[]{
//                "housedata/floors/floors.far",
//                "housedata/floors2/floors2.far",
//                "housedata/floors3/floors3.far",
//                "housedata/floors4/floors4.far"
//            };


//            var floorID = 1;

//            var floorGlobals = new Iff(GameFacade.GameFilePath("objectdata/globals/floors.iff"));
//            var buildGlobals = new Iff(GameFacade.GameFilePath("objectdata/globals/build.iff"));



//            for (var i = 1; i < 30; i++)
//            {
//                string spriteName = floorGlobals.SPR2s[i].Name;

//                var price = buildGlobals.StringTables[2].StringSets[0].Strings[(i - 1) * 3].Str;
//                var title = buildGlobals.StringTables[2].StringSets[0].Strings[(i - 1) * 3 + 1].Str;
//                var desc = buildGlobals.StringTables[2].StringSets[0].Strings[(i - 1) * 3 + 2].Str;

//                var newFloor = new FloorStyle
//                {
//                    ID = floorID,
//                    FarTexture = floorGlobals.SPR2s.First(x => x.ID == i).GetFrame(0),
//                    MediumTexture = floorGlobals.SPR2s.First(x => x.ID == i + 256).GetFrame(0),
//                    CloseTexture = floorGlobals.SPR2s.First(x => x.ID == i + 512).GetFrame(0),

//                    Name = title,
//                    Price = price,
//                    Description = desc
//                };

//                _Floors.Add(floorID, newFloor);
//                floorID++;
//            }

//            floorID = 256;

//            foreach (var archivePath in archives)
//            {
//                var archive = new FARArchive(GameFacade.GameFilePath(archivePath));
//                var entries = archive.GetAllFarEntries();

//                foreach (var item in entries)
//                {
//                    var iff = new Iff(archive.GetEntry(item));
//                    var catalogInfo = (iff.StringTables[0].StringSets.Count > 0) ? iff.StringTables[0].StringSets[0].Strings : iff.StringTables[0].Strings;


//                    var newFloor = new FloorStyle
//                    {
//                        ID = floorID,
//                        Name = catalogInfo[0].Str,
//                        Price = catalogInfo[1].Str,
//                        Description = catalogInfo[2].Str
//                    };

//                    newFloor.FarTexture = iff.SPR2s.First(x => x.ID == 1).GetFrame(0);
//                    newFloor.MediumTexture = iff.SPR2s.First(x => x.ID == 257).GetFrame(0);
//                    newFloor.CloseTexture = iff.SPR2s.First(x => x.ID == 513).GetFrame(0);

//                    _Floors.Add(floorID, newFloor);
//                    floorID++;
//                }
//            }


//            /** Hacks **/
//            GetFloor(10).AddAltView(HouseRotation.Angle180, GetFloor(11));
//            GetFloor(10).AddAltView(HouseRotation.Angle360, GetFloor(11));




//            ///** Walls **/
//            //var wallGlobals = new Iff(GameFacade.GameFilePath("objectdata/globals/walls.iff"));


//            ///** Wall Styles **/
//            //var wallID = 0;
//            //for (var i = 2; i <= 35; i++)
//            //{
//            //    var farZoom = wallGlobals.SPRs.FirstOrDefault(x => x.ID == i);
//            //    if (farZoom == null) { continue; }

//            //    var medZoom = wallGlobals.SPRs.First(x => x.ID == i + 512);
//            //    var closeZoom = wallGlobals.SPRs.First(x => x.ID == i + 1024);

//            //    var style = new WallStyle {
//            //        ID = wallID,

//            //        SpriteClose = closeZoom,
//            //        SpriteMedium = medZoom,
//            //        SpriteFar = farZoom
//            //    };
//            //    _WallStyles.Add(wallID, style);
//            //    wallID++;
//            //}

//            ///** Wall Patterns **/
//            //wallID = 0;
//            //for (var i = 0; i < 30; i++)
//            //{
//            //    var farZoom = wallGlobals.SPRs.FirstOrDefault(x => x.ID == 1536 + i);
//            //    if (farZoom == null) { continue; }

//            //    var medZoom = wallGlobals.SPRs.First(x => x.ID == i + 1792);
//            //    var closeZoom = wallGlobals.SPRs.First(x => x.ID == i + 2048);

//            //    var style = new WallStyle
//            //    {
//            //        ID = wallID,

//            //        SpriteClose = closeZoom,
//            //        SpriteMedium = medZoom,
//            //        SpriteFar = farZoom
//            //    };
//            //    _WallPatterns.Add(wallID, style);
//            //    wallID++;
//            //}


//            //wallID = 256;
//            //var wallArchives = new string[]{
//            //    "housedata/walls/walls.far",
//            //    "housedata/walls2/walls2.far",
//            //    "housedata/walls3/walls3.far",
//            //    "housedata/walls4/walls4.far"
//            //};
//            //foreach (var archivePath in wallArchives)
//            //{
//            //    var archive = new FARArchive(GameFacade.GameFilePath(archivePath));
//            //    var entries = archive.GetAllFarEntries();

//            //    foreach (var item in entries)
//            //    {
//            //        var iff = new Iff(archive.GetEntry(item));
//            //        var catalogInfo = (iff.StringTables[0].StringSets.Count > 0) ? iff.StringTables[0].StringSets[0].Strings : iff.StringTables[0].Strings;


//            //        var farZoom = iff.SPRs.FirstOrDefault(x => x.ID == 1);
//            //        if (farZoom == null) { continue; }

//            //        var medZoom = iff.SPRs.First(x => x.ID == 1793);
//            //        var closeZoom = iff.SPRs.First(x => x.ID == 2049);

//            //        var style = new WallStyle
//            //        {
//            //            ID = wallID,

//            //            SpriteClose = closeZoom,
//            //            SpriteMedium = medZoom,
//            //            SpriteFar = farZoom
//            //        };
//            //        _WallPatterns.Add(wallID, style);
//            //        wallID++;
//            //    }
//            //}
            
//            //using(var export = new HTMLPrinter(@"E:\Development\PDExport", "floorPatterns"))
//            //{
//            //    export.H1("Floor Patterns");

//            //    export.AddDataTable(_Floors.Values)
//            //            .WithColumn("ID", x => x.ID)
//            //            .WithColumn("Info", x => export.CreateTable()
//            //                                        .AddRow("Name", x.Name)
//            //                                        .AddRow("Price", x.Price)
//            //                                        .AddRow("Description", x.Description))

//            //            .WithColumn("Assets", x => export.CreateTable()
//            //                                        .AddRow(x.CloseTexture)
//            //                                        .AddRow(x.MediumTexture)
//            //                                        .AddRow(x.FarTexture));
//            //}


//            //using (var export = new HTMLPrinter(@"E:\Development\PDExport", "wallSyles"))
//            //{
//            //    export.H1("Wall Styles");

//            //    export.AddDataTable(_WallStyles.Values)
//            //            .WithColumn("ID", x => x.ID)
//            //            //.WithColumn("Info", x => export.CreateTable()
//            //            //                            .AddRow("Name", x.Name)
//            //            //                            .AddRow("Price", x.Price)
//            //            //                            .AddRow("Description", x.Description))

//            //            .WithColumn("Assets", x => export.CreateTable()
//            //                                        .AddRow(x.SpriteClose)
//            //                                        .AddRow(x.SpriteMedium)
//            //                                        .AddRow(x.SpriteFar));
//            //}


//            //using (var export = new HTMLPrinter(@"E:\Development\PDExport", "wallPatterns"))
//            //{
//            //    export.H1("Wall Patterns");

//            //    export.AddDataTable(_WallPatterns.Values)
//            //            .WithColumn("ID", x => x.ID)
//            //        //.WithColumn("Info", x => export.CreateTable()
//            //        //                            .AddRow("Name", x.Name)
//            //        //                            .AddRow("Price", x.Price)
//            //        //                            .AddRow("Description", x.Description))

//            //            .WithColumn("Assets", x => export.CreateTable()
//            //                                        .AddRow(x.SpriteClose)
//            //                                        .AddRow(x.SpriteMedium)
//            //                                        .AddRow(x.SpriteFar));
//            //}
            

//            /** Export **/
//            //var sb = new StringBuilder();
//            //foreach (var floor in _Floors)
//            //{
//            //    sb.Append("<h1>" + floor.Key + "</h1>");
//            //    var f = floor.Value;
//            //    sb.Append("<h2>" + f.Name + "</h2>");
//            //    sb.Append("<img src='" + floor.Key + ".png'></img>");
//            //    sb.Append("<br />");


//            //    var frame = f.Near;
//            //    var bmp = new System.Drawing.Bitmap(frame.Width, frame.Height);
//            //    for (var y = 0; y < frame.Height; y++)
//            //    {
//            //        for (var x = 0; x < frame.Width; x++)
//            //        {
//            //            bmp.SetPixel(x, y, frame.BitmapData.GetPixel(new System.Drawing.Point(x, y)));
//            //        }
//            //    }

//            //    var format = System.Drawing.Imaging.ImageFormat.Png;
//            //    bmp.Save(@"C:\Users\Darren\Desktop\xpo\foutput\" + floor.Key + ".png", format);
//            //    bmp.Dispose();
//            //}

//            ////var y = true;
//            //File.WriteAllText(@"C:\Users\Darren\Desktop\xpo\foutput\index.html", sb.ToString());




////            for (int i = 1; i < 30; i++)
////-            {
////-                Bitmap[] frames = new Bitmap[3];
////-
////-                string spriteName = floors5.SPR2s[i].Name;
////-                frames[0] = floors5.SPR2s.Find(delegate(SPR2Parser sp) { return sp.ID == i; }).GetFrame(0).BitmapData.BitMap;
////-                frames[1] = floors5.SPR2s.Find(delegate(SPR2Parser sp) { return sp.ID == i + 256; }).GetFrame(0).BitmapData.BitMap;
////-                frames[2] = floors5.SPR2s.Find(delegate(SPR2Parser sp) { return sp.ID == i + 512; }).GetFrame(0).BitmapData.BitMap;
////-
////-                string price = floors5names.StringTables[2].StringSets[0].Strings[(i - 1) * 3].Str;
////-                string title = floors5names.StringTables[2].StringSets[0].Strings[(i - 1) * 3 + 1].Str;
////-                string description = floors5names.StringTables[2].StringSets[0].Strings[(i - 1) * 3 + 2].Str;
////-
////-                m_Floors.Add(new Floor(title, price, description, frames, gd, spriteName));
////-            }






//            ///** Floors **/
            

//            //FloorPointers = new Dictionary<int, ArchivePointer>();
//            //var floorIndex = 0;

//            //var globals = new Iff(GameFacade.GameFilePath("objectdata/globals/floors.iff"));
//            //foreach (var spr2 in globals.SPR2s)
//            //{

//            //}


//            //foreach (var archivePath in archives)
//            //{
//            //    var archive = new FARArchive(GameFacade.GameFilePath(archivePath));
//            //    var entries = archive.GetAllFarEntries();

//            //    //var index = 1;
//            //    foreach (var item in entries)
//            //    {
//            //        FloorPointers.Add(floorIndex, new ArchivePointer {
//            //            Archive = archive,
//            //            Entry = item
//            //        });
//            //        floorIndex++;
//            //    }

//            //    //floorIndex += 100;
//            //}
//        }
    }

//    public class ArchivePointer {
//        public FARArchive Archive;
//        public FarEntry Entry;
//    }

}
