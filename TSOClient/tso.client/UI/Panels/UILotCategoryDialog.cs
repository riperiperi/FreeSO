using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Server.Database.DA.Lots;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Panels
{
    public class UILotCategoryDialog : UIDialog
    {
        public Texture2D HouseCategoryBackgroundImage { get; set; }

        public UIButton CategoryButton_Welcome { get; set; }
        public UIButton CategoryButton_Money { get; set; }
        public UIButton CategoryButton_Skills { get; set; }
        public UIButton CategoryButton_Services { get; set; }
        public UIButton CategoryButton_Entertainment { get; set; }
        public UIButton CategoryButton_Romance { get; set; }
        public UIButton CategoryButton_Shopping { get; set; }
        public UIButton CategoryButton_Games { get; set; }
        public UIButton CategoryButton_Offbeat { get; set; }
        public UIButton CategoryButton_Residence { get; set; }
        public UIButton CategoryButton_None { get; set; }

        public Dictionary<UIButton, DbLotCategory> ButtonToCategory;

        public delegate void CategoryChangeHandler(DbLotCategory cat);
        public event CategoryChangeHandler OnCategoryChange;

        public UILotCategoryDialog() : base(UIDialogStyle.Close, true)
        {
            var script = RenderScript("lotcategoryselectiondialog.uis");
            SetSize(201, 180);

            for (int i=1; i<12; i++)
            {
                var bg = script.Create<UIImage>("BackgroundThumbImage"+i);
                bg.Texture = HouseCategoryBackgroundImage;
                AddAt(3, bg);
            }


            ButtonToCategory = new Dictionary<UIButton, DbLotCategory>()
            {
                { CategoryButton_Welcome, DbLotCategory.welcome },
                { CategoryButton_Money, DbLotCategory.money },
                { CategoryButton_Skills, DbLotCategory.skills },
                { CategoryButton_Services, DbLotCategory.services },
                { CategoryButton_Entertainment, DbLotCategory.entertainment },
                { CategoryButton_Romance, DbLotCategory.romance },
                { CategoryButton_Shopping, DbLotCategory.shopping },
                { CategoryButton_Games, DbLotCategory.games },
                { CategoryButton_Offbeat, DbLotCategory.offbeat },
                { CategoryButton_Residence, DbLotCategory.residence },
                { CategoryButton_None, DbLotCategory.none }
            };

            foreach (var pair in ButtonToCategory)
            {
                pair.Key.OnButtonClick += CategoryChoose;
            }

            CloseButton.OnButtonClick += Close;
        }

        private void CategoryChoose(UIElement button)
        {
            var cat = ButtonToCategory[(UIButton)button];
            OnCategoryChange?.Invoke(cat);
            UIScreen.RemoveDialog(this);
        }

        private void Close(Framework.UIElement button)
        {
            UIScreen.RemoveDialog(this);
        }
    }
}
