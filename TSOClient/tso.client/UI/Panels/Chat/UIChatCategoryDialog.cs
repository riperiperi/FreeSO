using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.SimAntics.Model.TSOPlatform;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Panels.Chat
{
    public class UIChatCategoryDialog : UIDialog
    {
        public VMTSOChatChannel Channel;
        public UITextBox NameEdit;
        public UITextEdit DescEdit;
        public Action OnDelete;

        public UIChatCategoryDialog(VMTSOChatChannel cat, bool isNew) : base(UIDialogStyle.OK | UIDialogStyle.Close, true)
        {
            Channel = cat;
            Caption = GameFacade.Strings.GetString("f113", "21") + cat.ID;
            var topVbox = new UIVBoxContainer();

            var nameLabel = new UILabel();
            nameLabel.Caption = GameFacade.Strings.GetString("f113", "22");
            topVbox.Add(nameLabel);

            NameEdit = new UITextBox();
            NameEdit.SetSize(200, 25);
            NameEdit.CurrentText = cat.Name;
            NameEdit.MaxChars = 8;
            topVbox.Add(NameEdit);

            var descLabel = new UILabel();
            descLabel.Caption = GameFacade.Strings.GetString("f113", "23");
            topVbox.Add(descLabel);

            DescEdit = new UITextEdit();
            DescEdit.BackgroundTextureReference = UITextBox.StandardBackground;
            DescEdit.TextMargin = new Rectangle(8, 2, 8, 3);
            DescEdit.SetSize(400, 100);
            DescEdit.CurrentText = cat.Description;
            DescEdit.MaxChars = 256;
            DescEdit.MaxLines = 5;
            topVbox.Add(DescEdit);

            var flagLabel = new UILabel();
            flagLabel.Caption = GameFacade.Strings.GetString("f113", "24");
            topVbox.Add(flagLabel);

            var flagbox = new UIHBoxContainer();

            for (var i = 0; i < 2; i++)
            {
                var caption = GameFacade.Strings.GetString("f113", (25+i).ToString());
                var check = new UIButton(GetTexture(0x0000083600000001));
                check.Tooltip = caption;
                var flag = (VMTSOChatChannelFlags)(1 << i);
                check.OnButtonClick += x => {
                    check.Selected = !check.Selected;
                    cat.Flags ^= flag;
                };
                check.Selected = (cat.Flags & flag) > 0;

                flagbox.Add(check);

                flagbox.Add(new UILabel
                {
                    Caption = caption
                });
            }
            topVbox.Add(flagbox);
            flagbox.AutoSize();

            Add(topVbox);
            topVbox.AutoSize();
            topVbox.Position = new Vector2(20, 35);

            UIVBoxContainer before = null;
            for (int j = 0; j < 2; j++) {
                var vbox = new UIVBoxContainer();
                vbox.Add(new UILabel
                {
                    Caption = (j == 0) ? GameFacade.Strings.GetString("f113", "35") : GameFacade.Strings.GetString("f113", "36")
                });
                var viewMin = (j == 0);
                for (uint i = 0; i < 4; i++)
                {
                    var hbox = new UIHBoxContainer();
                    var radio = new UIRadioButton();
                    radio.RadioGroup = (j == 0) ? "viewPerm" : "showPerm";
                    radio.RadioData = (VMTSOAvatarPermissions)i;
                    radio.Selected = (viewMin)?(i == (int)cat.ViewPermMin): (i == (int)cat.SendPermMin);
                    radio.Tooltip = GameFacade.Strings.GetString("f113", (37 + i).ToString());
                    radio.OnButtonClick += (btn) =>
                    {
                        if (viewMin)
                        {
                            cat.ViewPermMin = (VMTSOAvatarPermissions)radio.RadioData;
                        }
                        else
                        {
                            cat.SendPermMin = (VMTSOAvatarPermissions)radio.RadioData;
                        }
                    };

                    hbox.Add(radio);
                    hbox.Add(new UILabel
                    {
                        Caption = GameFacade.Strings.GetString("f113", (37+i).ToString())
                    });
                    vbox.Add(hbox);
                }
                before = vbox;
                vbox.Position = new Vector2(20 + j*200, topVbox.Size.Y + 50);
                Add(vbox);
                vbox.AutoSize();
            }

            var buttonsHbox = new UIHBoxContainer();
            if (!isNew) {
                var deleteButton = new UIButton();
                deleteButton.Caption = GameFacade.Strings.GetString("f113", "33");
                deleteButton.OnButtonClick += (btn) =>
                {
                    UIScreen.RemoveDialog(this);
                    Channel.Flags |= VMTSOChatChannelFlags.Delete;
                    OnDelete();
                };
                buttonsHbox.Add(deleteButton);
            }

            var setColorButton = new UIButton();
            setColorButton.Caption = GameFacade.Strings.GetString("f113", "34");
            setColorButton.CaptionStyle = setColorButton.CaptionStyle.Clone();
            setColorButton.CaptionStyle.Color = cat.TextColor;
            setColorButton.CaptionStyle.Shadow = true;
            setColorButton.OnButtonClick += (btn) =>
            {
                UIAlert alert = null;
                alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
                {
                    Title = "",
                    Message = GameFacade.Strings.GetString("f113", "8"),
                    Color = true,
                    Buttons = new UIAlertButton[]
                    {
                        new UIAlertButton(UIAlertButtonType.OK, (btn2) => {
                            //set the color
                            var col = int.Parse(alert.ResponseText);
                            cat.TextColor = new Color(col>>16, (byte)(col>>8), (byte)col);
                            setColorButton.CaptionStyle.Color = cat.TextColor;
                            setColorButton.Invalidate();
                            UIScreen.RemoveDialog(alert);
                        }),
                        new UIAlertButton(UIAlertButtonType.Cancel)
                    }
                }, true);
            };
            buttonsHbox.Add(setColorButton);
            buttonsHbox.AutoSize();
            buttonsHbox.Position = new Vector2((440 - buttonsHbox.Size.X) / 2, topVbox.Size.Y + before.Size.Y + 65);
            Add(buttonsHbox);

            SetSize(440, (int)(topVbox.Size.Y + before.Size.Y + 115));

            CloseButton.OnButtonClick += (btn) =>
            {
                UIScreen.RemoveDialog(this);
            };
            
            OKButton.OnButtonClick += (btn) =>
            {
                Channel.Name = NameEdit.CurrentText;
                Channel.Description = DescEdit.CurrentText;
                UIScreen.RemoveDialog(this);
            };
        }
    }
}
