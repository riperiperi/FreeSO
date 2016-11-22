using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Common.DataService.Model;
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

        public Dictionary<UIButton, LotCategory> ButtonToCategory;

        public delegate void CategoryChangeHandler(LotCategory cat);
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


            ButtonToCategory = new Dictionary<UIButton, LotCategory>()
            {
                { CategoryButton_Welcome, LotCategory.welcome },
                { CategoryButton_Money, LotCategory.money },
                { CategoryButton_Skills, LotCategory.skills },
                { CategoryButton_Services, LotCategory.services },
                { CategoryButton_Entertainment, LotCategory.entertainment },
                { CategoryButton_Romance, LotCategory.romance },
                { CategoryButton_Shopping, LotCategory.shopping },
                { CategoryButton_Games, LotCategory.games },
                { CategoryButton_Offbeat, LotCategory.offbeat },
                { CategoryButton_Residence, LotCategory.residence },
                { CategoryButton_None, LotCategory.none }
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
