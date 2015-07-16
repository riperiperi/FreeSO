using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Framework;
using System.Xml;
using System.IO;
using TSO.Content;
using Microsoft.Xna.Framework.Graphics;
using TSO.Files.formats.iff.chunks;
using TSOClient.LUI;
using TSOClient.Code.UI.Panels.LotControls;

namespace TSOClient.Code.UI.Controls.Catalog
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

                    for (int i = 0; i < 30; i++) _Catalog[i].Sort(new CatalogSorter());

                    _Catalog[7].Insert(0, new UICatalogElement { Name="Wall", Category=7, Price=0,
                        Special = new UISpecialCatalogElement
                        {
                            Control = typeof(UIWallPlacer),
                            Icon = Content.Get().WorldWalls.WallIcon,
                            Parameters = new List<int> { 0, 1 } //pattern, style
                        }
                    });

                    return _Catalog;
                }
            }
        }

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
                elem.Icon = (elem.Info.Special != null)?elem.Info.Special.Icon.GetTexture(GameFacade.GraphicsDevice):GetObjIcon(elem.Info.GUID);
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
                var obj = Content.Get().WorldObjects.Get(GUID);
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
        public BMP Icon;
        public List<int> Parameters;
    }
}
