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
        public MSDFInfo Info;
        public char Fallback = '*';

        public int GlyphSize;
        private Texture2D Atlas;
        private FieldFont Font;
        public static Effect MSDFEffect;

        public List<MSDFFont> Fallbacks = new List<MSDFFont>();
        public Dictionary<MSDFFont, MSDFInfo> ChildInfo = new Dictionary<MSDFFont, MSDFInfo>();

        public MSDFFont(FieldFont font)
        {
            Font = font;
            GlyphSize = font.Atlas.GlyphSize;
            Info = new MSDFInfo(font);
        }

        public void AddFallback(FieldFont font, string name, float scale)
        {
            var msdf = new MSDFFont(font);
            msdf.VectorScale = scale;
            Fallbacks.Add(msdf);
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

        private MSDFGlyph GetGlyph(char c)
        {
            var result = Font.GetGlyph(c);
            if (result != null)
            {
                return new MSDFGlyph(result, this);
            }
            if (c == '\r') return null;
            foreach (var fallback in Fallbacks)
            {
                result = fallback.Font.GetGlyph(c);
                if (result != null)
                {
                    return new MSDFGlyph(result, fallback);
                }
            }
            return new MSDFGlyph(Font.GetGlyph(Fallback), this);
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

            var groups = new Dictionary<MSDFFont, MSDFRenderGroup>();
            var activeFont = this;

            var itemW = Info.itemW;
            var itemH = Info.itemH;

            var textureWidth = Info.textureWidth;
            var textureHeight = Info.textureHeight;

            var cutUX = Info.cutUX;
            var cutUY = Info.cutUY;

            var uW = Info.uW;
            var uH = Info.uH;

            var atlasWidth = Info.atlasWidth;
            var pairs = Info.pairs;
            var data = new MSDFRenderGroup(this);
            groups[this] = data;

            var verts = data.Vertices;
            var inds = data.Indices;

            effect.Parameters["WorldViewProjection"].SetValue(wvp);
            effect.Parameters["Color"].SetValue(color.ToVector4());

            effect.CurrentTechnique = effect.Techniques[0];
            var subScale = 1f;

            MSDFGlyph next = null;
            if (text.Length > 0)
            {
                var c = text[0];
                next = GetGlyph(c);
            }
            for (int i = 0; i < text.Length; i++)
            {
                var glyph = next;
                if (glyph == null)
                {
                    if (i + 1 >= text.Length) break;
                    var c = text[i + 1];
                    next = GetGlyph(c);
                    continue;
                }

                if (glyph.Font != activeFont)
                {
                    activeFont = glyph.Font;
                    subScale = (activeFont != this) ? activeFont.VectorScale / VectorScale : 1f;

                    var ainfo = activeFont.Info;
                    itemW = ainfo.itemW;
                    itemH = ainfo.itemH;

                    textureWidth = ainfo.textureWidth;
                    textureHeight = ainfo.textureHeight;

                    cutUX = ainfo.cutUX;
                    cutUY = ainfo.cutUY;

                    uW = ainfo.uW;
                    uH = ainfo.uH;

                    atlasWidth = ainfo.atlasWidth;
                    pairs = ainfo.pairs;
                    MSDFRenderGroup mdata = null;
                    if (!groups.TryGetValue(activeFont, out mdata))
                    {
                        mdata = new MSDFRenderGroup(activeFont);
                        groups[activeFont] = mdata;
                    }

                    verts = mdata.Vertices;
                    inds = mdata.Indices;
                }

                var fglyph = glyph.Glyph;
                var mscale = fglyph.Metrics.Scale;

                
                var left = point.X - (fglyph.Metrics.Translation.X - 1/mscale) * subScale;
                var bottom = point.Y + (fglyph.Metrics.Translation.Y + activeFont.YOff/ activeFont.VectorScale - 1 / mscale) * subScale;

                mscale /= subScale;
                var glyphHeight = (textureHeight - 2) / mscale;
                var glyphWidth = (textureWidth - 2) / mscale;

                var right = left + glyphWidth;
                var top = bottom - glyphHeight;

                var tx = (fglyph.AtlasIndex % atlasWidth) * itemW + cutUX;
                var ty = (fglyph.AtlasIndex / atlasWidth) * itemH + cutUY;

                var derivative = (new Vector2(uW/(right - left), uH/ (bottom - top))/scale)/2;

                if (!char.IsWhiteSpace(fglyph.Character))
                {
                    RenderQuad(inds, verts, new Vector2(left, bottom), new Vector2(right, top), new Vector2(tx, ty + uH), new Vector2(tx + uW, ty), derivative);
                }

                point.X += fglyph.Metrics.Advance * subScale;

                if (i < text.Length - 1)
                {
                    var c = text[i + 1];
                    next = GetGlyph(c);

                    if (next != null)
                    {
                        KerningPair pair;
                        if (pairs.TryGetValue(new string(new char[] { fglyph.Character, next.Glyph.Character }), out pair))
                        {
                            point.X += pair.Advance * subScale;
                        }
                    }
                }
            }

            foreach (var group in groups.Values)
            {
                effect.Parameters["PxRange"].SetValue(group.Font.Font.PxRange);
                var groupAtlas = group.Font.GetAtlas(gd);
                effect.Parameters["TextureSize"].SetValue(new Vector2(groupAtlas.Width, groupAtlas.Height));
                effect.Parameters["GlyphTexture"].SetValue(groupAtlas);
                effect.CurrentTechnique.Passes[0].Apply();
                if (group.Vertices.Count == 0) continue;

                gd.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, 
                    group.Vertices.ToArray(), 0, group.Vertices.Count,
                    group.Indices.ToArray(), 0, group.Indices.Count / 3);
            }
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
            var activeFont = this;
            if (string.IsNullOrEmpty(text))
                return new Vector2(0, Height / VectorScale);

            var size = new Vector2(0, Height / VectorScale);
            var subScale = 1f;

            MSDFGlyph next = null;
            if (text.Length > 0)
            {
                var c = text[0];
                next = GetGlyph(c);
            }
            for (int i = 0; i < text.Length; i++)
            {
                var glyph = next;
                if (next == null)
                {
                    if (i + 1 >= text.Length) break;
                    var c = text[i + 1];
                    next = GetGlyph(c);
                    continue;
                }
                if (activeFont != next.Font)
                {
                    activeFont = next.Font;
                    pairs = glyph.Font.Info.pairs;
                    subScale = (activeFont != this) ? activeFont.VectorScale / VectorScale : 1f;
                }

                size.X += glyph.Glyph.Metrics.Advance * subScale;

                if (i < text.Length - 1)
                {
                    var c = text[i+1];
                    next = GetGlyph(c);

                    if (next != null)
                    {
                        KerningPair pair;
                        if (pairs.TryGetValue(new string(new char[] { glyph.Glyph.Character, next.Glyph.Character }), out pair))
                        {
                            size.X += pair.Advance * subScale;
                        }
                    }
                }
            }

            return size;
        }
    }

    public class MSDFRenderGroup
    {
        public MSDFFont Font;
        public List<int> Indices;
        public List<MSDFFontVert> Vertices;

        public MSDFRenderGroup(MSDFFont font)
        {
            Font = font;
            Indices = new List<int>();
            Vertices = new List<MSDFFontVert>();
        }
    }

    public class MSDFGlyph
    {
        public FieldGlyph Glyph;
        public MSDFFont Font;

        public MSDFGlyph(FieldGlyph glyph, MSDFFont font)
        {
            Glyph = glyph;
            Font = font;
        }
    }

    public class MSDFInfo
    {
        public MSDFInfo(FieldFont Font)
        {
            itemW = 1f / Font.Atlas.Width;
            itemH = 1f / Font.Atlas.Height;

            textureWidth = Font.Atlas.GlyphSize;
            textureHeight = Font.Atlas.GlyphSize;

            cutUX = itemW / textureWidth;
            cutUY = itemH / textureHeight;

            cut2UX = cutUX * 2;
            cut2UY = cutUY * 2;

            uW = itemW - 2 * cutUX;
            uH = itemH - 2 * cutUY;

            cutX = 1f / textureWidth;
            cutY = 1f / textureHeight;

            atlasWidth = Font.Atlas.Width;
            pairs = Font.StringToPair;
        }

        public float itemW;
        public float itemH;

        public float textureWidth;
        public float textureHeight;

        public float cutUX;
        public float cutUY;

        public float cut2UX;
        public float cut2UY;

        public float uW;
        public float uH;

        public float cutX;
        public float cutY;

        public int atlasWidth;
        public Dictionary<string, KerningPair> pairs;
    }
}
