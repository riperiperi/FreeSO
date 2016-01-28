/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Client.UI.Framework;
using System.Xml;
using System.IO;
using FSO.Content;
using Microsoft.Xna.Framework.Graphics;
using FSO.Files.Formats.IFF.Chunks;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Panels.LotControls;

namespace FSO.Client.UI.Controls.Catalog
{
    public class UICatalog : UIContainer
    {
        private int Page;
        private static List<UICatalogElement>[] _Catalog;
        public event CatalogSelectionChangeDelegate OnSelectionChange;

        public static List<UICatalogElement>[] Catalog {
            get
            {
                if (_Catalog != null) return _Catalog;
                else
                {
                    //load and build catalog
                    _Catalog = new List<UICatalogElement>[30];
                    for (int i = 0; i < 30; i++) _Catalog[i] = new List<UICatalogElement>();

                    var packingslip = new XmlDocument();
                    
                    packingslip.Load(Path.Combine(GlobalSettings.Default.StartupPath, "packingslips\\catalog.xml"));
                    var objectInfos = packingslip.GetElementsByTagName("P");

                    foreach (XmlNode objectInfo in objectInfos)
                    {
                        sbyte Category = Convert.ToSByte(objectInfo.Attributes["s"].Value);
                        if (Category < 0) continue;
                        _Catalog[Category].Add(new UICatalogElement()
                        {
                            GUID = Convert.ToUInt32(objectInfo.Attributes["g"].Value, 16),
                            Category = Category,
                            Price = Convert.ToUInt32(objectInfo.Attributes["p"].Value),
                            Name = objectInfo.Attributes["n"].Value
                        });
                    }

                    AddWallpapers();
                    AddFloors();

                    for (int i = 0; i < 30; i++) _Catalog[i].Sort(new CatalogSorter());

                    AddWallStyles();

                    return _Catalog;
                }
            }
        }

        private static void AddWallpapers()
        {
            var res = new UICatalogWallpaperResProvider();

            var walls = Content.Content.Get().WorldWalls.List();

            for (int i = 0; i < walls.Count; i++)
            {
                var wall = (WallReference)walls[i];
                _Catalog[8].Insert(0, new UICatalogElement
                {
                    Name = wall.Name,
                    Category = 8,
                    Price = (uint)wall.Price,
                    Special = new UISpecialCatalogElement
                    {
                        Control = typeof(UIWallPainter),
                        ResID = wall.ID,
                        Res = res,
                        Parameters = new List<int> { (int)wall.ID } //pattern
                    }
                });
            }
        }

        private static void AddFloors()
        {
            var res = new UICatalogFloorResProvider();

            var floors = Content.Content.Get().WorldFloors.List();

            for (int i = 0; i < floors.Count; i++)
            {
                var floor = (FloorReference)floors[i];
                _Catalog[9].Insert(0, new UICatalogElement
                {
                    Name = floor.Name,
                    Category = 9,
                    Price = (uint)floor.Price,
                    Special = new UISpecialCatalogElement
                    {
                        Control = typeof(UIFloorPainter),
                        ResID = floor.ID,
                        Res = res,
                        Parameters = new List<int> { (int)floor.ID } //pattern
                    }
                });
            }

            _Catalog[5].Insert(0, new UICatalogElement
            {
                Name = "Pool",
                Category = 5,
                Price = (uint)0,
                Special = new UISpecialCatalogElement
                {
                    Control = typeof(UIFloorPainter),
                    ResID = 65535,
                    Res = res,
                    Parameters = new List<int> { (int)65535 } //pattern
                }
            });
        }

        private static void AddWallStyles()
        {
            var res = new UICatalogWallResProvider();

            for (int i = 0; i < WallStyleIDs.Length; i++)
            {
                _Catalog[7].Insert(0, new UICatalogElement
                {
                    Name = "Wall",
                    Category = 7,
                    Price = 0,
                    Special = new UISpecialCatalogElement
                    {
                        Control = typeof(UIWallPlacer),
                        ResID = (ulong)WallStyleIDs[i],
                        Res = res,
                        Parameters = new List<int> { WallStylePatterns[i], WallStyleIDs[i] } //pattern, style
                    }
                });
            }
        }

