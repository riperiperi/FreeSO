/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
RHY3756547. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Framework;
using TSOClient.Code.UI.Panels;
using TSOClient.Code.UI.Model;
using TSOClient.Code.UI.Controls;
using TSOClient.LUI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TSOClient.Code.Utils;
using TSOClient.Code.UI.Framework;
using TSO.Simantics;

namespace TSOClient.Code.UI.Panels
{
    public class UIPieMenu : UIContainer
    {
        public UIPieMenuItem PieTree;
        public List<UIButton> PieButtons;
        public UIPieMenuItem CurrentItem;
        public VMEntity obj;
        public VMEntity caller;
        public UILotControl Parent;
        public UIImage bg;
        private double bgGrow;

        private TextStyle ButtonStyle;

        public UIPieMenu(List<VMPieMenuInteraction> pie, VMEntity obj, VMEntity caller, UILotControl parent)
        {
            PieButtons = new List<UIButton>();
            this.obj = obj;
            this.caller = caller;
            this.Parent = parent;
            this.ButtonStyle = new TextStyle
            {
                Font = GameFacade.MainFont,
                Size = 12,
                Color = new Color(0xA5, 0xC3, 0xD6),
                SelectedColor = new Color(0x00, 0xFF, 0xFF),
                CursorColor = new Color(255, 255, 255)
            };

            bg = new UIImage(TextureGenerator.GetPieBG(GameFacade.GraphicsDevice));
            bg.SetSize(0, 0); //is scaled up later
            this.AddAt(0, bg);

            PieTree = new UIPieMenuItem()
            {
                Category = true
            };

            for (int i = 0; i < pie.Count; i++)
            {
                string[] depth = pie[i].Name.Split('/');

                var category = PieTree; //set category to root
                for (int j = 0; j < depth.Length-1; j++) //iterate through categories
                {
                    if (category.Children.ContainsKey(depth[j]))
                    {
                        category = category.Children[depth[j]];
                    }
                    else
                    {
                        var newCat = new UIPieMenuItem()
                        {
                            Category = true,
                            Name = depth[j],
                            Parent = category
                        };
                        category.Children.Add(depth[j], newCat);
                        category = newCat;
                    }
                }
                //we are in the category, put the interaction in here;

                var item = new UIPieMenuItem()
                {
                    Category = false,
                    Name = depth[depth.Length-1],
                    ID = pie[i].ID
                };
                if (!category.Children.ContainsKey(item.Name)) category.Children.Add(item.Name, item);
            }

            CurrentItem = PieTree;
            PieButtons = new List<UIButton>();
            RenderMenu();
        }

        public override void Update(TSO.Common.rendering.framework.model.UpdateState state)
        {
            base.Update(state);
            if (bgGrow < 1)
            {
                bgGrow += 1.0 / 30.0;
                bg.SetSize((float)bgGrow * 200, (float)bgGrow * 200);
                bg.X = (float)bgGrow * (-100);
                bg.Y = (float)bgGrow * (-100);
            }
        }

        public void RenderMenu()
        {
            for (int i = 0; i < PieButtons.Count; i++) //remove previous buttons
            {
                this.Remove(PieButtons[i]);
            }
            PieButtons.Clear();

            var elems = CurrentItem.Children;
            int dirConfig;
            if (elems.Count > 4) dirConfig = 8;
            else if (elems.Count > 2) dirConfig = 4;
            else dirConfig = 2;

            for (int i = 0; i < dirConfig; i++)
            {
                if (i >= elems.Count) break;
                var elem = elems.ElementAt(i);
                var but = new UIButton()
                {
                    Caption = elem.Value.Name+((elem.Value.Category)?"...":""),
                    CaptionStyle = ButtonStyle,
                    ImageStates = 1,
                    Texture = TextureGenerator.GetPieButtonImg(GameFacade.GraphicsDevice)

                };
                double dir = (((double)i)/dirConfig)*Math.PI*2;
                but.AutoMargins = 4;

                if (i == 0) { //top
                    but.X = (float)(Math.Sin(dir)*60-but.Width/2);
                    but.Y = (float)((Math.Cos(dir)*-60)-but.Size.Y);
                } else if (i == dirConfig/2) { //bottom
                    but.X = (float)(Math.Sin(dir)*60-but.Width/2);
                    but.Y = (float)((Math.Cos(dir)*-60));
                }
                else if (i < dirConfig / 2) //on right side
                {
                    but.X = (float)(Math.Sin(dir) * 60);
                    but.Y = (float)((Math.Cos(dir) * -60) - but.Size.Y / 2);
                }
                else //on left side
                {
                    but.X = (float)(Math.Sin(dir) * 60-but.Width);
                    but.Y = (float)((Math.Cos(dir) * -60) - but.Size.Y / 2);
                }

                this.Add(but);
                PieButtons.Add(but);
                but.OnButtonClick += new ButtonClickDelegate(PieButtonClick);
            }

            bool top = true;
            for (int i = 8; i < elems.Count; i++)
            {
                var elem = elems.ElementAt(i);
                var but = new UIButton()
                {
                    Caption = elem.Value.Name+((elem.Value.Category)?"...":""),
                    CaptionStyle = ButtonStyle,
                    ImageStates = 1,
                    Texture = TextureGenerator.GetPieButtonImg(GameFacade.GraphicsDevice)
                };
                but.AutoMargins = 4;

                but.X = (float)(- but.Width / 2);
                if (top)
                { //top
                    but.Y = (float)(-60 - but.Size.Y*((i-8)/2 + 2));
                }
                else
                {
                    but.Y = (float)(60 + but.Size.Y * ((i - 8) / 2 + 1));
                }

                this.Add(but);
                PieButtons.Add(but);
                but.OnButtonClick += new ButtonClickDelegate(PieButtonClick);

                top = !top;
            }

            if (CurrentItem.Parent != null)
            {
                var but = new UIButton()
                {
                    Caption = CurrentItem.Name,
                    CaptionStyle = ButtonStyle.Clone(),
                    ImageStates = 1,
                    Texture = TextureGenerator.GetPieButtonImg(GameFacade.GraphicsDevice)
                };
                but.CaptionStyle.Color = but.CaptionStyle.SelectedColor;
                but.AutoMargins = 4;
                but.X = (float)(- but.Width / 2);
                but.Y = (float)(- but.Size.Y / 2);
                this.Add(but);
                PieButtons.Add(but);
                but.OnButtonClick += new ButtonClickDelegate(BackButtonPress);
            }
        }

        void BackButtonPress(UIElement button)
        {
            if (CurrentItem.Parent == null) return; //shouldn't ever be...
            CurrentItem = CurrentItem.Parent;
            RenderMenu();
        }

        private void PieButtonClick(UIElement button)
        {
            int index = PieButtons.IndexOf((UIButton)button);
            var action = CurrentItem.Children.ElementAt(index).Value;

            if (action.Category) {
                CurrentItem = action;
                RenderMenu();
            } else {
                obj.PushUserInteraction(action.ID, caller, Parent.vm.Context);
                Parent.ClosePie();
            }
        }

    }

    public class UIPieMenuItem
    {
        public bool Category;
        public byte ID;
        public string Name;
        public Dictionary<string, UIPieMenuItem> Children = new Dictionary<string, UIPieMenuItem>();
        public UIPieMenuItem Parent;
    }
}
