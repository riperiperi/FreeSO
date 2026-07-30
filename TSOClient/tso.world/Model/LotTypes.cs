using Microsoft.Xna.Framework;

namespace FSO.LotView.Model
{
    public readonly struct LotTypeGrassInfo(Color lightGreen, Color lightBrown, Color darkGreen, Color darkBrown, Vector2 greenLengthDensity, Vector2 brownLengthDensity, int maxHeight, float baseDensity)
    {
        public readonly Color LightGreen = lightGreen;
        public readonly Color LightBrown = lightBrown;
        public readonly Color DarkGreen = darkGreen;
        public readonly Color DarkBrown = darkBrown;
        public readonly Vector2 GreenLengthDensity = greenLengthDensity;
        public readonly Vector2 BrownLengthDensity = brownLengthDensity;
        public readonly int MaxHeight = maxHeight;
        public readonly float BaseDensity = baseDensity;

        public const float MinDetailLength = 0.05f;

        public static LotTypeGrassInfo[] Info =
        [
            // Grass
            new(
                lightGreen: new Color(80, 116, 59),
                lightBrown: new Color(157, 117, 65),
                darkGreen: new Color(8, 52, 8),
                darkBrown: new Color(81, 60, 18),
                greenLengthDensity: new Vector2(1, 1),
                brownLengthDensity: new Vector2(MinDetailLength, 0.5f),
                maxHeight: 6,
                baseDensity: 1
            ),

            // Sand
            new(
                lightGreen: new Color(181, 171, 149),
                lightBrown: new Color(196, 185, 162),
                darkGreen: new Color(115, 109, 95),
                darkBrown: new Color(121, 114, 100),
                greenLengthDensity: new Vector2(MinDetailLength, 0.9f),
                brownLengthDensity: new Vector2(MinDetailLength, 0.75f),
                maxHeight: 1,
                baseDensity: 1
            ),

            // Rock
            new(
                lightGreen: new Color(126, 96, 70),
                lightBrown: new Color(126, 96, 70),
                darkGreen: new Color(107, 77, 57),
                darkBrown: new Color(107, 77, 57),
                greenLengthDensity: new Vector2(MinDetailLength, 1f),
                brownLengthDensity: new Vector2(MinDetailLength, 1f),
                maxHeight: 1,
                baseDensity: 1
            ),

            // Snow
            new(
                lightGreen: new Color(240, 245, 250),
                lightBrown: new Color(240, 245, 250),
                darkGreen: new Color(180, 180, 190),
                darkBrown: new Color(180, 180, 190),
                greenLengthDensity: new Vector2(MinDetailLength, 0.85f),
                brownLengthDensity: new Vector2(MinDetailLength, 0.85f),
                maxHeight: 1,
                baseDensity: 1
            ),

            // Water (debug)
            new(
                lightGreen: new Color(0, 0, 255),
                lightBrown: new Color(0, 0, 255),
                darkGreen: new Color(0, 0, 255),
                darkBrown: new Color(0, 0, 255),
                greenLengthDensity: new Vector2(0, 1f),
                brownLengthDensity: new Vector2(0, 1f),
                maxHeight: 0,
                baseDensity: 0
            ),

            // TS1 Dark Grass
            new(
                lightGreen: new Color(74, 89, 66),
                lightBrown: new Color(90, 69, 41),
                darkGreen: new Color(21, 30, 13),
                darkBrown: new Color(64, 69, 14),
                greenLengthDensity: new Vector2(1, 1),
                brownLengthDensity: new Vector2(MinDetailLength, 0.5f),
                maxHeight: 6,
                baseDensity: 0.8f
            ),

            // TS1 Autumn Grass
            new(
                lightGreen: new Color(140, 113, 49),
                lightBrown: new Color(115, 73, 33),
                darkGreen: new Color(109, 63, 35),
                darkBrown: new Color(56, 35, 17),
                greenLengthDensity: new Vector2(1, 1),
                brownLengthDensity: new Vector2(MinDetailLength, 0.5f),
                maxHeight: 6,
                baseDensity: 0.8f
            ),

            // Clouds
            new(
                lightGreen: new Color(240, 245, 250),
                lightBrown: new Color(15, 20, 140),
                darkGreen: new Color(180, 180, 190),
                darkBrown: new Color(15, 20, 140),
                greenLengthDensity: new Vector2(MinDetailLength, 1f),
                brownLengthDensity: new Vector2(MinDetailLength, 1f),
                maxHeight: 1,
                baseDensity: 1
            ),
        ];
    }
}
