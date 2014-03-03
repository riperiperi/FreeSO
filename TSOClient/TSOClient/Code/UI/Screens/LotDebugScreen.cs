using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Framework;
using TSOClient.Code.UI.Panels;
using tso.world;
using TSOClient.LUI;
using tso.world.model;
using tso.simantics;
using tso.simantics.utils;
using Microsoft.Xna.Framework;
using tso.debug;

namespace TSOClient.Code.UI.Screens
{
    public class LotDebugScreen : GameScreen
    {
        private UIUCP ucp;
        private World World;
        private UIButton VMDebug;
        private tso.simantics.VM vm;

        public LotDebugScreen()
        {
            var lotInfo = XmlHouseData.Parse(GameFacade.GameFilePath("housedata/blueprints/restaurant01_00.xml"));

            World = new World(GameFacade.Game.GraphicsDevice);
            GameFacade.Scenes.Add(World);

            vm = new tso.simantics.VM(new VMContext(World));
            vm.Init();

            var activator = new VMWorldActivator(vm, World);
            var blueprint = activator.LoadFromXML(lotInfo);

            World.InitBlueprint(blueprint);
            vm.Context.Blueprint = blueprint;

            var sim = activator.CreateAvatar();
            //sim.Position = new Vector3(31.5f, 55.5f, 0.0f);
            sim.Position = new Vector3(26.5f, 41.5f, 0.0f);

            VMDebug = new UIButton()
            {
                Caption = "Simantics",
                Y = 45,
                Width = 100,
                X = GlobalSettings.Default.GraphicsWidth - 110
            };
            VMDebug.OnButtonClick += new ButtonClickDelegate(VMDebug_OnButtonClick);
            this.Add(VMDebug);

            //var lotInfo = HouseData.Parse("C:\\restaurant00_00_small.xml");
            //for (int i = 1; i < 64; i++)
            //{
            //    lotInfo.World.Floors.Add(new HouseDataFloor {
            //         X = 1,
            //         Y = i,
            //         Level = 0,
            //         Value = 9
            //    });
            //}

            //lotInfo.World.Floors.Add(new HouseDataFloor {
            //    X = 0, Y = 0,
            //    Level = 0, Value = 20
            //});

            //lotInfo.World.Floors.Add(new HouseDataFloor
            //{
            //    X = 63,
            //    Y = 63,
            //    Level = 0,
            //    Value = 40
            //});

            //lotInfo.World.Floors.Add(new HouseDataFloor
            //{
            //    X = 0,
            //    Y = 63,
            //    Level = 0,
            //    Value = 20
            //});

            //lotInfo.World.Floors.Add(new HouseDataFloor
            //{
            //    X = 63,
            //    Y = 0,
            //    Level = 0,
            //    Value = 20
            //});



            //Renderer = new HouseRenderer();
            //Renderer.SetModel(lotInfo);
            ////Renderer.Position = new Microsoft.Xna.Framework.Vector3(-32.0f, -40.0f, 0.0f);

            //var scene = new ThreeDScene();
            //var focusPoint = Vector3.Zero;

            //var yValue = (float)Math.Cos(MathHelper.ToRadians(30.0f)) * 96.0f;
            //var cameraOffset = new Vector3(-96.0f, yValue, 96.0f);
            //var rotatedOffset = Vector3.Transform(cameraOffset, Microsoft.Xna.Framework.Matrix.CreateRotationY(MathHelper.PiOver2 * 0.5f));

            ////rotatedOffset = Vector3.Transform(rotatedOffset, Microsoft.Xna.Framework.Matrix.CreateScale(3f));
            ////Renderer.Position = new Vector3(-96.0f, 0.0f, -96.0f);

            //scene.Camera.Position = cameraOffset;// new Microsoft.Xna.Framework.Vector3(0, 0, 80);
            //scene.Add(Renderer);
            //Renderer.Scale = new Vector3(0.005f);

            //GameFacade.Scenes.AddScene(scene);


            ucp = new UIUCP();
            ucp.Y = ScreenHeight - 210;
            //ucp.OnZoomChanged += new UCPZoomChangeEvent(ucp_OnZoomChanged);
            //ucp.OnRotateChanged += new UCPRotateChangeEvent(ucp_OnRotateChanged);
            this.Add(ucp);
        }

        public override void Update(tso.common.rendering.framework.model.UpdateState state)
        {
            base.Update(state);
            vm.Update(state.Time);
        }

        void VMDebug_OnButtonClick(UIElement button)
        {
            System.Windows.Forms.Form gameWindowForm =
                (System.Windows.Forms.Form)System.Windows.Forms.Form.FromHandle(GameFacade.Game.Window.Handle);
            //gameWindowForm.Location = new System.Drawing.Point(0, 0);

            var debugTools = new Simantics(vm);
            debugTools.Show();
            debugTools.Location = new System.Drawing.Point(gameWindowForm.Location.X + gameWindowForm.Width, gameWindowForm.Location.Y);
            debugTools.UpdateAQLocation();
            
        }

        

        void ucp_OnZoomChanged(WorldZoom zoom)
        {
            World.State.Zoom = zoom;
            //Scene.Zoom = zoom;
        }
    }
}
