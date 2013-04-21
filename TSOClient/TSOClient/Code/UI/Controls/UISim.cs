using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Framework;
using TSOClient.VM;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using SimsLib.ThreeD;
using TSOClient.ThreeD;
using TSOClient.Code.Rendering;
using TSOClient.Code.Rendering.Sim;
using TSOClient.Code.Utils;

namespace TSOClient.Code.UI.Controls
{
    /// <summary>
    /// Renders a sim in the UI, this class just helps translate the UI world
    /// into the 3D world for sim rendering
    /// </summary>
    public class UISim : UIElement
    {
        private SimRenderer SimRender;
        private ThreeDScene SimScene;

        /** 45 degrees in either direction **/
        public float RotationRange = 45;
        public float RotationStartAngle = 180;
        public float RotationSpeed = new TimeSpan(0, 0, 10).Ticks;
        public bool AutoRotate = true;

        public float SimScale = 0.6f;
        public float ViewScale = 17.0f;

        public UISim()
        {
            SimRender = new SimRenderer();
            SimRender.ID = "SimRender";

            SimScene = new ThreeDScene();
            SimScene.ID = "SimScene";
            SimScene.Camera = new Camera(Vector3.Backward * ViewScale, Vector3.Zero, Vector3.Right);
            SimScene.Add(SimRender);
            //GameFacade.Scenes.AddScene(SimScene);


            /** Default settings **/
            SimRender.Scale = new Vector3(0.6f);
            SimRender.RotationY = (float)MathUtils.DegreeToRadian(25);
            SimRender.RotationX = (float)MathUtils.DegreeToRadian(RotationStartAngle);
            //
            //var scene = new TSOClient.ThreeD.ThreeDScene();
            //scene.Add(a);
            GameFacade.Scenes.AddExternalScene(SimScene);
        }



        private void CalculateView()
        {
            SimRender.Scale = new Vector3(_Scale.X * SimScale, _Scale.Y * SimScale, 1.0f);

            var screen = GameFacade.Screens.CurrentUIScreen;
            if (screen == null) { return; }

            var globalLocation = screen.GlobalPoint(this.LocalPoint(Vector2.Zero));



            //var aspect = GameFacade.GraphicsDevice.Viewport.AspectRatio;
            /*var ratioX = 1024.0f / 1024.0f;
            var ratioY = 10.0f / 768.0f;
            var projectionX = 0.0f - (1.0f * ratioX);
            var projectionY = 0.0f - (1.0f * ratioY);
            effect.Projection = Matrix.CreatePerspectiveOffCenter(projectionX, projectionX + 1.0f, (projectionY / aspect), (projectionY+1.0f) / aspect, 1.0f, 100.0f);
            */
            SimScene.Camera.ProjectionOrigin = globalLocation;



            //var cameraPosition = (Vector3.Backward * ViewScale);
            //var cameraTarget = Vector3.Zero;


            //var screenMatrix = GameFacade.Scenes.ProjectionMatrix;
            //var invertScreenMatrix = Microsoft.Xna.Framework.Matrix.Invert(screenMatrix);

            //var something =
            //    Vector3.Transform(new Vector3((float)globalLocation.X, (float)globalLocation.Y, 0.0f), invertScreenMatrix);

            //var unproject = GameFacade.GraphicsDevice.Viewport.Unproject(new Vector3(globalLocation.X, globalLocation.Y, 0.0f), GameFacade.Scenes.ProjectionMatrix, SimScene.Camera.View, Microsoft.Xna.Framework.Matrix.Identity);


            //var viewport = GameFacade.GraphicsDevice.Viewport;

            //var projection = Microsoft.Xna.Framework.Matrix.CreateOrthographicOffCenter(0, viewport.Width, viewport.Height, 0, 0, 1);
            //var halfPixelOffset = Microsoft.Xna.Framework.Matrix.CreateTranslation(-0.5f, -0.5f, 0);
            //var realProjection = projection * halfPixelOffset;


            //var unproj = viewport.Project(new Vector3(globalLocation.X, globalLocation.Y, 0.0f), realProjection, Microsoft.Xna.Framework.Matrix.Identity, Microsoft.Xna.Framework.Matrix.Identity);
            //var inScene = viewport.Unproject(unproj, GameFacade.Scenes.ProjectionMatrix, SimScene.Camera.View, SimRender.World);


            //var something = Vector3.Transform(new Vector3(globalLocation.X, globalLocation.Y, 0.0f), realProjection);


            //SimScene.Camera.Position = cameraPosition;
            //SimScene.Camera.Target = cameraTarget;





            /*var position = GameFacade.GraphicsDevice.Viewport.Unproject(new Vector3((float)globalLocation.X, (float)globalLocation.Y, 0.0f),
                                GameFacade.Scenes.ProjectionMatrix,
                                SimScene.Camera.View,
                                SimRender.World);

            */


            //SimScene.Camera.Position = cameraPosition + position;
            //SimScene.Camera.Target = cameraTarget - position;

            
            
            
            //var offsetX = (screen.ScreenWidth / 2.0f) - globalLocation.X;
            //var offsetY = (screen.ScreenHeight / 2.0f) - globalLocation.Y;
            //offsetX = (offsetX / screen.ScreenWidth) * ViewScale;
            //offsetY = (offsetY / screen.ScreenHeight) * ViewScale;

            //var cameraOffsetY = screen.ScreenWidth / offsetX;
            //var cameraOffsetX = screen.ScreenHeight / offsetY;

            //var halfWidth = screen.ScreenWidth / 2.0f;
            //var cameraOffsetY = ((halfWidth - globalLocation.X) / halfWidth) * (ViewScale / 2);
            //var cameraOffsetX = 0;// screen.ScreenWidth / offsetX;



            //SimScene.Camera.Position = (Vector3.Backward * ViewScale) + new Vector3(offsetX, offsetY, 0.0f);
            //SimRender.Position = new Vector3(offsetX, offsetY, 0.0f);

        }


        public override void Update(TSOClient.Code.UI.Model.UpdateState state)
        {
            base.Update(state);

            if (AutoRotate)
            {
                var startAngle = RotationStartAngle;
                var time = state.Time.TotalRealTime.Ticks;
                var phase = (time % RotationSpeed) / RotationSpeed;

                var multiplier = Math.Sin((Math.PI * 2) * phase);
                var newAngle = startAngle + (RotationRange * multiplier);

                SimRender.RotationX = (float)MathUtils.DegreeToRadian(newAngle);
            }
        }




        private Sim m_Sim;
        public Sim Sim
        {
            get { return m_Sim; }
            set
            {
                m_Sim = value;
                SimRender.Sim = value;
            }
        }

        private Vector2 m_Size;
        public Vector2 Size
        {
            get { return m_Size; }
            set
            {
                m_Size = value;
                CalculateView();
            }
        }


        protected override void CalculateMatrix()
        {
            base.CalculateMatrix();

            /** Re-calculate the 3D world **/
            CalculateView();
        }




        public override void Draw(UISpriteBatch batch)
        {
            batch.Pause();
            SimScene.Draw(GameFacade.GraphicsDevice);
            batch.Resume();
        }
    }
}
