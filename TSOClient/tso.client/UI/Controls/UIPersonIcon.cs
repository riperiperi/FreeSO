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

namespace FSO.Client.UI.Controls
{
    public class UIPersonIcon : UIButton
    {
        public VMAvatar Avatar;
        public VM vm;
        public VMTSOAvatarPermissions LastPermissions;

        private Texture2D Overlay;
        private Texture2D Icon;

        public UIPersonIcon(VMAvatar ava, VM vm)
        {
            Avatar = ava;
            this.vm = vm;

            UpdateAvatarState(((VMTSOAvatarState)Avatar.TSOState).Permissions);
            OnButtonClick += CenterPerson;
        }

        private void CenterPerson(UIElement button)
        {
            var worldState = vm.Context.World.State;
            worldState.CenterTile = new Vector2(Avatar.VisualPosition.X, Avatar.VisualPosition.Y);
            worldState.Level = Avatar.Position.Level;

            worldState.CenterTile -= (Avatar.Position.Level-1)* worldState.WorldSpace.GetTileFromScreen(new Vector2(0, 230))/(1<<(3-(int)worldState.Zoom));
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
            var perm = ((VMTSOAvatarState)Avatar.TSOState).Permissions;
            if (perm != LastPermissions)
            {
                UpdateAvatarState(perm);
            }
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
                case VMTSOAvatarPermissions.Admin:
                case VMTSOAvatarPermissions.Owner: bgID = 0x25200000001; overlayID = 0x7A000000001; break; //..., personbuttonoverlay_houseleaderlarge
            }

            if (Avatar.PersistID < 65536)
            {
                bgID = 0xCEF00000001; //peoplebuttontemplate_npclarge
            }
            /*if (Avatar.PersistID == vm.MyUID)
            {
                bgID = 0x25000000001; //personbuttontemplate_playerlarge
            }*/

            Texture = GetTexture(bgID);
            Icon = Avatar.GetIcon(GameFacade.GraphicsDevice, 0);
            if (Icon == null) Icon = GetTexture(0x79500000001); //personbuttontemplate_defaultthumbnail
            Overlay = (overlayID == 0)?null:GetTexture(overlayID);

            Tooltip = GetAvatarString(Avatar);
        }

        private string GetAvatarString(VMAvatar ava)
        {
            int prefixNum = 3;
            if (ava.IsPet) prefixNum = 5;
            else if (ava.PersistID < 65536) prefixNum = 4;
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
                if (Icon.Width <= 45)
                {
                    DrawLocalTexture(SBatch, Icon, new Rectangle(0, 0, Icon.Width, Icon.Height), new Vector2(2, 2), new Vector2(30f / Icon.Width, 30f / Icon.Height));
                }
                else DrawLocalTexture(SBatch, Icon, new Rectangle(0, 0, Icon.Width / 2, Icon.Height), new Vector2(2, 2), new Vector2(30f / (Icon.Width/2), 30f / Icon.Height));
            }
            if (Overlay != null) DrawLocalTexture(SBatch, Overlay, new Vector2());
            //draw the icon over the button
        }
    }
}
