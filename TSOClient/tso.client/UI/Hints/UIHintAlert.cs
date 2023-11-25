using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Files;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;

namespace FSO.Client.UI.Hints
{
    public class UIHintAlert : UIAlert
    {
        public List<UIHint> Hints;
        public int HintNumber = 0;
        public string OKText;
        public bool Dead;

        public UIButton PrevButton;
        public UIButton NextButton;
        public UIHint LastHint;
        public UIButton HideHintCheck;
        public UIHBoxContainer HideContainer;

        public UIHintAlert(UIHint hint) : base(new UIAlertOptions()
        {
            Title = "Hint (0 of 0)",
            Width = 700,
            Message = "",
            Buttons = new UIAlertButton[]
            {
                new UIAlertButton(UIAlertButtonType.Cancel, (btn) => { }, "Previous"),
                new UIAlertButton(UIAlertButtonType.OK, (btn) => { }),
            },
            AllowBB = true
        })
        {
            PrevButton = ButtonMap[UIAlertButtonType.Cancel];
            PrevButton.OnButtonClick += (btn) => HintAdvance(-1);
            
            NextButton = ButtonMap[UIAlertButtonType.OK];
            NextButton.OnButtonClick += (btn) => HintAdvance(1);
            OKText = NextButton.Caption;
            Hints = new List<UIHint>() { hint };

            var formField = new UIHBoxContainer();
            formField.X = 20;
            HideContainer = formField;
            Add(formField);

            var check = new UIButton(GetTexture(0x0000083600000001));
            check.Tooltip = "Mark all hints as read. Hints added by future updates will still be shown.";
            check.OnButtonClick += x => {
                check.Selected = !check.Selected;
            };
            HideHintCheck = check;

            formField.Add(check);

            formField.Add(new UILabel
            {
                Caption = "Hide Hints"
            });

            Opacity = 1f;
            RenderHint();
        }

        public void RenderHint()
        {
            RenderHint(Hints[HintNumber]);
        }

        public void RenderHint(UIHint hint)
        {
            Vector2? sizeBefore = null;
            if (LastHint != null) sizeBefore = Size;
            Caption = hint.Title + " (" + (HintNumber+1) + " of " + Hints.Count + ")";
            Body = hint.Body;

            if (hint != LastHint)
            {
                Icon.Texture?.Dispose();

                Icon.Texture = null;
                SetIcon(null, 0, 0);
                if (hint.Image != null && hint.Image != "")
                {
                    //try load the image for this hint
                    try
                    {
                        if (hint.Image.Length > 0 && hint.Image[0] == '@')
                        {

                            using (var strm = File.Open(Content.Content.Get().GetPath("uigraphics/hints/" + hint.Image.Substring(1)), FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                var tex = ImageLoader.FromStream(GameFacade.GraphicsDevice, strm);
                                SetIcon(tex, tex.Width, tex.Height);
                            }
                        }
                        else
                        {
                            using (var strm = File.Open("Content/UI/hints/images/" + hint.Image, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                var tex = ImageLoader.FromStream(GameFacade.GraphicsDevice, strm);
                                SetIcon(tex, tex.Width, tex.Height);
                            }
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
            }
            LastHint = hint;
            
            NextButton.Caption = (HintNumber + 1 == Hints.Count) ? OKText : "Next";
            PrevButton.Disabled = HintNumber == 0;

            if (sizeBefore != null) Position += (sizeBefore.Value - Size) / 2;
            HideContainer.Y = PrevButton.Y + 5;
            HideContainer.AutoSize();
        }

        public override void Removed()
        {
            base.Removed();
            Icon.Texture?.Dispose();
        }

        public void HintAdvance(int dir)
        {
            HintNumber += dir;
            if (HintNumber < 0)
            {
                HintNumber = 0;
                return;
            }
            if (HintNumber == Hints.Count)
            {
                UIScreen.RemoveDialog(this);
                if (HideHintCheck.Selected) FSOFacade.Hints.MarkAllRead();
                Dead = true;
            }
            else
            {
                RenderHint();
            }
        }

        public void AddHint(UIHint hint)
        {
            Hints.Add(hint);
            //updates the buttons and title
            RenderHint();
        }
    }
}
