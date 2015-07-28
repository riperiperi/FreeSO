﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.world.components;
using TSO.Common.rendering.framework.model;
using TSO.Simantics;
using TSO.Simantics.net.model.commands;
using TSOClient.Code.UI.Controls;
using TSOClient.Code.UI.Framework;
using TSOClient.Code.Utils;

namespace TSOClient.Code.UI.Panels
{
    public class UIChatPanel : UIContainer
    {
        private VM vm;
        private TextStyle Style;
        private UITextBox TextBox;

        private int Margin = 25;

        private Texture2D m_SelectionTexture;
        private Color m_SelectionFillColor;
        public Color SelectionFillColor
        {
            get
            {
                return m_SelectionFillColor;
            }
            set
            {
                m_SelectionFillColor = value;
                m_SelectionTexture = TextureUtils.TextureFromColor(GameFacade.GraphicsDevice, value);
            }
        }

        private List<UILabel> Labels;
        private UILotControl Owner;

        private Color[] Colours = new Color[] {
            new Color(255, 255, 255),
            new Color(200, 255, 255),
            new Color(255, 200, 255),
            new Color(255, 255, 200),
            new Color(200, 200, 255),
            new Color(255, 200, 200),
            new Color(200, 255, 200),
            new Color(150, 255, 255),
            new Color(150, 150, 255),
            new Color(150, 150, 150),
            new Color(150, 255, 150),
            new Color(255, 150, 150),
            new Color(255, 255, 150)
        };

        public UIChatPanel(VM vm, UILotControl owner)
        {
            this.vm = vm;
            this.Owner = owner;

            Style = TextStyle.DefaultTitle.Clone();
            Style.Size = 16;
            Style.Shadow = true;
            Labels = new List<UILabel>();

            TextBox = new UITextBox();
            TextBox.Visible = true;
            Add(TextBox);
            TextBox.Position = new Vector2(25, 25);
            TextBox.SetSize(300, 25);

            TextBox.OnEnterPress += SendMessage;

            SelectionFillColor = new Color(0, 25, 70);
        }

        private void SendMessage(UIElement element)
        {
            string message = TextBox.CurrentText;
            message = message.Replace("\r\n", "");
            if (message != "")
            {
                vm.SendCommand(new VMNetChatCmd
                {
                    CallerID = Owner.ActiveEntity.ObjectID,
                    Message = message
                });
            }
            TextBox.Clear();
        }

        public override void Update(UpdateState state)
        {
            if (state.NewKeys.Contains(Keys.Enter))
            {
                if (!TextBox.Visible) TextBox.Clear();
                TextBox.Visible = !TextBox.Visible;
            }

            var avatars = vm.Entities.Where(x => (x is VMAvatar)).ToList();
            while (avatars.Count < Labels.Count)
            {
                Remove(Labels[Labels.Count - 1]);
                Labels.RemoveAt(Labels.Count - 1);
            }
            while (avatars.Count > Labels.Count)
            { 
                var label = new UILabel();
                label.CaptionStyle = Style.Clone();
                label.CaptionStyle.Color = Colours[Labels.Count % Colours.Length];
                label.Alignment = TextAlignment.Center | TextAlignment.Middle;
                Add(label);
                Labels.Add(label);
            }

            for (int i=0; i<Labels.Count; i++)
            {
                var label = Labels[i];
                var avatar = avatars[i];
                label.Caption = ((VMAvatar)avatar).Message;
                label.Position = ((AvatarComponent)avatar.WorldUI).LastScreenPos + new Vector2(0, -175) / (1<<(3-(int)vm.Context.World.State.Zoom));

                TextAlignment alignment = 0;

                if (label.Position.X < Margin)
                {
                    alignment |= TextAlignment.Left;
                    label.Position = new Vector2(Margin, label.Position.Y);
                }
                else if (label.Position.X > GlobalSettings.Default.GraphicsWidth-Margin)
                {
                    alignment |= TextAlignment.Right;
                    label.Position = new Vector2(GlobalSettings.Default.GraphicsWidth-Margin, label.Position.Y);
                } else
                {
                    alignment |= TextAlignment.Center;
                }

                if (label.Position.Y < Margin)
                {
                    alignment |= TextAlignment.Top;
                    label.Position = new Vector2(label.Position.X, Margin);
                }
                else if (label.Position.Y > GlobalSettings.Default.GraphicsHeight - Margin)
                {
                    alignment |= TextAlignment.Bottom;
                    label.Position = new Vector2(label.Position.X, GlobalSettings.Default.GraphicsHeight - Margin);
                } else
                {
                    alignment |= TextAlignment.Middle;
                }
                label.Alignment = alignment;
                label.Size = new Vector2(1, 1);
            }
            base.Update(state);
        }

        public override void Draw(UISpriteBatch batch)
        {
            base.Draw(batch);
        }
    }
}