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
using static FSO.Content.WorldObjectCatalog;
using FSO.Common;
using FSO.SimAntics;
using FSO.SimAntics.Model;
using FSO.Content.Interfaces;
using FSO.Client.UI.Panels;
using System.Text.RegularExpressions;

namespace FSO.Client.UI.Controls.Catalog
{
    public class UICatalog : UIContainer
    {
        private int _Page;
        public int Page { get => _Page; }
        private int _Budget;
        public UILotControl LotControl;
        public VM ActiveVM { get
            {
                return LotControl?.vm;
            }
        }
        public int Budget
        {
            get { return _Budget; }
            set {
                if (value != _Budget)
                {
                    if (CatalogItems != null)
                    {
                        for (int i = 0; i < CatalogItems.Length; i++)
                        {
                            CatalogItems[i].SetDisabled(CatalogItems[i].Info.Item.Price > value);
                        }
                    }
                    _Budget = value;
                }
            }
        }
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

                    foreach (var obj in Content.Content.Get().WorldCatalog.All())
                    {
                        _Catalog[obj.Category].Add(new UICatalogElement()
                        {
                            Item = obj
                        });
                    }

                    AddWallpapers();
                    AddFloors();

                    for (int i = 0; i < 30; i++) _Catalog[i].Sort(new CatalogSorter());

