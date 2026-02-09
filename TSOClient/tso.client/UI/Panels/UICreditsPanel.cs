using FSO.Client.UI.Framework;
using FSO.Client.UI.Framework.Parser;
using FSO.Common;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Utils;
using Microsoft.Xna.Framework;

namespace FSO.Client.UI.Panels
{
    public class UICreditsPanel : UIElement
    {
        private struct CreditsNewLine
        {
            public int LineHeight;
            public int FontSize;
            public Color FontColor;

            public CreditsNewLine(string input)
            {
                // After NewLine|
                var split = input[8..].Split('|');

                LineHeight = int.Parse(split[0]);
                FontSize = int.Parse(split[1]);
                
                var colorSplit = split[2].Split(',');

                FontColor = new Color(byte.Parse(colorSplit[0]), byte.Parse(colorSplit[1]), byte.Parse(colorSplit[2]), (byte)255);
            }
        }

        private enum CreditsAlignment
        {
            Left,
            Center,
            Right,
        }

        private struct CreditsLineEntry
        {
            public CreditsAlignment Alignment;
            public int NumericAlignment;
            public string Text;
            public Color? UnderlineColor;

            public CreditsLineEntry(string input)
            {
                // After LineEntry|
                var split = input[10..].Split('|');

                if (int.TryParse(split[0], out NumericAlignment))
                {
                    Alignment = CreditsAlignment.Left;
                }
                else
                {
                    Alignment = split[0] switch
                    {
                        "Left" => CreditsAlignment.Left,
                        "Right" => CreditsAlignment.Right,
                        _ => CreditsAlignment.Center
                    };
                }

                Text = split[1];

                if (split.Length > 2)
                {
                    var colorSplit = split[2].Split(',');
                    UnderlineColor = new Color(byte.Parse(colorSplit[0]), byte.Parse(colorSplit[1]), byte.Parse(colorSplit[2]), (byte)255);
                }
            }
        }

        private struct CreditsBlock
        {
            public CreditsNewLine LineInfo;
            public List<CreditsLineEntry> Entries;
            public int Y;
        }

        [UIAttribute("size")]
        public override Vector2 Size { get; set; }

        private readonly TextStyle BaseStyle;
        private readonly List<CreditsBlock> Blocks;
        private float ScrollSpeed = 21; //pixels per second
        private float ActiveScroll;

        public UICreditsPanel()
        {
            BaseStyle = TextStyle.DefaultLabel.Clone();
            Blocks = BuildBlocks(MaxisCredits());
        }

        private IEnumerable<string> MaxisCredits()
        {
            int index = 1;
            var strings = GameFacade.Strings;

            bool hasValue = true;
            do
            {
                string message = strings.GetString("242", index.ToString());

                index++;

                if (!string.IsNullOrEmpty(message))
                {
                    yield return message;
                }
                else
                {
                    hasValue = false;
                }
            }
            while (hasValue);

            yield break;
        }

        private List<CreditsBlock> BuildBlocks(IEnumerable<string> nextLine)
        {
            var blocks = new List<CreditsBlock>();
            List<CreditsLineEntry> entries = [];
            CreditsNewLine? activeLine = null;
            int yTotal = 0;

            foreach (var line in nextLine)
            {
                if (line.StartsWith("NewLine|"))
                {
                    if (activeLine != null)
                    {
                        var toAdd = activeLine.Value;
                        blocks.Add(new CreditsBlock()
                        {
                            LineInfo = toAdd,
                            Entries = entries,
                            Y = yTotal
                        });

                        entries = [];
                        yTotal += toAdd.LineHeight;
                    }

                    activeLine = new CreditsNewLine(line);
                }
                else if (line.StartsWith("LineEntry|"))
                {
                    var entry = new CreditsLineEntry(line);

                    entries.Add(entry);
                }
            }

            return blocks;
        }

        public override void Update(UpdateState state)
        {
            if (state.MouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed)
            {
                ScrollSpeed = Math.Min(400, ScrollSpeed + (100f / FSOEnvironment.RefreshRate));
            }
            else
            {
                ScrollSpeed = Math.Max(21, ScrollSpeed - (300f / FSOEnvironment.RefreshRate));
            }

            float scrollPerUpdate = ScrollSpeed / FSOEnvironment.RefreshRate;

            ActiveScroll += scrollPerUpdate;
            var lastBlock = Blocks.LastOrDefault();

            // Prepare drawing for any items that are onscreen
            float areaHeight = Size.Y;
            float scrollHeight = lastBlock.Y + lastBlock.LineInfo.LineHeight + areaHeight * 2;

            if (ActiveScroll > scrollHeight)
            {
                ActiveScroll -= scrollHeight;
            }

            /*
            int i = 0;
            foreach (var block in Blocks)
            {
                float top = block.Y + areaHeight - ActiveScroll;
                float bottom = top + block.LineInfo.LineHeight;

                if (top > areaHeight)
                {
                    break;
                }

                if (bottom > 0)
                {
                    // Ensure this credits item can be drawn
                }

                i++;
            }
            */

            base.Update(state);
        }

        public override void Draw(UISpriteBatch SBatch)
        {
            float areaWidth = Size.X;
            float areaHeight = Size.Y;
            var whitePx = TextureGenerator.GetPxWhite(SBatch.GraphicsDevice);
            var style = BaseStyle;

            float edgeMargin = 10;

            foreach (var block in Blocks)
            {
                float top = block.Y + areaHeight - ActiveScroll;
                float bottom = top + block.LineInfo.LineHeight;

                if (top > areaHeight)
                {
                    break;
                }

                if (bottom > 0)
                {
                    // Draw this item
                    float edgeDist = Math.Min(Math.Max(top - block.LineInfo.LineHeight, 0), Math.Max(areaHeight - bottom, 0));
                    float opacity = Math.Clamp(edgeDist / edgeMargin, 0, 1);

                    style.Color = block.LineInfo.FontColor * opacity;
                    style.Size = block.LineInfo.FontSize;

                    foreach (var entry in block.Entries)
                    {
                        float x = 0;
                        var entrySize = style.MeasureString(entry.Text);

                        if (entry.Alignment != CreditsAlignment.Left)
                        {
                            x = entry.Alignment switch
                            {
                                CreditsAlignment.Right => areaWidth - entrySize.X,
                                _ => (areaWidth - entrySize.X) / 2,
                            };
                        }

                        DrawLocalString(SBatch, entry.Text, new Vector2(x, top), style);

                        if (entry.UnderlineColor != null)
                        {
                            DrawLocalTexture(SBatch, whitePx, null, new Vector2(x, top + entrySize.Y), new Vector2(entrySize.X, 1), style.Color);
                        }
                    }
                }
            }
        }
    }
}
