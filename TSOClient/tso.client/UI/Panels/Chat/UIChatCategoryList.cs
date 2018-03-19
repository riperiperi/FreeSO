using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.SimAntics;
using FSO.SimAntics.Model.TSOPlatform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Common.Rendering.Framework.Model;
using FSO.SimAntics.NetPlay.Model.Commands;

namespace FSO.Client.UI.Panels.Chat
{
    public class UIChatCategoryList : UIContainer
    {
        public UIChatDialog Dialog;
        public bool HasButtons;
        public VMTSOAvatarPermissions LastPerm;
        public bool EditMode;
        public List<VMTSOChatChannel> LastChannels = new List<VMTSOChatChannel>();

        public UIChatCategoryList(UIChatDialog dialog)
        {
            Dialog = dialog;

            //init language for default channels
            VMTSOChatChannel.MainChannel.Name = GameFacade.Strings.GetString("f113", "9");
            VMTSOChatChannel.MainChannel.Description = GameFacade.Strings.GetString("f113", "10");
            VMTSOChatChannel.AdminChannel.Name = GameFacade.Strings.GetString("f113", "11");
            VMTSOChatChannel.AdminChannel.Description = GameFacade.Strings.GetString("f113", "12");

            Populate();
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
            var dirty = false;
            var owner = ((Dialog.Owner.ActiveEntity as VMAvatar)?.AvatarState?.Permissions ?? VMTSOAvatarPermissions.Visitor);

            if (owner != LastPerm) dirty = true;
            if (!dirty && Dialog.Owner.vm.Ready)
            {
                var channels = Dialog.Owner.vm.TSOState.ChatChannels;
                if (channels.Count != LastChannels.Count) dirty = true;
                else {
                    for (int i = 0; i < LastChannels.Count; i++)
                    {
                        if (channels[i] != LastChannels[i])
                        {
                            dirty = true;
                            break;
                        }
                    }
                }
            }

            if (dirty) Populate();
        }

        public void Populate()
        {
            var childCopy = new List<UIElement>(Children);
            foreach (var child in childCopy)
                Remove(child);
            var channels = new List<VMTSOChatChannel>();
            var perm = ((Dialog.Owner.ActiveEntity as VMAvatar)?.AvatarState?.Permissions ?? VMTSOAvatarPermissions.Visitor);

            var ui = Content.Content.Get().CustomUI;
            var btnTex = ui.Get("chat_cat.png").Get(GameFacade.GraphicsDevice);

            channels.Add(VMTSOChatChannel.MainChannel);
            channels.AddRange(Dialog.Owner.vm.TSOState.ChatChannels);
            if (perm == VMTSOAvatarPermissions.Admin)
                channels.Add(VMTSOChatChannel.AdminChannel);

            HasButtons = channels.Count(x => perm <= x.ViewPermMin) > 1 || (perm >= VMTSOAvatarPermissions.Owner);
            if (!HasButtons)
            {
                LastChannels = new List<VMTSOChatChannel>(Dialog.Owner.vm.TSOState.ChatChannels);
                LastPerm = perm;
                Invalidate();
                return;
            }
            var btnCaption = TextStyle.DefaultLabel.Clone();
            btnCaption.Size = 8;
            btnCaption.Shadow = true;

            var active = Dialog.Owner.ChatPanel.ActiveChannel;
            var xPos = 0;
            foreach (var channel in channels)
            {
                if (perm < channel.ViewPermMin) continue;
                var btn = new UIButton(btnTex);
                if (!EditMode)
                {
                    btn.Selected = (channel.ID == active);
                    if ((Dialog.ShowChannels & (1 << channel.ID)) == 0) btn.ForceState = 3;
                }
                if (EditMode && channel.ID == 0)
                {
                    btn.Tooltip = GameFacade.Strings.GetString("f113", "18");
                    btn.Disabled = true;
                }
                else
                {
                    btn.Tooltip = (EditMode) ? GameFacade.Strings.GetString("f113", "19") : channel.Description;
                }

                btn.Caption = channel.Name;
                btn.CaptionStyle = btnCaption.Clone();
                btn.CaptionStyle.Color = channel.TextColor;
                btn.OnButtonClick += (btn2) => ChannelSelect(channel);

                btn.X = xPos;
                xPos += (int)btn.Width + 1;
                Add(btn);
            }

            if (EditMode)
            {
                var btn2 = new UIButton(btnTex);
                btn2.Caption = GameFacade.Strings.GetString("f113", "14");
                btn2.Tooltip = GameFacade.Strings.GetString("f113", "16");
                btn2.CaptionStyle = btnCaption;
                btn2.OnButtonClick += NewButton;

                btn2.X = xPos;

                if (channels.Count(x => x.ID < 7) < 5)
                {
                    xPos += (int)btn2.Width + 1;
                    Add(btn2);
                }
                
                btn2 = new UIButton(btnTex);
                btn2.Caption = GameFacade.Strings.GetString("f113", "15");
                btn2.Tooltip = GameFacade.Strings.GetString("f113", "17");
                btn2.CaptionStyle = btnCaption;
                btn2.OnButtonClick += CancelEditButton;

                btn2.X = xPos;
                xPos += (int)btn2.Width + 1;
                Add(btn2);
            }
            else if (perm >= VMTSOAvatarPermissions.Owner)
            {
                var btn2 = new UIButton(btnTex);
                btn2.Caption = GameFacade.Strings.GetString("f113", "13");
                btn2.Tooltip = GameFacade.Strings.GetString("f113", "20");
                btn2.CaptionStyle = btnCaption;
                btn2.OnButtonClick += EditButton;

                btn2.X = xPos;
                xPos += (int)btn2.Width + 1;
                Add(btn2);
            }

            LastPerm = perm;
            LastChannels = new List<VMTSOChatChannel>(Dialog.Owner.vm.TSOState.ChatChannels);
            Invalidate();
        }

