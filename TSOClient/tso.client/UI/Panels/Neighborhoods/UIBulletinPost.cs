using FSO.Client.Controllers;
using FSO.Client.Controllers.Panels;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Common;
using FSO.Common.Rendering.Framework.Model;
using FSO.Files.Formats.tsodata;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Panels.Neighborhoods
{
    public class UIBulletinPost : UICachedContainer
    {
        public UITextEdit TitleEdit;
        public UILabel TitlePlaceholder;
        public UIButton BackButton;
        public UIButton MiddleButton; //Mod Delete (with ban prompt)
        public UIButton RightButton; //POST/Delete/Promote
        public UITextEdit BodyText;
        public UILabel TimeLabel;
        public UILabel TypeLabel;

        public UIImage PropertyButtonBG;
        public UIImage PersonButtonBG;

        public UILabel PropertyButtonName;
        public UILabel PersonButtonName;

        public UILotThumbButtonAuto LotThumbButton;
        public UIBigPersonButton PersonButton;

        public BulletinItem ActiveItem;

        public Action OnBack;
        public bool EditorMode;
        public bool AcceptSelections;
        private TextStyle BaseTitleStyle;

        public bool IsMayor;

        public UIBulletinPost()
        {
            var ui = Content.Content.Get().CustomUI;
            var gd = GameFacade.GraphicsDevice;

            var bigCaption = TextStyle.DefaultLabel.Clone();
            bigCaption.Color = Color.White;
            bigCaption.Shadow = true;
            bigCaption.Size = 28;
            BaseTitleStyle = bigCaption.Clone();
            Add(TitleEdit = new UITextEdit()
            {
                Position = new Vector2(27, 40),
                Size = new Vector2(542, 44),
                TextStyle = bigCaption,
                Alignment = TextAlignment.Middle | TextAlignment.Center,
                CurrentText = "",
                MaxChars = 64,
                FlashOnEmpty = true
            });

            var semiT = bigCaption.Clone();
            semiT.Color *= 0.6f;
            semiT.Shadow = false;
            Add(TitlePlaceholder = new UILabel()
            {
                Position = new Vector2(27, 40),
                Size = new Vector2(542, 44),
                CaptionStyle = semiT,
                Alignment = TextAlignment.Middle | TextAlignment.Center,
                Caption = GameFacade.Strings.GetString("f120", "23")
            });

            BackButton = new UIButton(ui.Get("vote_big_btn.png").Get(gd));
            BackButton.Width = 150;
            BackButton.Caption = GameFacade.Strings.GetString("f120", "13");
            BackButton.Tooltip = GameFacade.Strings.GetString("f120", "15");
            BackButton.CaptionStyle = BackButton.CaptionStyle.Clone();
            BackButton.CaptionStyle.Color = Color.White;
            BackButton.CaptionStyle.Shadow = true;
            BackButton.CaptionStyle.Size = 22;
            BackButton.Position = new Vector2(30, 481);
            Add(BackButton);

            BackButton.OnButtonClick += GoBack;

            MiddleButton = new UIButton(ui.Get("vote_big_btn.png").Get(gd));
            MiddleButton.Width = 150;
            MiddleButton.Caption = GameFacade.Strings.GetString("f120", "35");
            MiddleButton.CaptionStyle = BackButton.CaptionStyle.Clone();
            MiddleButton.CaptionStyle.Color = Color.White;
            MiddleButton.CaptionStyle.Shadow = true;
            MiddleButton.CaptionStyle.Size = 22;
            MiddleButton.Position = new Vector2(220, 481);
            Add(MiddleButton);

            MiddleButton.OnButtonClick += MButtonClick;

            RightButton = new UIButton(ui.Get("vote_big_btn.png").Get(gd));
            RightButton.Width = 150;
            RightButton.Caption = GameFacade.Strings.GetString("f120", "12");
            RightButton.CaptionStyle = BackButton.CaptionStyle.Clone();
            RightButton.CaptionStyle.Color = Color.White;
            RightButton.CaptionStyle.Shadow = true;
            RightButton.CaptionStyle.Size = 22;
            RightButton.Position = new Vector2(410, 481);
            Add(RightButton);

            RightButton.OnButtonClick += RButtonClick;

            BodyText = new UITextEdit();
            BodyText.BackgroundTextureReference = UITextBox.StandardBackground;
            BodyText.TextMargin = new Rectangle(8, 3, 8, 3);
            BodyText.FlashOnEmpty = true;
            BodyText.MaxChars = 1000;
            BodyText.ScrollbarGutter = 7;
            BodyText.TextMargin = new Rectangle(12, 10, 12, 10);
            BodyText.SetSize(388, 346);
            BodyText.Position = new Vector2(22, 96);
            Add(BodyText);
            BodyText.ScrollbarImage = GetTexture(0x4AB00000001);
            BodyText.InitDefaultSlider();
            BodyText.OnChange += BodyText_OnChange;

            var whiteText = TextStyle.DefaultLabel.Clone();
            whiteText.Color = Color.White;
            whiteText.Shadow = true;

            Add(TimeLabel = new UILabel()
            {
                Position = new Vector2(34, 442),
                CaptionStyle = whiteText
            });

            Add(TypeLabel = new UILabel()
            {
                Position = new Vector2(34, 442),
                Size = new Vector2(388-24, 1),
                Alignment = TextAlignment.Right | TextAlignment.Top,
                CaptionStyle = whiteText
            });


            Add(PropertyButtonBG = new UIImage(ui.Get("bulletin_post_lot_bg.png").Get(gd))
            {
                Position = new Vector2(440, 101)
            });

            Add(PersonButtonBG = new UIImage(ui.Get("bulletin_post_ava_bg.png").Get(gd))
            {
                Position = new Vector2(449, 266-23)
            });

            Add(LotThumbButton = new UILotThumbButtonAuto()
            {
                Position = new Vector2(446, 107)
            });
            LotThumbButton.OnNameChange += (id, name) =>
            {
                if (id == 0)
                    PropertyButtonName.Caption = GameFacade.Strings.GetString("f120", "28");
                else
                    PropertyButtonName.Caption = name;
                if (EditorMode) LotThumbButton.LotTooltip = GameFacade.Strings.GetString("f120", "29");
            };
            LotThumbButton.OnLotClick += PropertyButtonClick;
            LotThumbButton.Init(GetTexture(0x0000079300000001), GetTexture(0x0000079300000001));

            DynamicOverlay.Add(PersonButton = new UIBigPersonButton()
            {
                Position = new Vector2(452, 269 - 23)
            });

            PersonButton.OnNameChange += (id, name) =>
            {
                PersonButtonName.Caption = name;
            };

            Add(PropertyButtonName = new UILabel()
            {
                Position = new Vector2(435, 202),
                Size = new Vector2(151, 1),
                Alignment = TextAlignment.Center | TextAlignment.Top,
                CaptionStyle = whiteText,
                Caption = "",
                Wrapped = true,
                MaxLines = 4
            });

            Add(PersonButtonName = new UILabel()
            {
                Position = new Vector2(435, 442 - 23),
                Size = new Vector2(151, 1),
                Alignment = TextAlignment.Center | TextAlignment.Top,
                CaptionStyle = whiteText,
                Caption = "",
                Wrapped = true,
                MaxLines = 3
            });

            Size = new Vector2(600, 550);
        }

        public void Lock()
        {
            AcceptSelections = false;
        }

        public void Unlock()
        {
            AcceptSelections = true;
        }

        private void MButtonClick(UIElement button)
        {
            var controller = FindController<BulletinDialogController>();
            //delete
            UIAlert.YesNo("", GameFacade.Strings.GetString("f120", "21"), true, (answer) =>
            {
                if (answer)
                {
                    //delete the message!
                    controller.Delete(ActiveItem.ID);
                }
            });
        }

        private void RButtonClick(UIElement button)
        {
            if (AcceptSelections && Opacity == 1)
            {
                var controller = FindController<BulletinDialogController>();
                if (EditorMode)
                {
                    UIAlert.YesNo(GameFacade.Strings.GetString("f120", "33"), GameFacade.Strings.GetString("f120", "34"), true, (answer) =>
                    {
                        if (answer)
                        {
                            //send the message!
                            controller.MakePost(TitleEdit.CurrentText, BodyText.CurrentText, LotThumbButton.LotId, false);
                        }
                    });
                }
                else
                {
                    //delete or promote
                    if (FindController<CoreGameScreenController>()?.IsMe(LotThumbButton.LotId) ?? false)
                    {
                        //delete
                        UIAlert.YesNo("", GameFacade.Strings.GetString("f120", "21"), true, (answer) =>
                        {
                            if (answer)
                            {
                                //delete the message!
                                controller.Delete(ActiveItem.ID);
                            }
                        });
                    }
                    else
                    {
                        //promote (should only be visible if post is community and player is mayor)
                        UIAlert.YesNo("", GameFacade.Strings.GetString("f120", "20"), true, (answer) =>
                        {
                            if (answer)
                            {
                                //promote the message!
                                controller.Promote(ActiveItem.ID);
                            }
                        });
                    }
                }
            }
        }

        private void PropertyButtonClick(UIElement button)
        {
            if (EditorMode) SelectLot();
            else FindController<CoreGameScreenController>()?.ShowLotPage(LotThumbButton.LotId);
        }

        private void SelectLot()
        {
            var lotCont = new UIPropertySelectContainer();
            UIAlert alert = null;
            alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                Title = GameFacade.Strings.GetString("f120", "30"),
                Message = GameFacade.Strings.GetString("f120", "31"),
                Width = 440,
                GenericAddition = lotCont,
                GenericAdditionDynamic = true,
                Buttons = new UIAlertButton[] {
                    new UIAlertButton(UIAlertButtonType.OK, (btn2) => {
                        var lotID = lotCont.SelectedLot;
                        LotThumbButton.LotId = lotID;
                        UIScreen.RemoveDialog(alert);
                    }),
                    new UIAlertButton(UIAlertButtonType.Cancel, (btn2) => {
                        UIScreen.RemoveDialog(alert);
                    })
                }
            }, true);
        }

        private void BodyText_OnChange(UIElement element)
        {
            if (EditorMode)
            {
                TimeLabel.Caption = BodyText.CurrentText.Length + "/1000";
            }
        }

        private void GoBack(UIElement button)
        {
            if (AcceptSelections && Opacity == 1)
            {
                if (EditorMode && (TitleEdit.CurrentText.Length > 0 || BodyText.CurrentText.Length > 0))
                {
                    UIAlert.YesNo("", GameFacade.Strings.GetString("f120", "19"), true, (answer) =>
                    {
                        if (answer)
                        {
                            OnBack?.Invoke();
                        }
                    });
                }
                else
                {
                    OnBack?.Invoke();
                }
            }
        }

        public void SetPost(BulletinItem item)
        {
            ActiveItem = item;
            if (item == null)
            {
                EditorMode = true;
                TitleEdit.CurrentText = "";
                TitleEdit.Mode = UITextEditMode.Editor;
                BodyText.CurrentText = "";
                BodyText.Mode = UITextEditMode.Editor;
                TimeLabel.Caption = "0/1000";
                TypeLabel.Caption = "";
                RightButton.Caption = GameFacade.Strings.GetString("f120", "12");
                RightButton.Visible = true;

                LotThumbButton.Visible = true;
                LotThumbButton.LotId = 0;
                PropertyButtonName.Visible = true;
                PropertyButtonName.Caption = GameFacade.Strings.GetString("f120", "28");
                LotThumbButton.LotTooltip = GameFacade.Strings.GetString("f120", "29");
                PropertyButtonBG.Visible = true;

                PersonButton.Visible = true;
                PersonButton.AvatarId = FindController<CoreGameScreenController>()?.MyID() ?? 0;
                PersonButtonName.Visible = true;
                PersonButtonBG.Visible = true;

                MiddleButton.Visible = false;
            }
            else
            {
                EditorMode = false;
                TitleEdit.CurrentText = item.Subject;
                TitleEdit.Mode = UITextEditMode.ReadOnly;
                BodyText.CurrentText = item.Body;
                BodyText.Mode = UITextEditMode.ReadOnly;
                var time = ClientEpoch.ToDate((uint)item.Time).ToLocalTime();
                TimeLabel.Caption = time.ToShortTimeString() + " " + time.ToShortDateString();

                switch (item.Type)
                {
                    case BulletinType.Community:
                        TypeLabel.Caption = GameFacade.Strings.GetString("f120", "24");
                        break;
                    case BulletinType.System:
                        TypeLabel.Caption = GameFacade.Strings.GetString("f120", "25");
                        break;
                    case BulletinType.Mayor:
                        if ((item.Flags & BulletinFlags.PromotedByMayor) > 0) TypeLabel.Caption = GameFacade.Strings.GetString("f120", "27");
                        else TypeLabel.Caption = GameFacade.Strings.GetString("f120", "26");
                        break;
                }

                var hasLot = item.LotID != 0;
                var hasPerson = item.SenderID != 0 && item.SenderID != 0xFFFFFFFF;

                PropertyButtonBG.Visible = hasLot;
                PropertyButtonName.Visible = hasLot;
                LotThumbButton.Visible = hasLot;

                PersonButton.Visible = hasPerson;
                PersonButtonBG.Visible = hasPerson;
                PersonButtonName.Visible = hasPerson;

                LotThumbButton.LotId = item.LotID;
                PersonButton.AvatarId = item.SenderID;

                var canPromote = IsMayor && item.Type == BulletinType.Community;
                var myPost = FindController<CoreGameScreenController>()?.IsMe(item.SenderID) ?? false;
                var admin = GameFacade.EnableMod;

                RightButton.Visible = true;
                if (canPromote)
                    RightButton.Caption = GameFacade.Strings.GetString("f120", "14");
                else
                {
                    if (myPost) RightButton.Caption = GameFacade.Strings.GetString("f120", "35");
                    else RightButton.Visible = false;
                }

                MiddleButton.Caption = GameFacade.Strings.GetString("f120", "35");
                MiddleButton.Visible = !myPost && GameFacade.EnableMod;
            }
        }

        private float LastOpacity;
        private string LastMessage;
        public override void Update(UpdateState state)
        {
            if (Opacity != LastOpacity)
            {
                Invalidate();
                Parent.Invalidate();
                LastOpacity = Opacity;
            }

            var empty = TitleEdit.CurrentText.Length == 0;
            if (empty != TitlePlaceholder.Visible)
            {
                TitlePlaceholder.Visible = empty;
                Invalidate();
            }
            base.Update(state);

            if (TitleEdit.CurrentText != LastMessage)
            {
                var width = BaseTitleStyle.MeasureString(TitleEdit.CurrentText).X;
                if (width >= TitleEdit.Width)
                {
                    TitleEdit.TextStyle.Size = (int)(28 * TitleEdit.Width / width);
                    TitleEdit.ForceDirty();
                }
                else if (TitleEdit.TextStyle.Size != 28)
                {
                    TitleEdit.TextStyle.Size = 28;
                    TitleEdit.ForceDirty();
                }
                LastMessage = TitleEdit.CurrentText;
            }

            RightButton.Disabled = EditorMode && (TitleEdit.CurrentText.Length == 0 || BodyText.CurrentText.Length == 0);
        }

        public void Fade(float opacity)
        {
            /*
            foreach (var child in GetChildren())
            {
                GameFacade.Screens.Tween.To(child, 0.66f, new Dictionary<string, float>() { { "Opacity", opacity } }, (Opacity == 0) ? TweenQuad.EaseIn: TweenQuad.EaseOut);
            }
            */
            foreach (var child in PersonButton.GetChildren())
            {
                GameFacade.Screens.Tween.To(child, 0.66f, new Dictionary<string, float>() { { "Opacity", opacity } }, (Opacity == 0) ? TweenQuad.EaseIn : TweenQuad.EaseOut);
            }
            GameFacade.Screens.Tween.To(PersonButton, 0.66f, new Dictionary<string, float>() { { "Opacity", opacity } }, (Opacity == 0) ? TweenQuad.EaseIn : TweenQuad.EaseOut);
            GameFacade.Screens.Tween.To(this, 0.66f, new Dictionary<string, float>() { { "Opacity", opacity } }, (Opacity == 0) ? TweenQuad.EaseIn : TweenQuad.EaseOut);
        }
    }
}
