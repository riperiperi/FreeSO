using FSO.Client.UI.Framework;
using FSO.SimAntics;
using FSO.SimAntics.Entities;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Common.Rendering.Framework.Model;
using FSO.LotView;
using FSO.Client;
using FSO.SimAntics.NetPlay.Drivers;
using FSO.SimAntics.Model;
using FSO.LotView.Model;
using FSO.LotView.Components;
using Microsoft.Xna.Framework;
using FSO.SimAntics.Engine.TSOTransaction;

namespace FSO.IDE.Common
{
    public class UIThumbnailRenderer : UIContainer
    {
        private Texture2D Thumb;
        private VM TempVM;
        private VMMultitileGroup TargetOBJ;

        private uint GUID;
        private uint oldGUID;

        public UIThumbnailRenderer(uint id)
        {
            GUID = id;
        }

        private void UpdateThumb()
        {
            if (Thumb != null)
            {
                Thumb.Dispose();
                Thumb = null;
            }
            if (TargetOBJ == null) return;
            var objects = TargetOBJ.Objects;
            ObjectComponent[] objComps = new ObjectComponent[objects.Count];
            for (int i = 0; i < objects.Count; i++)
            {
                objComps[i] = (ObjectComponent)objects[i].WorldUI;
            }
            Thumb = TempVM.Context.World.GetObjectThumb(objComps, TargetOBJ.GetBasePositions(), GameFacade.GraphicsDevice);
        }

        public void SetGUIDLocal(uint id)
        {
            oldGUID = id;
            GUID = id;
            if (TempVM != null)
            {
                if (TargetOBJ != null) TargetOBJ.Delete(TempVM.Context);
                TargetOBJ = TempVM.Context.CreateObjectInstance(GUID, LotTilePos.OUT_OF_WORLD, Direction.NORTH, true);
                if (TargetOBJ != null && TargetOBJ.BaseObject is VMGameObject) UpdateThumb();
                else Thumb = null;
            }
        }

        public void SetGUID(uint id)
        {
            GUID = id;
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
            if (TempVM == null && GUID != 0)
            {
                var world = new ExternalWorld(GameFacade.GraphicsDevice);
                world.Initialize(GameFacade.Scenes);
                var context = new VMContext(world);

                TempVM = new VM(context, new VMServerDriver(new VMTSOGlobalLinkStub()), new VMNullHeadlineProvider());
                TempVM.Init();

                var blueprint = new Blueprint(1, 1);
                blueprint.Light = new RoomLighting[]
{
                    new RoomLighting() { OutsideLight = 100 },
                    new RoomLighting() { OutsideLight = 100 },
                    new RoomLighting() { OutsideLight = 100 },
};
                blueprint.OutsideColor = Color.White;
                blueprint.GenerateRoomLights();
                blueprint.RoomColors[2].A /= 2;
                world.State._2D.AmbientLight.SetData(blueprint.RoomColors);

                world.InitBlueprint(blueprint);
                context.Blueprint = blueprint;
                context.Architecture = new VMArchitecture(1, 1, blueprint, TempVM.Context);
            }

            if (GUID != oldGUID)
            {
                SetGUIDLocal(GUID);
                state.SharedData["ExternalDraw"] = true;
            }
        }

        public override void Draw(UISpriteBatch batch)
        {
            base.Draw(batch);
            if (Thumb != null)
            {
                Vector2 size = new Vector2(GameFacade.GraphicsDevice.Viewport.Width, GameFacade.GraphicsDevice.Viewport.Height);
                float minSize = Math.Min(size.X, size.Y);

                float scale = Math.Min(1, minSize / Math.Max(Thumb.Height, Thumb.Width));

                DrawLocalTexture(batch, Thumb, null, new Vector2(size.X / 2 - (Thumb.Width * scale / 2), size.Y / 2 - (Thumb.Height * scale / 2)), new Vector2(scale, scale));
            }
        }

    }
}