                    AddWallStyles();
                    AddRoofs();
                    AddTerrainTools();

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
                    Item = new ObjectCatalogItem()
                    {
                        Name = wall.Name,
                        Category = 8,
                        Price = (uint)wall.Price,
                    },
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
                sbyte category = (sbyte)((floor.ID >= 65534) ? 5 : 9);
                _Catalog[category].Insert(0, new UICatalogElement
                {
                    Item = new ObjectCatalogItem()
                    {
                        Name = floor.Name,
                        Category = category,
                        Price = (uint)floor.Price,
                    },
                    Special = new UISpecialCatalogElement
                    {
                        Control = typeof(UIFloorPainter),
                        ResID = floor.ID,
                        Res = res,
                        Parameters = new List<int> { (int)floor.ID } //pattern
                    }
                });
            }
        }

        private static void AddRoofs()
        {
            var res = new UICatalogRoofResProvider();

            var total = Content.Content.Get().WorldRoofs.Count;

            for (int i = 0; i < total; i++)
            {
                sbyte category = 6;
                _Catalog[category].Insert(0, new UICatalogElement
                {
                    Item = new ObjectCatalogItem()
                    {
                        Name = "",
                        Category = category,
                        Price = 0,
                    },
                    Special = new UISpecialCatalogElement
                    {
                        Control = typeof(UIRoofer),
                        ResID = (uint)i,
                        Res = res,
                        Parameters = new List<int> { i } //pattern
                    }
                });
            }
        }

        private static void AddWallStyles()
        {
            var res = new UICatalogWallResProvider();

            for (int i = 0; i < WallStyleIDs.Length; i++)
            {
                var walls = Content.Content.Get().WorldWalls;
                var style = walls.GetWallStyle((ulong)WallStyleIDs[i]);
                _Catalog[7].Insert(0, new UICatalogElement
                {
                    Item = new ObjectCatalogItem()
                    {
                        Name = style.Name,
                        Category = 7,
                        Price = (uint)style.Price,
                    },
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

        private static void AddTerrainTools()
        {
            var res = new UICatalogWallResProvider();

            _Catalog[10].Insert(0, new UICatalogElement
            {
                Item = new ObjectCatalogItem()
                {
                    Name = "Raise/Lower Terrain",
                    Category = 7,
                    Price = 1,
                },
                Special = new UISpecialCatalogElement
                {
                    Control = typeof(UITerrainRaiser),
                    ResID = 0,
                    Res = new UICatalogTerrainResProvider(),
                    Parameters = new List<int> { }
                }
            });

            _Catalog[10].Insert(0, new UICatalogElement
            {
                Item = new ObjectCatalogItem()
                {
                    Name = "Flatten Terrain",
                    Category = 7,
                    Price = 1,
                },
                Special = new UISpecialCatalogElement
                {
                    Control = typeof(UITerrainFlatten),
                    ResID = 1,
                    Res = new UICatalogTerrainResProvider(),
                    Parameters = new List<int> { }
                }
            });

            _Catalog[10].Insert(0, new UICatalogElement
            {
                Item = new ObjectCatalogItem()
                {
                    Name = "Grass Tool",
                    Category = 7,
                    Price = 1,
                },
                Special = new UISpecialCatalogElement
                {
                    Control = typeof(UIGrassPaint),
                    ResID = 2,
                    Res = new UICatalogTerrainResProvider(),
                    Parameters = new List<int> { }
                }
            });
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

        public int PageSize { get; set; }
        public List<UICatalogElement> Selected;
        public List<UICatalogElement> Filtered;
        private UICatalogItem[] CatalogItems;
        private Dictionary<uint, Texture2D> IconCache;

        private string SearchTerm;

        public UICatalog(int pageSize)
        {
            IconCache = new Dictionary<uint, Texture2D>();
            PageSize = pageSize;
        }

        public void SetActive(int selection, bool active) {
            int index = selection - _Page * PageSize;
            if (index >= 0 && index < CatalogItems.Length) CatalogItems[index].SetActive(active);
        }

        public void SetCategory(List<UICatalogElement> select) {
            Selected = select;
            FilterSelected();
            SetPage(0);
        }

        private void AddMatchScore(string name, Regex search, ref int score)
        {
            if (name == null) return;

            var allMatches = search.Matches(name);

            foreach (Match match in allMatches)
            {
                int matchScore = 4;

                if (match.Index == 0 || char.IsWhiteSpace(name[match.Index - 1]))
                {
                    matchScore *= 2;
                }

                if (match.Index + match.Value.Length == name.Length || char.IsWhiteSpace(name[match.Index + match.Value.Length]))
                {
                    matchScore *= 3;
                }

                score += matchScore;
            }
        }

        private int GetScore(UICatalogElement elem)
        {
            ref var item = ref elem.Item;

            string name = item.Name.ToLowerInvariant();
            string catalogName = item.CatalogName?.ToLowerInvariant();
            string tags = item.Tags?.ToLowerInvariant();

            string[] termWords = SearchTerm.ToLowerInvariant().Split(' ');

            int score = 0;

            foreach (string word in termWords)
            {
                var search = new Regex(".*" + Regex.Escape(word) + ".*");

                AddMatchScore(name, search, ref score);
                AddMatchScore(catalogName, search, ref score);
                AddMatchScore(tags, search, ref score);
            }

            return score;
        }

        public void FilterSelected()
        {
            if (SearchTerm != null && Selected != null)
            {
                Filtered = Selected
                    .Select(elem => new Tuple<UICatalogElement, int>(elem, GetScore(elem)))
                    .Where(tuple => tuple.Item2 > 0)
                    .OrderByDescending(tuple => tuple.Item2)
                    .Select(tuple => tuple.Item1)
                    .ToList();
            }
            else
            {
                Filtered = Selected;
            }
        }

        public void SetSearchTerm(string term)
        {
            if (term == "") term = null;

            if (SearchTerm != term)
            {
                SearchTerm = term;
                FilterSelected();
                SetPage(0);
            }
        }

        public int TotalPages()
        {
            if (Filtered == null) return 0;
            return ((Filtered.Count - 1) / PageSize) + 1;
        }

        public int GetPage()
        {
            return _Page;
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
            if (Filtered == null) return;
            CatalogItems = new UICatalogItem[Math.Min(PageSize, Math.Max(Filtered.Count - index, 0))];
            int halfPage = PageSize / 2;
            
            for (int i=0; i<CatalogItems.Length; i++)
            {
                var sel = Filtered[index++];
                var elem = new UICatalogItem(false);
                if (sel.Item.GUID == uint.MaxValue) elem.Visible = false;
                elem.Index = index-1;
                elem.Info = sel;
                elem.Info.CalcPrice = (int)elem.Info.Item.Price;

                if (elem.Info.Item.GUID != 0)
                {
                    var price = (int)elem.Info.Item.Price;
                    var dcPercent = VMBuildableAreaInfo.GetDiscountFor(elem.Info.Item, ActiveVM);
                    var finalPrice = (price * (100 - dcPercent)) / 100;
                    if (LotControl.ObjectHolder.DonateMode) finalPrice -= (finalPrice * 2) / 3;
                    elem.Info.CalcPrice = finalPrice;
                }

                elem.Icon = (elem.Info.Special?.Res != null)?elem.Info.Special.Res.GetIcon(elem.Info.Special.ResID):GetObjIcon(elem.Info.Item.GUID);
                elem.Tooltip = (elem.Info.CalcPrice > 0)?("$"+elem.Info.CalcPrice.ToString()):null;
                elem.X = (i % halfPage) * 45 + 2;
                elem.Y = (i / halfPage) * 45 + 2;
                elem.OnMouseEvent += new ButtonClickDelegate(InnerSelect);
                elem.SetDisabled(elem.Info.CalcPrice > Budget);
                CatalogItems[i] = elem;
                this.Add(elem);
            }
            _Page = page;
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
                if (x.Item.Price > y.Item.Price) return 1;
                else if (x.Item.Price < y.Item.Price) return -1;
                else return 0;
            }

            #endregion
        }
    }

    public delegate void CatalogSelectionChangeDelegate(int selection);

    public struct UICatalogElement {
        public ObjectCatalogItem Item;
        public int CalcPrice;
        public UISpecialCatalogElement Special;
        public int? Count;
        public List<int> Attributes;
        public object Tag;
    }

    public class UISpecialCatalogElement
    {
        public Type Control;
        public ulong ResID;
        public UICatalogResProvider Res;
        public List<int> Parameters;
    }
}
