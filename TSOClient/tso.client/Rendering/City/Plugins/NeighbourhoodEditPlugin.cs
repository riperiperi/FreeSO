using System.Collections.Generic;
using System.Linq;
using FSO.Client.Rendering.City.Model;
using FSO.Client.UI.Controls;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Utils;
using FSO.UI.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;

namespace FSO.Client.Rendering.City.Plugins
{
    public class NeighbourhoodEditPlugin : AbstractCityPlugin
    {
        public List<CityNeighbourhood> EditTarget;
        public CityNeighbourhood Selected;

        private Texture2D PxBlack;
        private Texture2D PxWhite;

        public NeighbourhoodEditPlugin(Terrain city) : base(city)
        {
            PxBlack = TextureUtils.TextureFromColor(GameFacade.GraphicsDevice, Color.Black);
            PxWhite = TextureUtils.TextureFromColor(GameFacade.GraphicsDevice, Color.White);
            EditTarget = city.NeighGeom.Data;
        }

        public override void Draw(SpriteBatch sb)
        {
            if (EditTarget == null) return;
            sb.Begin();

            foreach (var neigh in EditTarget)
            {
                var onScreen = City.Get2DFromTile(neigh.Location.X, neigh.Location.Y);
                City.DrawLine(PxBlack, onScreen + new Vector2(0, -8), onScreen + new Vector2(0, 8), sb, 16, 100);
                City.DrawLine(PxWhite, onScreen + new Vector2(0, -8), onScreen + new Vector2(0, 2), sb, 10, 100);
            }

            if (Selected != null)
            {
                var targ = City.GetHoverSquare(new double[] { 0.0, 0.0, 512.0, 512.0 });
                if (targ != null)
                {
                    Selected.Location = targ.Value.ToPoint();
                }
                City.NeighGeom.Generate(GameFacade.GraphicsDevice);
            }
            
            /* raycasting debug

            var hits = new List<Point>();
            var test = City.EstTileAtPosWithScroll(mp, hits);
            foreach (var hit in hits)
            {
                City.PathTile(hit.X, hit.Y, 0, Color.Red);
            }

            if (test.X >= 0 && test.X < 512 && test.Y >= 0 && test.Y < 512)
                City.PathTile((int)test.X, (int)test.Y, 0, Color.Green);
            City.Draw2DPoly(false);
            */

            sb.End();
        }

        Vector2 mp;

        public override void TileHover(Vector2? tile)
        {
        }

        public override void TileMouseDown(Vector2 tile)
        {
        }

        public override void TileMouseUp(Vector2? tile)
        {
        }

        private bool MouseWasDown;
        private bool CtrlDown;
        private bool ShiftDown;

        public override void Update(UpdateState state)
        {
            mp = state.MouseState.Position.ToVector2();
            var md = state.MouseState.LeftButton == ButtonState.Pressed;
            CtrlDown = state.CtrlDown;
            ShiftDown = state.ShiftDown;

            if (md != MouseWasDown)
            {
                if (md)
                {
                    if (ShiftDown)
                    {
                        var neigh = new CityNeighbourhood()
                        {
                            Location = new Point(256, 256),
                            Name = "Neigh" + EditTarget.Count
                        };
                        EditTarget.Add(neigh);
                        Selected = neigh;
                    }
                    else
                    {
                        var closest = EditTarget.OrderBy(x => (City.Get2DFromTile(x.Location.X, x.Location.Y) - state.MouseState.Position.ToVector2()).Length()).FirstOrDefault();
                        if (closest != null && (City.Get2DFromTile(closest.Location.X, closest.Location.Y) - state.MouseState.Position.ToVector2()).Length() < 16)
                        {
                            if (CtrlDown)
                            {
                                EditTarget.Remove(closest);
                                City.NeighGeom.Generate(GameFacade.GraphicsDevice);
                            }
                            else
                            {
                                Selected = closest;
                            }
                        }
                    }
                }
                else
                {
                    Selected = null;
                }
                MouseWasDown = md;
            }

            if (state.NewKeys.Contains(Keys.R)) {
                var proj = City.EstTileAtPosWithScroll(state.MouseState.Position.ToVector2(), null);
                var near = City.NeighGeom.NhoodNearest(proj);

                if (near != -1)
                {
                    var nhood = City.NeighGeom.Data[near];
                    UIAlert.Prompt(new UIAlertOptions()
                    {
                        Message = "Rename this neighbourhood to what?",
                        TextEntry = true
                    }, (result, alert) =>
                    {
                        if (result)
                        {
                            nhood.Name = alert.ResponseText;
                        }
                    });
                }
            }

            if (state.NewKeys.Contains(Keys.C))
            {
                var proj = City.EstTileAtPosWithScroll(state.MouseState.Position.ToVector2(), null);
                var near = City.NeighGeom.NhoodNearest(proj);

                if (near != -1)
                {
                    var nhood = City.NeighGeom.Data[near];
                    UIAlert.Prompt(new UIAlertOptions()
                    {
                        Message = "Change this neighbourhood colour to what?",
                        GenericAddition = new UIColorPicker()
                    }, (result, alert) =>
                    {
                        if (result)
                        {
                            var col = int.Parse(alert.ResponseText);
                            nhood.Color = new Color(col>>16, (col>>8)&0xFF, col&0xFF);
                        }
                    });
                }
            }

            if (state.NewKeys.Contains(Keys.F10))
            {
                if (state.ShiftDown)
                {
                    using (var file = System.IO.File.Open("Content/edit_neigh.json", System.IO.FileMode.Create, System.IO.FileAccess.Write))
                    using (var writer = new System.IO.StreamWriter(file))
                        writer.Write(Newtonsoft.Json.JsonConvert.SerializeObject(EditTarget, Newtonsoft.Json.Formatting.Indented));
                } else
                {
                    using (var file = System.IO.File.Open("Content/edit_neigh.json", System.IO.FileMode.Open, System.IO.FileAccess.Read))
                    using (var reader = new System.IO.StreamReader(file))
                    {
                        EditTarget = JsonConvert.DeserializeObject<List<CityNeighbourhood>>(reader.ReadToEnd());
                        CityNeighbourhood.Init(EditTarget);
                        City.NeighGeom.Data = EditTarget;
                        City.NeighGeom.Generate(GameFacade.GraphicsDevice);
                    }
                }

            }
        }
    }
}
