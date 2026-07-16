using FSO.Client;
using FSO.Common.Utils;
using FSO.LotView;
using FSO.LotView.Components;
using FSO.LotView.Model;
using FSO.SimAntics;
using FSO.SimAntics.Engine.TSOTransaction;
using FSO.SimAntics.Entities;
using FSO.SimAntics.Model;
using FSO.SimAntics.NetPlay.Drivers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.UI.Utils
{
    public static class CatThumbGenerator
    {
        private static VM ThumbVM;

        private static VM GetThumbVM()
        {
            if (ThumbVM == null)
            {
                var world = new ExternalWorld(GameFacade.GraphicsDevice);
                world.Initialize(GameFacade.Scenes);
                var context = new VMContext(world);

                ThumbVM = new VM(context, new VMServerDriver(new VMTSOGlobalLinkStub()), new VMNullHeadlineProvider());
                ThumbVM.Init();

                var blueprint = new Blueprint(1, 1)
                {
                    Light =
                    [
                        new RoomLighting() { OutsideLight = 100 },
                        new RoomLighting() { OutsideLight = 100 },
                        new RoomLighting() { OutsideLight = 100 },
                    ],
                    OutsideColor = Color.White
                };
                blueprint.GenerateRoomLights();
                blueprint.RoomColors[2].A /= 2;
                world.State.AmbientLight.SetData(blueprint.RoomColors);
                world.State.OutsidePx.SetData([Color.White]);

                world.InitBlueprint(blueprint);
                context.Blueprint = blueprint;
                context.Architecture = new VMArchitecture(1, 1, blueprint, ThumbVM.Context);
            }

            return ThumbVM;
        }

        public static Texture2D GenerateThumb(VMMultitileGroup obj, VM vm)
        {
            var gd = GameFacade.GraphicsDevice;
            var objects = obj.Objects;
            ObjectComponent[] objComps = new ObjectComponent[objects.Count];
            for (int i = 0; i < objects.Count; i++)
            {
                objComps[i] = (ObjectComponent)objects[i].WorldUI;
            }
            var thumb = vm.Context.World.GetObjectThumb(objComps, obj.GetBasePositions(), GameFacade.GraphicsDevice);

            var data = new Color[thumb.Width * thumb.Height];
            thumb.GetData(data);
            thumb.Dispose();
            var newAgain = new Texture2D(GameFacade.GraphicsDevice, thumb.Width, thumb.Height, true, SurfaceFormat.Color);
            TextureUtils.UploadWithMips(newAgain, GameFacade.GraphicsDevice, data);

            var sb = new SpriteBatch(GameFacade.GraphicsDevice);
            var result = new RenderTarget2D(GameFacade.GraphicsDevice, 74, 37);

            var oldRts = gd.GetRenderTargets();
            gd.SetRenderTarget(result);
            gd.Clear(Color.Black);
            var sampler = new SamplerState()
            {
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
                Filter = TextureFilter.Linear,
                MipMapLevelOfDetailBias = -0.5f,
            };
            sb.Begin(blendState: BlendState.NonPremultiplied, samplerState: sampler);
            var minScale = Math.Min(37f/newAgain.Width, 37f/newAgain.Height);
            if (minScale > 1) minScale = 1;
            var rect = new Rectangle(
                (int)(newAgain.Width * minScale / -2 + 18),
                (int)(newAgain.Height * minScale / -2 + 19),
                (int)(minScale * newAgain.Width),
                (int)(minScale * newAgain.Height));

            var px = TextureGenerator.GetPxWhite(GameFacade.GraphicsDevice);
            sb.Draw(px, new Rectangle(0, 0, 37, 37), new Color(56, 88, 120));
            sb.Draw(px, new Rectangle(37, 0, 37, 37), new Color(184, 212, 240));

            sb.Draw(newAgain, rect, Color.White);
            rect.Offset(37, 0);
            sb.Draw(newAgain, rect, Color.White);
            sb.End();

            gd.SetRenderTargets(oldRts);
            newAgain.Dispose();
            return result;
        }

        public static Texture2D GenerateThumb(uint guid)
        {
            var vm = GetThumbVM();

            var obj = vm.Context.CreateObjectInstance(guid, LotTilePos.OUT_OF_WORLD, Direction.NORTH, true);

            if (obj == null)
            {
                return null;
            }

            var icon = GenerateThumb(obj, vm);

            obj.Delete(vm.Context);

            return icon;
        }
    }
}
