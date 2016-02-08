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
using FSO.Client.UI.Panels;
using FSO.Client.UI.Model;
using FSO.Client.UI.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FSO.Client.Utils;
using FSO.SimAntics;
using FSO.HIT;
using FSO.Vitaboy;
using FSO.Common.Rendering.Framework.Camera;
using FSO.Common.Rendering.Framework;
using FSO.Common.Utils;
using FSO.SimAntics.NetPlay.Model.Commands;

namespace FSO.Client.UI.Panels
{
    public class UIPieMenu : UIContainer
    {
        public UIPieMenuItem m_PieTree;
        public List<UIButton> m_PieButtons;
        public UIPieMenuItem m_CurrentItem;
        public VMEntity m_Obj;
        public VMEntity m_Caller;
        public UILotControl m_Parent;
        public UIImage m_Bg;

        private _3DTargetScene HeadScene;
        private BasicCamera HeadCamera;
        private double m_BgGrow;

        private bool ShiftDown; //shift activates IDE

        //This is a standard AdultVitaboyModel instance. Since nothing is needed but the head for pie menus,
        //the other parts of the body will be stripped from it (see constructor).
        private SimAvatar m_Head;

        private TextStyle ButtonStyle;

        public UIPieMenu(List<VMPieMenuInteraction> pie, VMEntity obj, VMEntity caller, UILotControl parent)
        {
            m_PieButtons = new List<UIButton>();
            this.m_Obj = obj;
            this.m_Caller = caller;
            this.m_Parent = parent;
            this.ButtonStyle = new TextStyle
            {
                Font = GameFacade.MainFont,
                Size = 12,
                Color = new Color(0xA5, 0xC3, 0xD6),
                SelectedColor = new Color(0x00, 0xFF, 0xFF),
                CursorColor = new Color(255, 255, 255)
            };

            m_Bg = new UIImage(TextureGenerator.GetPieBG(GameFacade.GraphicsDevice));
            m_Bg.SetSize(0, 0); //is scaled up later
            this.AddAt(0, m_Bg);

            m_PieTree = new UIPieMenuItem()
            {
                Category = true
            };

            for (int i = 0; i < pie.Count; i++)
            {
                string[] depth = pie[i].Name.Split('/');

                var category = m_PieTree; //set category to root
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
                    Name = depth[depth.Length - 1],
                    ID = pie[i].ID,
                    Param0 = pie[i].Param0
                };
                if (!category.Children.ContainsKey(item.Name)) category.Children.Add(item.Name, item);
            }

            m_CurrentItem = m_PieTree;
            m_PieButtons = new List<UIButton>();
            RenderMenu();

            VMAvatar Avatar = (VMAvatar)caller;
            m_Head = new SimAvatar(Avatar.Avatar); //talk about confusing...
            m_Head.StripAllButHead();

            initSimHead();
        }

        private void initSimHead()
        {
            HeadCamera = new BasicCamera(GameFacade.GraphicsDevice, new Vector3(0.0f, 7.0f, -17.0f), Vector3.Zero, Vector3.Up);

            HeadCamera.Position = new Vector3(0, 5.2f, 12.5f);
            HeadCamera.Target = new Vector3(0, 5.2f, 0.0f);

            HeadScene = new _3DTargetScene(GameFacade.Game.GraphicsDevice, HeadCamera, new Point(200,200), (GlobalSettings.Default.AntiAlias) ? 8 : 0);
            HeadScene.ID = "UIPieMenuHead";

            m_Head.Scene = HeadScene;
            m_Head.Scale = new Vector3(1f);

            HeadCamera.Zoom = 0f;
            HeadScene.Add(m_Head);
            GameFacade.Scenes.AddExternal(HeadScene); //AddExternal(HeadScene);
        }

        public void RotateHeadCam(Vector2 point)
        {
            double xdir = Math.Atan(-point.X / 100.0);
            double ydir = Math.Atan(-point.Y / 100.0);

            Vector3 off = new Vector3(0, 0, 13.5f);
            Matrix mat = Microsoft.Xna.Framework.Matrix.CreateRotationY((float)xdir) * Microsoft.Xna.Framework.Matrix.CreateRotationX((float)ydir);

            HeadCamera.Position = new Vector3(0, 5.2f, 0)+Vector3.Transform(off, mat);
        }

        public void RemoveSimScene()
        {
            GameFacade.Scenes.RemoveExternal(HeadScene);
            HeadScene.Target.Dispose();
        }

        public void UpdateHeadPosition(int x, int y)
        {
            HeadCamera.ProjectionOrigin = new Vector2(100, 100);
        }

