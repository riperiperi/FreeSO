using Microsoft.Xna.Framework.Content;

namespace MSDFData
{    
    public class FieldGlyph
    {        
        [ContentSerializer] private readonly char CharacterBackend;
        [ContentSerializer] private readonly int AtlasIndexBackend;
        [ContentSerializer] private readonly Metrics MetricsBackend;

        public FieldGlyph()
        {
           
        }

        public FieldGlyph(char character, int atlasIndex, Metrics metrics)
        {
            this.CharacterBackend = character;
            this.AtlasIndexBackend = atlasIndex;
            this.MetricsBackend = metrics;
        }
        
        /// <summary>
        /// The character this glyph represents
        /// </summary>
        public char Character => this.CharacterBackend;
        /// <summary>
        /// Index of this character in the atlas.
        /// </summary>
        public int AtlasIndex => this.AtlasIndexBackend;                
        /// <summary>
        /// Metrics for this character
        /// </summary>
        public Metrics Metrics => this.MetricsBackend;
    }
}