        public static short[] WallStyleIDs =
        {
            0x1, //wall
            0x2, //picket fence
            0xD, //iron fence
            0xC, //privacy fence
            0xE //banisters
        };

        public static short[] WallStylePatterns =
        {
            0, //wall
            248, //picket fence
            250, //iron fence
            249, //privacy fence
            251, //banisters
        };

        private int PageSize;
        private List<UICatalogElement> Selected;
        private UICatalogItem[] CatalogItems;
        private Dictionary<uint, Texture2D> IconCache;

        public UICatalog(int pageSize)
        {
            IconCache = new Dictionary<uint, Texture2D>();
            PageSize = pageSize;
        }

        public void SetActive(int selection, bool active) {
            int index = selection - Page * PageSize;
            if (index >= 0 && index < CatalogItems.Length) CatalogItems[index].SetActive(active);
        }

        public void SetCategory(List<UICatalogElement> select) {
            Selected = select;
            SetPage(0);
        }

        public int TotalPages()
        {
            if (Selected == null) return 0;
            return ((Selected.Count-1) / PageSize)+1;
        }

        public int GetPage()
        {
            return Page;
        }

        public void SetPage(int page) {
            if (CatalogItems != null)
            {
                for (int i = 0; i < CatalogItems.Length; i++)
                {
                    this.Remove(CatalogItems[i]);
                }
            }

            int index = page*PageSize;
            CatalogItems = new UICatalogItem[Math.Min(PageSize, Math.Max(Selected.Count-index, 0))];
            int halfPage = PageSize / 2;
            
            for (int i=0; i<CatalogItems.Length; i++) {
                var elem = new UICatalogItem(false);
                elem.Index = index;
                elem.Info = Selected[index++];
                elem.Icon = (elem.Info.Special != null)?elem.Info.Special.Res.GetIcon(elem.Info.Special.ResID):GetObjIcon(elem.Info.GUID);
                elem.Tooltip = "$"+elem.Info.Price.ToString();
                elem.X = (i % halfPage) * 45 + 2;
                elem.Y = (i / halfPage) * 45 + 2;
                elem.OnMouseEvent += new ButtonClickDelegate(InnerSelect);
                CatalogItems[i] = elem;
                this.Add(elem);
            }
            Page = page;
        }

        void InnerSelect(UIElement button)
        {
            if (OnSelectionChange != null) OnSelectionChange(((UICatalogItem)button).Index);
        }

        public Texture2D GetObjIcon(uint GUID)
        {
            if (!IconCache.ContainsKey(GUID)) {
                var obj = Content.Content.Get().WorldObjects.Get(GUID);
                if (obj == null)
                {
                    IconCache[GUID] = null;
                    return null;
                }
                var bmp = obj.Resource.Get<BMP>(obj.OBJ.CatalogStringsID);
                if (bmp != null) IconCache[GUID] = bmp.GetTexture(GameFacade.GraphicsDevice);
                else IconCache[GUID] = null;
            }
            return IconCache[GUID];
        }

        private class CatalogSorter : IComparer<UICatalogElement>
        {
            #region IComparer<UICatalogElement> Members

            public int Compare(UICatalogElement x, UICatalogElement y)
            {
                if (x.Price > y.Price) return 1;
                else if (x.Price < y.Price) return -1;
                else return 0;
            }

            #endregion
        }
    }

    public delegate void CatalogSelectionChangeDelegate(int selection);

    public struct UICatalogElement {
        public uint GUID;
        public sbyte Category;
        public uint Price;
        public string Name;
        public UISpecialCatalogElement Special;
    }

    public class UISpecialCatalogElement
    {
        public Type Control;
        public ulong ResID;
        public UICatalogResProvider Res;
        public List<int> Parameters;
    }
}
