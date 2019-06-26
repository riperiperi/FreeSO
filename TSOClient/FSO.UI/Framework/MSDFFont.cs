using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MSDFData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.UI.Framework
{
    public class MSDFFont
    {
        public float Height = 20;
        public float YOff = 20 * 0.8f;
        public float VectorScale = 1f;
        public char Fallback = '*';

        public int GlyphSize;
        private Texture2D Atlas;
        private FieldFont Font;
        public static Effect MSDFEffect;

        public MSDFFont(FieldFont font)
        {
            Font = font;
            GlyphSize = font.Atlas.GlyphSize;
        }

        public Texture2D GetAtlas(GraphicsDevice gd)
        {
            if (Atlas == null)
            {
                using (var stream = new MemoryStream(Font.Atlas.PNGData))
                {
                    Atlas = Texture2D.FromStream(gd, stream);
                }
            }
            return Atlas;
        }

        public void Draw(GraphicsDevice gd, string text, Vector2 pos, Color color, Vector2 scale, Matrix? mat)
        {
            if (string.IsNullOrEmpty(text))
                return;

            var point = new Vector2(0, 0);
            var atlas = GetAtlas(gd);

            var wv = Matrix.CreateScale(scale.X, scale.Y, 1)
                * Matrix.CreateTranslation(pos.X, pos.Y, 0);

            if (mat != null)
            {
                wv = wv * mat.Value;

                wv.Decompose(out var scale2, out var quat, out var trans);
                scale.X = scale2.X;
                scale.Y = scale2.Y;
            }

            var wvp = wv * Matrix.CreateOrthographicOffCenter(new Rectangle(0, 0, gd.Viewport.Width, gd.Viewport.Height), -0.1f, 1f);
            var effect = MSDFEffect;

            var itemW = 1f / Font.Atlas.Width;
            var itemH = 1f / Font.Atlas.Height;

            var textureWidth = Font.Atlas.GlyphSize;
            var textureHeight = Font.Atlas.GlyphSize;

            var cutUX = itemW / textureWidth;
            var cutUY = itemH / textureHeight;

            var cut2UX = cutUX*2;
            var cut2UY = cutUY*2;

            var uW = itemW - 2 * cutUX;
            var uH = itemH - 2 * cutUY;

            var cutX = 1f / textureWidth;
            var cutY = 1f / textureHeight;

            var atlasWidth = Font.Atlas.Width;
            var pairs = Font.StringToPair;

            effect.Parameters["WorldViewProjection"].SetValue(wvp);
            effect.Parameters["PxRange"].SetValue(this.Font.PxRange);
            effect.Parameters["TextureSize"].SetValue(new Vector2(Atlas.Width, Atlas.Height));
            effect.Parameters["Color"].SetValue(color.ToVector4());
            effect.Parameters["GlyphTexture"].SetValue(Atlas);

            effect.CurrentTechnique = effect.Techniques[0];

            var verts = new List<MSDFFontVert>();
            var inds = new List<int>();

            FieldGlyph next = null;
            if (text.Length > 0)
            {
                var c = text[0];
                next = Font.GetGlyph(c);
                if (next == null && c != '\r') next = Font.GetGlyph(Fallback);
            }
            for (int i = 0; i < text.Length; i++)
            {
                var glyph = next;
                if (glyph == null)
                {
                    if (i + 1 >= text.Length) break;
                    var c = text[i + 1];
                    next = Font.GetGlyph(c);
                    if (next == null && c != '\r') next = Font.GetGlyph(Fallback);
                    continue;
                }

                var mscale = glyph.Metrics.Scale;
                var glyphHeight = (textureHeight-2) / mscale;
                var glyphWidth = (textureWidth-2) / mscale;
                
                var left = point.X - glyph.Metrics.Translation.X + 1/mscale;
                var bottom = point.Y + glyph.Metrics.Translation.Y + YOff/VectorScale - 1 / mscale;

                var right = left + glyphWidth;
                var top = bottom - glyphHeight;

                var tx = (glyph.AtlasIndex % atlasWidth) * itemW + cutUX;
                var ty = (glyph.AtlasIndex / atlasWidth) * itemH + cutUY;

                var derivative = (new Vector2(uW/(right - left), uH/ (bottom - top))/scale)/2;

                if (!char.IsWhiteSpace(glyph.Character))
                {
                    RenderQuad(inds, verts, new Vector2(left, bottom), new Vector2(right, top), new Vector2(tx, ty + uH), new Vector2(tx + uW, ty), derivative);
                }

                point.X += glyph.Metrics.Advance;

                if (i < text.Length - 1)
                {
                    var c = text[i + 1];
                    next = Font.GetGlyph(c);
                    if (next == null && c != '\r') next = Font.GetGlyph(Fallback);

                    if (next != null)
                    {
                        KerningPair pair;
                        if (pairs.TryGetValue(new string(new char[] { glyph.Character, next.Character }), out pair))
                        {
                            point.X += pair.Advance;
                        }
                    }
                }
            }

            effect.CurrentTechnique.Passes[0].Apply();
            if (verts.Count == 0) return;

            gd.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, verts.ToArray(), 0, verts.Count, inds.ToArray(), 0, inds.Count / 3);
        }

        private void RenderQuad(List<int> indices, List<MSDFFontVert> vertices, Vector2 v1, Vector2 v2, Vector2 tc1, Vector2 tc2, Vector2 derivative)
        {
            var b = vertices.Count;
            vertices.Add(new MSDFFontVert(new Vector3(v2.X, v1.Y, 0), new Vector2(tc2.X, tc1.Y), derivative));
            vertices.Add(new MSDFFontVert(new Vector3(v1, 0), tc1, derivative));
            vertices.Add(new MSDFFontVert(new Vector3(v1.X, v2.Y, 0), new Vector2(tc1.X, tc2.Y), derivative));
            vertices.Add(new MSDFFontVert(new Vector3(v2, 0), tc2, derivative));

            indices.Add(b);
            indices.Add(b + 1);
            indices.Add(b + 2);

            indices.Add(b + 2);
            indices.Add(b + 3);
            indices.Add(b);
        }

        public Vector2 MeasureString(string text)
        {
            var pairs = Font.StringToPair;
            if (string.IsNullOrEmpty(text))
                return new Vector2(0, Height / VectorScale);

            var size = new Vector2(0, Height / VectorScale);

            FieldGlyph next = null;
            if (text.Length > 0)
            {
                var c = text[0];
                next = Font.GetGlyph(c);
                if (next == null && c != '\r') next = Font.GetGlyph(Fallback);
            }
            for (int i = 0; i < text.Length; i++)
            {
                var glyph = next;
                if (next == null)
                {
                    if (i + 1 >= text.Length) break;
                    var c = text[i + 1];
                    next = Font.GetGlyph(c);
                    if (next == null && c != '\r') next = Font.GetGlyph(Fallback);
                    continue;
                }
                
                size.X += glyph.Metrics.Advance;

                if (i < text.Length - 1)
                {
                    var c = text[i+1];
                    next = Font.GetGlyph(c);
                    if (next == null && c != '\r') next = Font.GetGlyph(Fallback);

                    if (next != null)
                    {
                        KerningPair pair;
                        if (pairs.TryGetValue(new string(new char[] { glyph.Character, next.Character }), out pair))
                        {
                            size.X += pair.Advance;
                        }
                    }
                }
            }

            return size;
        }
    }
}
