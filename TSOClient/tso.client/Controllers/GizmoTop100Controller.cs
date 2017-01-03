using FSO.Client.UI.Panels;
using FSO.Common.DatabaseService;
using FSO.Common.DatabaseService.Model;
using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using FSO.Common.Domain.Top100;
using FSO.Common.Enum;
using FSO.Content.Model;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.Controllers
{
    public class GizmoTop100Controller
    {
        private UIGizmoTop100 View;
        private IDatabaseService DatabaseService;
        private IClientDataService DataService;
        private Top100Category Category;
        private ITop100Domain Domain;
        private Content.Content Content;

        private Top100Category LastLotCategory = Top100Category.lot_money;
        private Top100Category LastAvatarCategory = Top100Category.avatar_most_famous;

        public GizmoTop100Controller(UIGizmoTop100 view, IDatabaseService databaseService, IClientDataService dataService, ITop100Domain top100, Content.Content content)
        {
            this.View = view;
            this.DatabaseService = databaseService;
            this.DataService = dataService;
            this.Domain = top100;
            this.Content = content;

            SetTab(UIGizmoTab.Property);
        }

        public void SetTab(UIGizmoTab tab)
        {
            var categoryType = tab == UIGizmoTab.People ? Top100CategoryType.AVATAR : Top100CategoryType.LOT;
            var items = Domain.Categories.Where(x => x.CategoryType == categoryType).Select(x =>
            {
                return new Top100CategoryListItem()
                {
                    Category = x.Category,
                    Icon = GetCategoryIcon(x.Category),
                    Name = x.Name
                };
            }).ToList();

            View.DisplayCategories(items);

            if (tab == UIGizmoTab.Property)
            {
                SetCategory(LastLotCategory);
            }
            else
            {
                SetCategory(LastAvatarCategory);
            }
        }

        public void SetCategory(Top100Category category)
        {
            this.Category = category;

            if (category.IsLotCategory()){
                LastLotCategory = category;
            }else{
                LastAvatarCategory = category;
            }

            View.SelectCategory(category);
            DatabaseService.GetTop100(new GetTop100Request() { Category = category })
                .ContinueWith(x => {
                    //Category has been switched
                    if (Category != category) { return; }


                    List<Top100ListItem> enriched = null;

                    if (category.IsLotCategory())
                    {
                        enriched = DataService.EnrichList<Top100ListItem, Top100Entry, Lot>(x.Result.Items, y => y.TargetId, (top100Entry, lot) =>
                        {
                            return new Top100ListItem()
                            {
                                Top100Entry = top100Entry,
                                Lot = lot
                            };
                        });
                    }
                    else
                    {
                        enriched = DataService.EnrichList<Top100ListItem, Top100Entry, Avatar>(x.Result.Items, z => z.TargetId, (top100Entry, avatar) =>
                        {
                            return new Top100ListItem()
                            {
                                Top100Entry = top100Entry,
                                Avatar = avatar
                            };
                        });
                    }

                    //Fill in any gaps
                    while(enriched.Count < 100)
                    {
                        enriched.Add(new Top100ListItem {
                            Top100Entry = new Top100Entry()
                            {
                                Rank = (byte)(enriched.Count+1)
                            }
                        });
                    }

                    enriched = enriched.OrderBy(i => i.Top100Entry.Rank).ToList();
                    View.DisplayResults(enriched);
                });
        }

        /// <summary>
        /// In the original game the icons were transmitted from the server. However, unless we plan to change
        /// the categories often this is not really necessary
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        private ITextureRef GetCategoryIcon(Top100Category category)
        {
            switch (category)
            {
                case Top100Category.lot_money:
                    return Content.UIGraphics.Get(0x00000B3B00000001);
                case Top100Category.lot_offbeat:
                    return Content.UIGraphics.Get(0x00000B3C00000001);
                case Top100Category.lot_romance:
                    return Content.UIGraphics.Get(0x00000B3D00000001);
                case Top100Category.lot_services:
                    return Content.UIGraphics.Get(0x00000B3E00000001);
                case Top100Category.lot_shopping:
                    return Content.UIGraphics.Get(0x00000B3F00000001);
                case Top100Category.lot_skills:
                    return Content.UIGraphics.Get(0x00000B4000000001);
                case Top100Category.lot_welcome:
                    return Content.UIGraphics.Get(0x00000B4100000001);
                case Top100Category.lot_games:
                    return Content.UIGraphics.Get(0x00000B3A00000001);
                case Top100Category.lot_entertainment:
                    return Content.UIGraphics.Get(0x00000B3900000001);
                case Top100Category.lot_residence:
                    return Content.UIGraphics.Get(0x00000B8400000001);
                case Top100Category.avatar_most_famous:
                    return Content.UIGraphics.Get(0x0000031600000001);
                case Top100Category.avatar_best_karma:
                    return Content.UIGraphics.Get(0x00000ce500000001);
                case Top100Category.avatar_friendliest:
                    return Content.UIGraphics.Get(0x00000ce300000001);
                case Top100Category.avatar_most_infamous:
                    return Content.UIGraphics.Get(0x00000ce400000001);
                case Top100Category.avatar_meanest:
                    return Content.UIGraphics.Get(0x00000ce600000001);
                default:
                    return null;
            }
        }
    }
    

    public class Top100CategoryListItem
    {
        public Top100Category Category;
        public ITextureRef Icon;
        public string Name;
    }

    public class Top100ListItem
    {
        public Top100Entry Top100Entry;
        public Lot Lot;
        public Avatar Avatar;

        public string TargetName
        {
            get
            {
                if(Top100Entry.TargetId == null){
                    return "";
                }

                if(Lot != null && Lot.Lot_Name != null && !Lot.IsDefaultName)
                {
                    return Lot.Lot_Name;
                }else if(Avatar != null && Avatar.Avatar_Name != null && !Avatar.IsDefaultName){
                    return Avatar.Avatar_Name;
                }else{
                    return Top100Entry.TargetName;
                }
            }
        }

        public bool TargetIsOnline
        {
            get
            {
                if (Top100Entry.TargetId == null){
                    return false;
                }

                if (Lot != null)
                {
                    return Lot.Lot_IsOnline;
                }else if(Avatar != null){
                    return Avatar.Avatar_IsOnline;
                }else{
                    return false;
                }
            }
        }

        public bool TargetIsOffline
        {
            get
            {
                return !TargetIsOnline;
            }
        }
    }
}
