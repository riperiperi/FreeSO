/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.ThreeD;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.IO;
using TSOClient.Code.Utils;
using TSOClient.Code.UI.Model;
using tso.common.rendering.framework.model;
using tso.common.utils;
using tso.common.rendering.framework;

namespace TSOClient.Code.Rendering.City
{
    public class CitySceneElement : _3DComponent
    {
        public CityData City;
        public ICityGeom Geom;


        private Texture2D TextureBlend;
        private Texture2D TextureTerrain;

        private Texture2D TextureGrass;
        private Texture2D TextureSnow;
        private Texture2D TextureSand;
        private Texture2D TextureRock;
        private Texture2D TextureWater;

        private Effect effect;
        private Vector3 lightDirection = new Vector3(-0.5f, -30, -0.5f);

        public float CellWidth = 1;
        public float CellHeight = 1;
        public float CellScale = 15;

        private RenderTarget2D RenderTarget;

        private bool zoomedIn = true;

        public int degs = -13;
        public float zoom = 1.24f;
        public float transX = 0;
        public float transY = 0;


        /// <summary>
        /// Allows the game component to perform any initialization it needs to before starting
        /// to run.  This is where it can query for any required services and load content.
        /// </summary>
        public void Initialize()
        {
            //RenderTarget = RenderTargetUtils.CreateRenderTarget(game.GraphicsDevice, 1, SurfaceFormat.Color, 800, 600);

            /** Load the terrain effect **/
            effect = GameFacade.Game.Content.Load<Effect>("Effects/TerrainSplat");
            

            /** Setup **/
            //SetCity("0020");
            SetCity("0013");

            //transX = -(City.Width * Geom.CellWidth / 2);
            //transY = +(City.Height / 2 * Geom.CellHeight / 2);

            /**
             * Setup terrain texture
             */

            var device = GameFacade.GraphicsDevice;

            var textureBase = GameFacade.GameFilePath("gamedata/terrain/newformat/");

            var grass = Texture2D.FromFile(device, Path.Combine(textureBase, "gr.tga"));
            var rock = Texture2D.FromFile(device, Path.Combine(textureBase, "rk.tga"));
            var snow = Texture2D.FromFile(device, Path.Combine(textureBase, "sn.tga"));
            var sand = Texture2D.FromFile(device, Path.Combine(textureBase, "sd.tga"));
            var water = Texture2D.FromFile(device, Path.Combine(textureBase, "wt.tga"));

            TextureTerrain = TextureUtils.MergeHorizontal(device, grass, snow, sand, rock, water);

            TextureGrass = grass;
            TextureSand = sand;
            TextureSnow = snow;
            TextureRock = rock;
            TextureWater = water;

            effect.Parameters["xTextureBlend"].SetValue(TextureBlend);
            effect.Parameters["xTextureTerrain"].SetValue(TextureTerrain);
            

            /** Dont need these anymore **/
            //grass.Dispose();
            //rock.Dispose();
            //snow.Dispose();
            //sand.Dispose();
            //water.Dispose();

            /**
             * Setup alpha map texture
             */
            /** Construct a single texture out of the alpha maps **/
            Texture2D[] alphaMaps = new Texture2D[15];
            for (var t = 0; t < 15; t++)
            {
                var index = t.ToString();
                if (t < 10) { index = "0" + index; }
                alphaMaps[t] = Texture2D.FromFile(device, Path.Combine(textureBase, "transb" + index + "b.tga"));
            }

            /** We add an extra 64px so that the last slot in the sheet is a solid color aka no blending **/
            TextureBlend = TextureUtils.MergeHorizontal(device, 64, alphaMaps);
            alphaMaps.ToList().ForEach(x => x.Dispose());


            effect.Parameters["xTextureBlend"].SetValue(TextureBlend);
            effect.Parameters["xTextureTerrain"].SetValue(TextureTerrain);
            

            //TextureBlend.Save(@"C:\Users\Admin\Desktop\blendBB.jpg", ImageFileFormat.Jpg);
        }


        public void SetCity(string code)
        {
            //currentCity = code;
            City = CityData.Load(GameFacade.GraphicsDevice, GameFacade.GameFilePath("cities/city_" + code + "/"));
            RecalculateGeometry();
        }

        public void RecalculateGeometry()
        {
            Geom = new RhysGeom();

            Geom.CellHeight = CellHeight;
            Geom.CellWidth = CellWidth;
            Geom.CellYScale = CellScale;

            Geom.Process(City);
            Geom.CreateBuffer(GameFacade.GraphicsDevice);


            lightDirection = new Vector3((City.Width * CellWidth), (City.Height * CellHeight), -400f);
        }

        public override void Update(UpdateState GState)
        {
        }

        public override void Draw(GraphicsDevice device)
        {
            var gd = GameFacade.GraphicsDevice;

            gd.VertexDeclaration = new VertexDeclaration(gd, TerrainVertex.VertexElements);
            effect.CurrentTechnique = effect.Techniques["TerrainSplat"];

            effect.Parameters["xWorld"].SetValue(World);
            effect.Parameters["xView"].SetValue(View);
            effect.Parameters["xProjection"].SetValue(Projection);

            effect.Parameters["xEnableLighting"].SetValue(true);
            effect.Parameters["xAmbient"].SetValue(0.8f);
            effect.Parameters["xLightDirection"].SetValue(lightDirection);
            effect.CommitChanges();

            effect.Begin();
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Begin();
                Geom.Draw(gd);
                pass.End();
            }
            effect.End();
        }
    }
}
