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

namespace TSOClient.Code.Rendering.City
{
    public class CitySceneElement : ThreeDElement
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
        public void Initialize(string CityName)
        {
            /** Load the terrain effect **/
            if (zoomedIn)
            {
                effect = GameFacade.Game.Content.Load<Effect>("Effects/TerrainSplat");
            }
            else
            {
                effect = GameFacade.Game.Content.Load<Effect>("Effects/TerrainSplat2");
            }

            //Queue fucking gigantic switch :)
            switch (CityName)
            {
                case "Blazing Falls":
                    SetCity("0001");
                    break;
                case "Alphaville":
                    SetCity("0002");
                    break;
                case "Test Center":
                    SetCity("0003");
                    break;
                case "Interhogan":
                    SetCity("0004");
                    break;
                case "Ocean's Edge":
                    SetCity("0005");
                    break;
                case "East Jerome":
                    SetCity("0006");
                    break;
                case "Fancy Fields":
                    SetCity("0007");
                    break;
                case "Betaville":
                    SetCity("0008");
                    break;
                case "Charvatia":
                    SetCity("0009");
                    break;
                case "Dragon's Cove":
                    SetCity("0010");
                    break;
                case "Rancho Rizzo":
                    SetCity("0011");
                    break;
                case "Zavadaville":
                    SetCity("0012");
                    break;
                case "Queen Margaret's":
                    SetCity("0013");
                    break;
                case "Shannopolis":
                    SetCity("0014");
                    break;
                case "Grantley Grove":
                    SetCity("0015");
                    break;
                case "Calvin's Creek":
                    SetCity("0016");
                    break;
                case "The Billabong":
                    SetCity("0017");
                    break;
                case "Mount Fuji":
                    SetCity("0018");
                    break;
                case "Dan's Grove":
                    SetCity("0019");
                    break;
                case "Jolly Pines":
                    SetCity("0020");
                    break;
                case "Yatesport":
                    SetCity("0021");
                    break;
                case "Landry Lakes":
                    SetCity("0022");
                    break;
                case "Nichol's Notch":
                    SetCity("0023");
                    break;
                case "King Canyons":
                    SetCity("0024");
                    break;
                case "Virginia Islands":
                    SetCity("0025");
                    break;
                case "Pixie Point":
                    SetCity("0026");
                    break;
                case "West Darrington":
                    SetCity("0027");
                    break;
                case "Upper Shankelston":
                    SetCity("0028");
                    break;
                case "Albertstown":
                    SetCity("0029");
                    break;
                case "Terra Tablante":
                    SetCity("0030");
                    break;
                default:
                    SetCity("0001");
                    break;
            }

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
        }


        public void SetCity(string code)
        {
            City = CityData.Load(GameFacade.GraphicsDevice, GameFacade.GameFilePath("cities/city_" + code + "/"));
            RecalculateGeometry();
        }

        public void RecalculateGeometry()
        {
            if (zoomedIn)
            {
                Geom = new CityGeom();
            }

            Geom.CellHeight = CellHeight;
            Geom.CellWidth = CellWidth;
            Geom.CellYScale = CellScale;

            Geom.Process(City);
            Geom.CreateBuffer(GameFacade.GraphicsDevice);


            lightDirection = new Vector3((City.Width * CellWidth), (City.Height * CellHeight), -400f);
        }

        public override void Update(GameTime Time)
        {
        }

        public override void Draw(GraphicsDevice device, ThreeDScene scene)
        {
            var camera = new Camera(new Vector3(0, -14.1759f, 10f), new Vector3(0, 0, 0), Vector3.Up);

            var gd = GameFacade.GraphicsDevice;

            gd.VertexDeclaration = new VertexDeclaration(gd, TerrainVertex.VertexElements);
            effect.CurrentTechnique = effect.Techniques["TerrainSplat"];

            if (zoomedIn)
            {
                effect.Parameters["xTextureBlend"].SetValue(TextureBlend);
                effect.Parameters["xTextureTerrain"].SetValue(TextureTerrain);
            }
            else
            {
                effect.Parameters["xTextureGrass"].SetValue(TextureGrass);
                effect.Parameters["xTextureSnow"].SetValue(TextureSnow);
                effect.Parameters["xTextureSand"].SetValue(TextureSand);
                effect.Parameters["xTextureRock"].SetValue(TextureRock);
                effect.Parameters["xTextureWater"].SetValue(TextureWater);
            }
            //;
            //var worldMatrix = Matrix.Identity * Matrix.CreateTranslation(transX, 0, transY) * Matrix.CreateScale(zoom) * Matrix.CreateRotationX(MathHelper.ToRadians(degs));
            
            effect.Parameters["xWorld"].SetValue(World);
            effect.Parameters["xView"].SetValue(scene.Camera.View);
            effect.Parameters["xProjection"].SetValue(GameFacade.Scenes.ProjectionMatrix);

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
