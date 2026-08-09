using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace FSO.ContactSheet
{
    /// <summary>
    /// Minimal built-in 5x7 pixel font (uppercase + digits + a handful of symbols) so contact
    /// sheet labels don't need System.Drawing/libgdiplus (not portable in this dev
    /// environment — see PngWriter.cs's doc comment for the same constraint). Covers only the
    /// characters labels actually use: object ids and "clone:0x..."/"gen:name" source tags,
    /// uppercased before drawing.
    /// </summary>
    public static class BitmapFont
    {
        const int GlyphW = 5, GlyphH = 7;

        // Each glyph: 7 rows, top to bottom, 5 chars per row, '#' = lit pixel, '.' = empty.
        static readonly Dictionary<char, string[]> Glyphs = new Dictionary<char, string[]>
        {
            ['A'] = new[] { ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
            ['B'] = new[] { "####.", "#...#", "#...#", "####.", "#...#", "#...#", "####." },
            ['C'] = new[] { ".###.", "#...#", "#....", "#....", "#....", "#...#", ".###." },
            ['D'] = new[] { "####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####." },
            ['E'] = new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#####" },
            ['F'] = new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#...." },
            ['G'] = new[] { ".###.", "#...#", "#....", "#.###", "#...#", "#...#", ".###." },
            ['H'] = new[] { "#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
            ['I'] = new[] { ".###.", "..#..", "..#..", "..#..", "..#..", "..#..", ".###." },
            ['J'] = new[] { "...##", "....#", "....#", "....#", "....#", "#...#", ".###." },
            ['K'] = new[] { "#...#", "#..#.", "#.#..", "##...", "#.#..", "#..#.", "#...#" },
            ['L'] = new[] { "#....", "#....", "#....", "#....", "#....", "#....", "#####" },
            ['M'] = new[] { "#...#", "##.##", "#.#.#", "#.#.#", "#...#", "#...#", "#...#" },
            ['N'] = new[] { "#...#", "##..#", "#.#.#", "#..##", "#...#", "#...#", "#...#" },
            ['O'] = new[] { ".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." },
            ['P'] = new[] { "####.", "#...#", "#...#", "####.", "#....", "#....", "#...." },
            ['Q'] = new[] { ".###.", "#...#", "#...#", "#...#", "#.#.#", "#..#.", ".##.#" },
            ['R'] = new[] { "####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#" },
            ['S'] = new[] { ".####", "#....", "#....", ".###.", "....#", "....#", "####." },
            ['T'] = new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.." },
            ['U'] = new[] { "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." },
            ['V'] = new[] { "#...#", "#...#", "#...#", "#...#", "#...#", ".#.#.", "..#.." },
            ['W'] = new[] { "#...#", "#...#", "#...#", "#.#.#", "#.#.#", "##.##", "#...#" },
            ['X'] = new[] { "#...#", "#...#", ".#.#.", "..#..", ".#.#.", "#...#", "#...#" },
            ['Y'] = new[] { "#...#", "#...#", ".#.#.", "..#..", "..#..", "..#..", "..#.." },
            ['Z'] = new[] { "#####", "....#", "...#.", "..#..", ".#...", "#....", "#####" },
            ['0'] = new[] { ".###.", "#...#", "#..##", "#.#.#", "##..#", "#...#", ".###." },
            ['1'] = new[] { "..#..", ".##..", "..#..", "..#..", "..#..", "..#..", ".###." },
            ['2'] = new[] { ".###.", "#...#", "....#", "...#.", "..#..", ".#...", "#####" },
            ['3'] = new[] { ".###.", "#...#", "....#", "..##.", "....#", "#...#", ".###." },
            ['4'] = new[] { "#..#.", "#..#.", "#..#.", "#####", "...#.", "...#.", "...#." },
            ['5'] = new[] { "#####", "#....", "####.", "....#", "....#", "#...#", ".###." },
            ['6'] = new[] { ".###.", "#...#", "#....", "####.", "#...#", "#...#", ".###." },
            ['7'] = new[] { "#####", "....#", "...#.", "..#..", ".#...", ".#...", ".#..." },
            ['8'] = new[] { ".###.", "#...#", "#...#", ".###.", "#...#", "#...#", ".###." },
            ['9'] = new[] { ".###.", "#...#", "#...#", ".####", "....#", "#...#", ".###." },
            [' '] = new[] { ".....", ".....", ".....", ".....", ".....", ".....", "....." },
            [':'] = new[] { ".....", "..#..", "..#..", ".....", "..#..", "..#..", "....." },
            ['_'] = new[] { ".....", ".....", ".....", ".....", ".....", ".....", "#####" },
            ['-'] = new[] { ".....", ".....", ".....", "#####", ".....", ".....", "....." },
            ['.'] = new[] { ".....", ".....", ".....", ".....", ".....", "..##.", "..##." },
            ['('] = new[] { "...#.", "..#..", ".#...", ".#...", ".#...", "..#..", "...#." },
            [')'] = new[] { ".#...", "..#..", "...#.", "...#.", "...#.", "..#..", ".#..." },
            ['/'] = new[] { "....#", "...#.", "...#.", "..#..", ".#...", ".#...", "#...." },
            ['?'] = new[] { ".###.", "#...#", "....#", "...#.", "..#..", ".....", "..#.." },
        };

        /// <summary>Pixel width of <paramref name="text"/> rendered at the given scale, including inter-glyph spacing.</summary>
        public static int MeasureWidth(string text, int scale)
        {
            return text.Length * (GlyphW + 1) * scale;
        }

        public static int LineHeight(int scale) => GlyphH * scale;

        /// <summary>
        /// Draws uppercased <paramref name="text"/> into <paramref name="canvas"/> (row-major,
        /// width x height) at (x, y), each font pixel expanded to a scale x scale block.
        /// Unknown characters (not in Glyphs) draw as a blank cell, not an error — labels are
        /// diagnostic aids, not something that should abort the sheet over a stray character.
        /// </summary>
        public static void DrawText(Color[] canvas, int canvasWidth, int canvasHeight, int x, int y, string text, Color color, int scale = 2)
        {
            var upper = text.ToUpperInvariant();
            int cursorX = x;
            foreach (var ch in upper)
            {
                if (Glyphs.TryGetValue(ch, out var rows))
                {
                    for (int gy = 0; gy < GlyphH; gy++)
                        for (int gx = 0; gx < GlyphW; gx++)
                        {
                            if (rows[gy][gx] != '#') continue;
                            for (int sy = 0; sy < scale; sy++)
                                for (int sx = 0; sx < scale; sx++)
                                    PlotPixel(canvas, canvasWidth, canvasHeight, cursorX + gx * scale + sx, y + gy * scale + sy, color);
                        }
                }
                cursorX += (GlyphW + 1) * scale;
            }
        }

        static void PlotPixel(Color[] canvas, int width, int height, int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= width || y >= height) return;
            canvas[y * width + x] = color;
        }
    }
}