        public override void Update(FSO.Common.Rendering.Framework.Model.UpdateState state)
        {
            base.Update(state);
            if (m_BgGrow < 1)
            {
                m_BgGrow += 1.0 / 30.0;
                HeadCamera.Zoom = (float)m_BgGrow*5.12f;

                m_Bg.SetSize((float)m_BgGrow * 200, (float)m_BgGrow * 200);
                m_Bg.X = (float)m_BgGrow * (-100);
                m_Bg.Y = (float)m_BgGrow * (-100);
            }
            RotateHeadCam(GlobalPoint(new Vector2(state.MouseState.X, state.MouseState.Y)));
            ShiftDown = state.KeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift);
        }

        public void RenderMenu()
        {
            for (int i = 0; i < m_PieButtons.Count; i++) //remove previous buttons
            {
                this.Remove(m_PieButtons[i]);
            }
            m_PieButtons.Clear();

            var elems = m_CurrentItem.Children;
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
                m_PieButtons.Add(but);
                but.OnButtonClick += new ButtonClickDelegate(PieButtonClick);
                but.OnButtonHover += new ButtonClickDelegate(PieButtonHover);
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
                m_PieButtons.Add(but);
                but.OnButtonClick += new ButtonClickDelegate(PieButtonClick);

                top = !top;
            }

            if (m_CurrentItem.Parent != null)
            {
                var but = new UIButton()
                {
                    Caption = m_CurrentItem.Name,
                    CaptionStyle = ButtonStyle.Clone(),
                    ImageStates = 1,
                    Texture = TextureGenerator.GetPieButtonImg(GameFacade.GraphicsDevice)
                };

                but.CaptionStyle.Color = but.CaptionStyle.SelectedColor;
                but.AutoMargins = 4;
                but.X = (float)(- but.Width / 2);
                but.Y = (float)(- but.Size.Y / 2);
                this.Add(but);
                m_PieButtons.Add(but);
                but.OnButtonClick += new ButtonClickDelegate(BackButtonPress);
            }
        }

        void PieButtonHover(UIElement button)
        {
            int index = m_PieButtons.IndexOf((UIButton)button);
            //todo, make sim look at button
            HITVM.Get().PlaySoundEvent(UISounds.PieMenuHighlight);
        }

        void BackButtonPress(UIElement button)
        {
            if (m_CurrentItem.Parent == null) return; //shouldn't ever be...
            m_CurrentItem = m_CurrentItem.Parent;
            HITVM.Get().PlaySoundEvent(UISounds.PieMenuSelect);
            RenderMenu();
        }

        private void PieButtonClick(UIElement button)
        {
            int index = m_PieButtons.IndexOf((UIButton)button);
            if (index == -1) return; //bail! this isn't meant to happen!
            var action = m_CurrentItem.Children.ElementAt(index).Value;
            HITVM.Get().PlaySoundEvent(UISounds.PieMenuSelect);

            if (action.Category) {
                m_CurrentItem = action;
                RenderMenu();
            } else {

                if (m_Obj == m_Parent.GotoObject)
                {
                    m_Parent.vm.SendCommand(new VMNetGotoCmd
                    {
                        Interaction = action.ID,
                        CallerID = m_Caller.ObjectID,
                        x = m_Obj.Position.x,
                        y = m_Obj.Position.y,
                        level = m_Obj.Position.Level
                    });
                }
                else
                {
                    if (Debug.IDEHook.IDE != null && ShiftDown) {
                        if (m_Obj.TreeTable.InteractionByIndex.ContainsKey((uint)action.ID)) {
                            var act = m_Obj.TreeTable.InteractionByIndex[(uint)action.ID];
                            ushort ActionID = act.ActionFunction;

                            var function = m_Obj.GetBHAVWithOwner(ActionID, m_Parent.vm.Context);

                            Debug.IDEHook.IDE.InjectIDEInto(
                                GameFacade.Screens.CurrentUIScreen,
                                m_Parent.vm,
                                function.bhav,
                                m_Obj.Object
                            );
                        }
                    }
                    else
                    {
                        m_Parent.vm.SendCommand(new VMNetInteractionCmd
                        {
                            Interaction = action.ID,
                            CallerID = m_Caller.ObjectID,
                            CalleeID = m_Obj.ObjectID,
                            Param0 = action.Param0
                        });
                    }
                }
                HITVM.Get().PlaySoundEvent(UISounds.QueueAdd);
                m_Parent.ClosePie();
                
            }
        }

        public override void PreDraw(UISpriteBatch batch)
        {
            HeadScene.Draw(GameFacade.GraphicsDevice);
            base.PreDraw(batch);
        }

        public override void Draw(UISpriteBatch batch)
        {
            base.Draw(batch);
            if (m_CurrentItem == m_PieTree)
            {
                DrawLocalTexture(batch, HeadScene.Target, new Vector2(-100, -100));
            } //if we're top level, draw head!
        }
    }

    public class UIPieMenuItem
    {
        public bool Category;
        public byte ID;
        public short Param0;
        public string Name;
        public Dictionary<string, UIPieMenuItem> Children = new Dictionary<string, UIPieMenuItem>();
        public UIPieMenuItem Parent;
    }
}