        private void EditButton(UIElement button)
        {
            EditMode = true;
            Populate();
        }

        private void CancelEditButton(UIElement button)
        {
            EditMode = false;
            Populate();
        }

        private void NewButton(UIElement button)
        {
            //make a new chat channel with a free id
            var channels = Dialog.Owner.vm.TSOState.ChatChannels;
            if (channels.Count >= 4) return;
            int freeID = 0;
            int i = 0;
            foreach (var chan in channels.OrderBy(x => x.ID))
            {
                if (freeID + 1 + (i++) < chan.ID)
                {
                    freeID = chan.ID;
                    break;
                }
            }
            if (freeID == 0) freeID = channels.Count + 1;

            var dialog = new UIChatCategoryDialog(new VMTSOChatChannel {
                ID = (byte)freeID,
                Name = GameFacade.Strings.GetString("f113", "41"),
                Description = GameFacade.Strings.GetString("f113", "42")
            }, true);
            UIScreen.GlobalShowDialog(dialog, true);
            dialog.OKButton.OnButtonClick += (btn) =>
            {
                Dialog.Owner.vm.SendCommand(new VMNetChatEditChanCmd()
                {
                    Channel = dialog.Channel
                });
            };
        }

        private void ChannelSelect(VMTSOChatChannel chan)
        {
            if (EditMode)
            {
                var dialog = new UIChatCategoryDialog(chan.Clone(), false);
                UIScreen.GlobalShowDialog(dialog, true);

                dialog.OKButton.OnButtonClick += (btn) =>
                {
                    Dialog.Owner.vm.SendCommand(new VMNetChatEditChanCmd()
                    {
                        Channel = dialog.Channel
                    });
                };

                dialog.OnDelete += () =>
                {
                    Dialog.Owner.vm.SendCommand(new VMNetChatEditChanCmd()
                    {
                        Channel = dialog.Channel
                    });
                };
            }
            else
            {
                var active = Dialog.Owner.ChatPanel.ActiveChannel;
                if (chan.ID == active)
                {
                    //they're toggling this chat to invisible
                    Dialog.ShowChannels &= (byte)(~(1<<chan.ID));
                    if (Dialog.ShowChannels != 0)
                    {
                        for (int i=0; i<5; i++)
                        {
                            if ((Dialog.ShowChannels & (1<<i)) > 0)
                            {
                                Dialog.Owner.ChatPanel.ActiveChannel = i;
                                break;
                            }
                        }
                    }
                    else
                    {
                        Dialog.Owner.ChatPanel.ActiveChannel = 0;
                    }
                }
                else
                {
                    //setting this chat as default, and showing it.
                    Dialog.ShowChannels |= (byte)(1 << chan.ID);
                    Dialog.Owner.ChatPanel.ActiveChannel = chan.ID;
                }
                if (Dialog.ShowChannels == 0) Dialog.ShowChannels = 1;
                Dialog.RenderEvents();
                Populate();
            }
        }
    }
}
