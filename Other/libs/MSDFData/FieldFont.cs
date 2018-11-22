using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Content;

namespace MSDFData
{
    public class FieldFont
    {
        [ContentSerializer] private readonly Dictionary<char, FieldGlyph> Glyphs;
        [ContentSerializer] private readonly string NameBackend;
        [ContentSerializer] private readonly float PxRangeBackend;
        [ContentSerializer] private readonly List<KerningPair> KerningPairsBackend;
        [ContentSerializer] private readonly FieldAtlas AtlasBackend;

        public FieldFont()
        {
        }

        public FieldFont(string name, IReadOnlyCollection<FieldGlyph> glyphs, IReadOnlyCollection<KerningPair> kerningPairs, float pxRange, FieldAtlas atlas)
        {
            this.NameBackend = name;
            this.PxRangeBackend = pxRange;
            this.KerningPairsBackend = kerningPairs.ToList();
            this.AtlasBackend = atlas;

            this.Glyphs = new Dictionary<char, FieldGlyph>(glyphs.Count);
            foreach (var glyph in glyphs)
            {
                this.Glyphs.Add(glyph.Character, glyph);
            }
        }

        /// <summary>
        /// Name of the font
        /// </summary>
        public string Name => this.NameBackend;

        /// <summary>
        /// Distance field effect range in pixels
        /// </summary>
        public float PxRange => this.PxRangeBackend;

        /// <summary>
        /// Kerning pairs available in this font
        /// </summary>
        public IReadOnlyList<KerningPair> KerningPairs => this.KerningPairsBackend;

        /// <summary>
        /// Characters supported by this font
        /// </summary>
        [ContentSerializerIgnore]
        public IEnumerable<char> SupportedCharacters => this.Glyphs.Keys;

        private Dictionary<string, KerningPair> StringToPairBackend;
        [ContentSerializerIgnore]
        public Dictionary<string, KerningPair> StringToPair {
            get {
                if (StringToPairBackend == null)
                {
                    StringToPairBackend = KerningPairs.ToDictionary(x => new string(new char[] { x.Left, x.Right }));
                }
                return StringToPairBackend;
            }
        }

        /// <summary>
        /// Characters supported by this font
        /// </summary>
        public FieldAtlas Atlas => AtlasBackend;
       
        /// <summary>
        /// Returns the glyph for the given character, or returns null when the glyph is not supported by this font
        /// </summary>        
        public FieldGlyph GetGlyph(char c)
        {
            if (this.Glyphs.TryGetValue(c, out FieldGlyph glyph))
            {
                return glyph;
            }

            return null;
        }
    }
}
