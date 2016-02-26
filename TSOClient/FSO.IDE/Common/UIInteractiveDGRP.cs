using FSO.Client;
using FSO.Client.UI.Framework;
using FSO.Common.Rendering.Framework.Model;
using FSO.Files.Formats.IFF.Chunks;
using FSO.LotView.Components;
using FSO.LotView.Model;
using FSO.SimAntics;
using FSO.SimAntics.Entities;
using FSO.SimAntics.Model;
using FSO.SimAntics.NetPlay.Drivers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.IDE.Common
{
    public class UIInteractiveDGRP : UIContainer
    {
        private VM TempVM;
        private VMMultitileGroup TargetOBJ;
        protected VMEntity TargetTile;

        private uint GUID;
        private uint oldGUID;

        public UIInteractiveDGRP(uint id)
        {
            GUID = id;
        }

        public void SetGUIDLocal(uint id, VM TempVM)
        {
            oldGUID = id;
            GUID = id;
            if (TempVM != null)
            {
                if (TargetOBJ != null) TargetOBJ.Delete(TempVM.Context);
                //create our master, center camera on target object
                var objDefinition = FSO.Content.Content.Get().WorldObjects.Get(GUID);

                if (objDefinition != null)
                {
                    var masterID = id;
                    var notMaster = objDefinition.OBJ.MasterID != 0 && objDefinition.OBJ.SubIndex != -1;
                    if (notMaster)
                    {
                        //find the master
                        var master = objDefinition.Resource.List<OBJD>().FirstOrDefault(x => x.MasterID == objDefinition.OBJ.MasterID && x.SubIndex == -1);
                        if (master != null) masterID = master.GUID;
                    }
                    TargetOBJ = TempVM.Context.CreateObjectInstance(masterID, LotTilePos.OUT_OF_WORLD, Direction.SOUTH, true);
                    TargetOBJ.SetVisualPosition(new Vector3(0.5f, 0.5f, 0f), Direction.SOUTH, TempVM.Context);
                    TempVM.Entities = TargetOBJ.Objects;
                    if (TargetOBJ == null) return;
                    TargetTile = TargetOBJ.Objects.FirstOrDefault(x => x.Object.OBJ.GUID == id);
                    if (TargetTile == null) TargetTile = TargetOBJ.BaseObject;
                    var tile = TargetTile.VisualPosition;
                    TempVM.Context.World.State.CenterTile = new Vector2(tile.X, tile.Y) - new Vector2(2.5f, 2.5f);
                    foreach (var obj in TargetOBJ.Objects)
                    {
                        if (obj is VMGameObject) ((ObjectComponent)obj.WorldUI).renderInfo.Layer = LotView.WorldObjectRenderLayer.DYNAMIC;
                        if (notMaster && obj.Object.OBJ.GUID != id) obj.SetRoom(2);
                    }
                }
            }
        }

        public void SetGUID(uint id)
        {
            oldGUID = 0;
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

                TempVM = new VM(context, new VMServerDriver(37565), new VMNullHeadlineProvider());
                TempVM.Init();

                var blueprint = new Blueprint(32, 32);
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
                SetGUIDLocal(GUID, TempVM);
                state.SharedData["ExternalDraw"] = true;
            }

            if (TempVM != null) TempVM.Update();
        }

        public override void Draw(UISpriteBatch batch)
        {
            base.Draw(batch);
            var bg = EditorComponent.EditorResource.Get().ViewBG;
            var viewport = GameFacade.GraphicsDevice.Viewport;
            DrawLocalTexture(batch, bg, new Vector2(viewport.Width / 2 - 400, viewport.Height / 2 - 300));
            batch.Pause();
            GameFacade.GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            if (TempVM == null) return;
            var world = TempVM.Context.World;
            world.State.SetDimensions(new Vector2(viewport.Width, viewport.Height));
            world.Draw(GameFacade.GraphicsDevice);
            GameFacade.GraphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
            batch.Resume();
        }
    }
}
