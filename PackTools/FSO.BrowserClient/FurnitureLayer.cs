using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FSO.LotView;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;

namespace FSO_BrowserClient
{
    /// <summary>
    /// Draws the AI-imported furniture (sprites exported by FSO.ContactSheet
    /// --export-dir) and placeholder sims onto the browser lot as billboards.
    /// This is a stand-in for the real object pipeline until the TSO content
    /// system is served to the browser; positions come from a furnish JSON.
    /// </summary>
    public class FurnitureLayer
    {
        class Placement
        {
            public Texture2D Tex;
            public Vector2 Tile;
            public string Label;      // sims only
            public Color LabelColor;  // sims only
        }

        readonly List<Placement> placements = new List<Placement>();
        public bool Ready { get; private set; }

        public async Task LoadAsync(GraphicsDevice gd, string baseUrl, string furnishUrl)
        {
            using var http = new HttpClient();
            var manifest = JArray.Parse(await http.GetStringAsync(new Uri(new Uri(baseUrl), "objects/manifest.json")));
            var byId = new Dictionary<string, string>();
            foreach (var o in manifest) byId[(string)o["id"]] = (string)o["png"];

            var furnish = JObject.Parse(await http.GetStringAsync(furnishUrl));
            var texCache = new Dictionary<string, Texture2D>();

            foreach (var f in (JArray)furnish["furniture"])
            {
                var id = (string)f["id"];
                if (!byId.TryGetValue(id, out var png)) continue;
                if (!texCache.TryGetValue(png, out var tex))
                {
                    var bytes = await http.GetByteArrayAsync(new Uri(new Uri(baseUrl), "objects/" + png));
                    using var ms = new System.IO.MemoryStream(bytes);
                    tex = Texture2D.FromStream(gd, ms);
                    texCache[png] = tex;
                }
                placements.Add(new Placement { Tex = tex, Tile = new Vector2((float)f["x"], (float)f["y"]) });
            }

            var simTex = MakeSimTexture(gd);
            foreach (var s in (JArray)furnish["sims"])
            {
                var c = (JArray)s["color"];
                placements.Add(new Placement
                {
                    Tex = simTex,
                    Tile = new Vector2((float)s["x"], (float)s["y"]),
                    Label = (string)s["name"],
                    LabelColor = new Color((int)c[0], (int)c[1], (int)c[2]),
                });
            }
            Ready = true;
            Console.WriteLine($"furniture layer ready: {placements.Count} placements");
        }

        static Texture2D MakeSimTexture(GraphicsDevice gd)
        {
            // A simple capsule silhouette — honest placeholder until Vitaboy
            // avatars are available in the browser.
            const int w = 24, h = 52;
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var dx = (x - w / 2f) / (w / 2f);
                    float dy;
                    if (y < w / 2) dy = (y - w / 2f) / (w / 2f);            // head cap
                    else if (y > h - w / 2) dy = (y - (h - w / 2f)) / (w / 2f); // foot cap
                    else dy = 0;
                    px[y * w + x] = (dx * dx + dy * dy <= 1) ? Color.White : Color.Transparent;
                }
            }
            var tex = new Texture2D(gd, w, h);
            tex.SetData(px);
            return tex;
        }

        public void Draw(SpriteBatch sb, WorldState state)
        {
            if (!Ready) return;
            var space = state.WorldSpace;
            var offset = space.GetPointScreenOffset();
            // Exported sprites are Near zoom (64px half-tile); scale to current zoom.
            var scale = space.TilePxWidthHalf / 64f;

            foreach (var p in placements.OrderBy(p => p.Tile.X + p.Tile.Y))
            {
                var screen = space.GetScreenFromTile(p.Tile + new Vector2(0.5f, 0.5f)) + offset;
                var isSim = p.Label != null;
                var drawScale = isSim ? 1f : scale;
                var w = p.Tex.Width * drawScale;
                var h = p.Tex.Height * drawScale;
                var pos = new Vector2(screen.X - w / 2, screen.Y - h + space.TilePxHeightHalf * (isSim ? 0.5f : 1f));
                var tint = isSim ? p.LabelColor : Color.White;
                sb.Draw(p.Tex, pos, null, tint, 0f, Vector2.Zero, drawScale, SpriteEffects.None, 0f);
            }
        }
    }
}
