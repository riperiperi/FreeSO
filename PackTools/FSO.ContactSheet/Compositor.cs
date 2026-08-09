using System;
using System.Collections.Generic;
using System.Linq;
using FSO.PackCompiler.ArtGen;
using Microsoft.Xna.Framework;

namespace FSO.ContactSheet
{
    /// <summary>Lays out ContactSheetBuilder.Cell rows into one composited, labeled PNG.</summary>
    public static class Compositor
    {
        const int Pad = 8;
        const int LabelColumnWidth = 260;
        const int HeaderHeight = 24;
        static readonly Color Background = new Color(30, 30, 34, 255);
        static readonly Color CellBorder = new Color(70, 70, 78, 255);
        static readonly Color LabelColor = new Color(230, 230, 230, 255);
        static readonly Color ErrorColor = new Color(230, 90, 90, 255);
        static readonly string[] ZoomOrder = { "FAR", "MEDIUM", "NEAR" };

        public static void WriteSheet(List<ContactSheetBuilder.Cell> cells, string outPath)
        {
            // Column width = the widest frame seen in that zoom column, across every row —
            // keeps every cell in a column the same width so silhouettes are directly comparable.
            var colWidth = new Dictionary<string, int>();
            foreach (var zoom in ZoomOrder) colWidth[zoom] = 40;
            foreach (var cell in cells)
                foreach (var zoom in ZoomOrder)
                    if (cell.FramesByZoom.TryGetValue(zoom, out var f))
                        colWidth[zoom] = Math.Max(colWidth[zoom], f.Width);

            var rowHeight = new int[cells.Count];
            for (int i = 0; i < cells.Count; i++)
            {
                var maxH = 40;
                foreach (var zoom in ZoomOrder)
                    if (cells[i].FramesByZoom.TryGetValue(zoom, out var f))
                        maxH = Math.Max(maxH, f.Height);
                rowHeight[i] = maxH;
            }

            int gridWidth = ZoomOrder.Sum(z => colWidth[z] + Pad) + Pad;
            int width = LabelColumnWidth + gridWidth;
            int height = HeaderHeight + rowHeight.Sum(h => h + Pad) + Pad;

            var canvas = new Color[width * height];
            for (int i = 0; i < canvas.Length; i++) canvas[i] = Background;

            // header: zoom column names
            int hx = LabelColumnWidth + Pad;
            foreach (var zoom in ZoomOrder)
            {
                BitmapFont.DrawText(canvas, width, height, hx, 4, zoom, LabelColor, scale: 1);
                hx += colWidth[zoom] + Pad;
            }

            int y = HeaderHeight;
            for (int i = 0; i < cells.Count; i++)
            {
                var cell = cells[i];
                DrawRowBorder(canvas, width, height, 0, y, width - 1, rowHeight[i]);

                // label: object id + source, wrapped onto up to two lines if it doesn't fit
                var labelY = y + Pad / 2;
                foreach (var line in WrapLabel(cell.Label, LabelColumnWidth - Pad * 2))
                {
                    BitmapFont.DrawText(canvas, width, height, Pad, labelY, line, LabelColor, scale: 1);
                    labelY += BitmapFont.LineHeight(1) + 2;
                }
                if (cell.Errors.Count > 0)
                {
                    var errLine = "! " + cell.Errors[0];
                    if (errLine.Length > 40) errLine = errLine.Substring(0, 40);
                    BitmapFont.DrawText(canvas, width, height, Pad, labelY, errLine, ErrorColor, scale: 1);
                }

                int x = LabelColumnWidth + Pad;
                foreach (var zoom in ZoomOrder)
                {
                    if (cell.FramesByZoom.TryGetValue(zoom, out var frame))
                        BlitCentered(canvas, width, height, x, y, colWidth[zoom], rowHeight[i], frame);
                    x += colWidth[zoom] + Pad;
                }

                y += rowHeight[i] + Pad;
            }

            PngWriter.Write(outPath, canvas, width, height);
        }

        static IEnumerable<string> WrapLabel(string label, int maxWidthPx)
        {
            var maxChars = Math.Max(4, maxWidthPx / BitmapFont.MeasureWidth("A", 1));
            if (label.Length <= maxChars) { yield return label; yield break; }
            yield return label.Substring(0, maxChars);
            var rest = label.Substring(maxChars);
            yield return rest.Length > maxChars ? rest.Substring(0, maxChars) : rest;
        }

        static void BlitCentered(Color[] canvas, int canvasWidth, int canvasHeight, int cellX, int cellY, int cellW, int cellH, ContactSheetBuilder.RenderedFrame frame)
        {
            int offsetX = cellX + (cellW - frame.Width) / 2;
            int offsetY = cellY + (cellH - frame.Height) / 2;
            for (int py = 0; py < frame.Height; py++)
                for (int px = 0; px < frame.Width; px++)
                {
                    var src = frame.Pixels[py * frame.Width + px];
                    if (src.A == 0) continue; // transparent background pixel — leave the canvas color
                    int dx = offsetX + px, dy = offsetY + py;
                    if (dx < 0 || dy < 0 || dx >= canvasWidth || dy >= canvasHeight) continue;
                    canvas[dy * canvasWidth + dx] = src;
                }
        }

        static void DrawRowBorder(Color[] canvas, int canvasWidth, int canvasHeight, int x, int y, int w, int h)
        {
            for (int px = x; px < x + w && px < canvasWidth; px++)
            {
                if (y < canvasHeight) canvas[y * canvasWidth + px] = CellBorder;
                var bottom = y + h;
                if (bottom < canvasHeight) canvas[bottom * canvasWidth + px] = CellBorder;
            }
        }
    }
}
