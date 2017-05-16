using FSO.Client.UI.Framework;
using FSO.SimAntics;
using FSO.SimAntics.Model.TSOPlatform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Common.Rendering.Framework.Model;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using FSO.LotView.Components;
using FSO.Client.UI.Screens;
using FSO.Client.Controllers;

namespace FSO.Client.UI.Controls
{
    public class UIVMPersonButton : UIButton
    {
        public VMAvatar Avatar;
        public VM vm;
        public VMTSOAvatarPermissions LastPermissions;
        public bool Small;

        private Texture2D Overlay;
        private Texture2D Icon;
        private Texture2D Target;
        private bool RMB;

        public UIVMPersonButton(VMAvatar ava, VM vm, bool small)
        {
            Avatar = ava;
            this.vm = vm;

            Small = small;
            UpdateAvatarState(((VMTSOAvatarState)Avatar.TSOState).Permissions);
            OnButtonClick += CenterPerson;
        }

        private void CenterPerson(UIElement button)
        {
            if (RMB)
            {
                vm.Context.World.State.ScrollAnchor = (AvatarComponent)(Avatar?.WorldUI);
            }
            else
            {
                var worldState = vm.Context.World.State;
                worldState.CenterTile = new Vector2(Avatar.VisualPosition.X, Avatar.VisualPosition.Y);
                worldState.ScrollAnchor = null;
                worldState.Level = Avatar.Position.Level;

                worldState.CenterTile -= (Avatar.Position.Level - 1) * worldState.WorldSpace.GetTileFromScreen(new Vector2(0, 230)) / (1 << (3 - (int)worldState.Zoom));

                //try show person page. this assumes that we are in the core game screen.
                if (Avatar.PersistID != 0 && UIScreen.Current is CoreGameScreen)
                {
                    var cg = (CoreGameScreen)UIScreen.Current;
                    cg.PersonPage.FindController<PersonPageController>()?.Show(Avatar.PersistID);
                }
            }
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
            var perm = ((VMTSOAvatarState)Avatar.TSOState).Permissions;
            if (perm != LastPermissions)
            {
                UpdateAvatarState(perm);
            }
            RMB = state.MouseState.RightButton == ButtonState.Pressed;
            if (RMB && Hovered) CenterPerson(this);
        }

        public void UpdateAvatarState(VMTSOAvatarPermissions perm)
        {
            LastPermissions = perm;

            //personbuttontemplate_defaultthumbnail = 0x79500000001,

            ulong bgID = 0;
            ulong overlayID = 0;

            switch (perm)
            {
                case VMTSOAvatarPermissions.Visitor: bgID = 0x25400000001; break; //personbuttontemplate_visitorlarge
                case VMTSOAvatarPermissions.Roommate:
                case VMTSOAvatarPermissions.BuildBuyRoommate: bgID = 0x25200000001; overlayID = 0xB7F00000001; break; //personbuttontemplate_roommatelarge, personbuttonoverlay_roommatelarge
                case VMTSOAvatarPermissions.Admin: bgID = 0x25200000001; overlayID = 0x8B000000001; break;
                case VMTSOAvatarPermissions.Owner: bgID = 0x25200000001; overlayID = 0x7A000000001; break; //..., personbuttonoverlay_houseleaderlarge
            }

            if (Avatar.PersistID == 0)
            {
                bgID = 0xCEF00000001; //peoplebuttontemplate_npclarge
            }

            if (Small)
            {
                bgID += 0x00100000000;
                overlayID += 0x00100000000;
            }

            Texture = GetTexture(bgID);
            Icon = Avatar.GetIcon(GameFacade.GraphicsDevice, 0);
            if (Icon == null) Icon = GetTexture(0x79500000001); //personbuttontemplate_defaultthumbnail
            Overlay = (overlayID == 0)?null:GetTexture(overlayID);
            Target = GetTexture(0x25700000001);

            Tooltip = GetAvatarString(Avatar);
        }

        private string GetAvatarString(VMAvatar ava)
        {
            int prefixNum = 3;
            if (ava.IsPet) prefixNum = 5;
            else if (ava.PersistID == 0) prefixNum = 4;
            else
            {
                var permissionsLevel = ((VMTSOAvatarState)ava.TSOState).Permissions;
                switch (permissionsLevel)
                {
                    case VMTSOAvatarPermissions.Visitor: prefixNum = 3; break;
                    case VMTSOAvatarPermissions.Roommate:
                    case VMTSOAvatarPermissions.BuildBuyRoommate: prefixNum = 2; break;
                    case VMTSOAvatarPermissions.Admin:
                    case VMTSOAvatarPermissions.Owner: prefixNum = 1; break;
                }
            }

            return GameFacade.Strings.GetString("217", prefixNum.ToString()) + ava.ToString();
        }

        public override void Draw(UISpriteBatch SBatch)
        {
            base.Draw(SBatch);

            if (Icon != null)
            {
                var pos = (Small) ? new Vector2(1, 1) : new Vector2(2, 2);
                var targetSize = (Small) ? new Vector2(18, 18) : new Vector2(30, 30);
                if (Icon.Width <= 45)
                {
                    DrawLocalTexture(SBatch, Icon, new Rectangle(0, 0, Icon.Width, Icon.Height), pos, targetSize / new Vector2(Icon.Width, Icon.Height));
                }
                else DrawLocalTexture(SBatch, Icon, new Rectangle(0, 0, Icon.Width / 2, Icon.Height), pos, targetSize / new Vector2((Icon.Width/2), Icon.Height));
            }
            if (Overlay != null)
            {
                DrawLocalTexture(SBatch, Overlay, new Vector2(34-Overlay.Width, 34-Overlay.Height));
            }

            if (Icon != null && vm.Context.World.State.ScrollAnchor == Avatar?.WorldUI)
            {
                DrawLocalTexture(SBatch, Target, new Vector2(Icon.Width-Target.Width, Icon.Height-Target.Height));
            }
            //draw the icon over the button
        }
    }
}
