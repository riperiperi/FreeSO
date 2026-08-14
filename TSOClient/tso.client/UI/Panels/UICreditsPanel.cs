using FSO.Client.UI.Framework;
using FSO.Client.UI.Framework.Parser;
using FSO.Common;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Utils;
using FSO.Content;
using FSO.Files.Formats.IFF.Chunks;
using FSO.Files.RC;
using Microsoft.Xna.Framework;
using System.Globalization;

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
        private List<CreditsBlock> Blocks;
        private float ScrollSpeed = 21; //pixels per second
        private float ActiveScroll;
        private FSO3DCredits[] RemeshCredits;
        private Dictionary<string, List<GameObjectReference>> ObjectsByFilename;

        public UICreditsPanel()
        {
            RemeshCredits = Content.Content.Get().RCMeshes.Packages.GetCredits();
            BaseStyle = TextStyle.DefaultLabel.Clone();
        }

        public void Init(bool fso)
        {
            ScrollSpeed = 21;
            ActiveScroll = 0;

            Blocks = BuildBlocks(fso ? FreeSOCredits() : MaxisCredits());
        }

        private Dictionary<string, List<GameObjectReference>> EnsureObjectsByFilename()
        {
            if (ObjectsByFilename == null)
            {
                var objProvider = Content.Content.Get().WorldObjects;

                var byFilename = new Dictionary<string, List<GameObjectReference>>();

                foreach (var item in objProvider.Entries)
                {
                    var filename = Path.GetFileNameWithoutExtension(item.Value.FileName);

                    if (!byFilename.TryGetValue(filename, out var list))
                    {
                        list = [];
                        byFilename.Add(filename, list);
                    }

                    list.Add(item.Value);
                }

                ObjectsByFilename = byFilename;
            }

            return ObjectsByFilename;
        }

        private IEnumerable<string> RemeshPackageCredits()
        {
            TextStyle measure = BaseStyle.Clone();
            measure.Size = 10;
            var maxCreditWidth = 210;

            var smallNames = new List<string>(2);
            var largeNames = new List<string>(2);

            foreach (var package in RemeshCredits)
            {
                yield return "NewLine|25|13|247,232,145";
                yield return $"LineEntry|Center|{package.Metadata.Name.ToUpper()}|247,232,145";
                yield return "NewLine|5|7|180,210,226";

                foreach (var author in package.Authors)
                {
                    yield return "NewLine|25|12|210,240,250";
                    yield return $"LineEntry|Center|{author.Metadata.Name}|210,240,250";
                    yield return "NewLine|5|7|180,210,226";

                    var groups = author.Groups;

                    foreach (var group in author.Groups)
                    {
                        var name = group.Metadata.Name;
                        var width = measure.MeasureString(name).X;

                        if (width > maxCreditWidth)
                        {
                            largeNames.Add(name);
                        }
                        else
                        {
                            smallNames.Add(name);
                        }

                        if (smallNames.Count == 2)
                        {
                            yield return "NewLine|25|10|180,210,226";
                            yield return $"LineEntry|Left|{smallNames[0]}";
                            yield return $"LineEntry|Right|{smallNames[1]}";

                            smallNames.Clear();

                            foreach (var largeName in largeNames)
                            {
                                yield return "NewLine|25|10|180,210,226";
                                yield return $"LineEntry|Center|{largeName}";
                            }

                            largeNames.Clear();
                        }
                    }

                    largeNames.AddRange(smallNames);
                    smallNames.Clear();

                    foreach (var largeName in largeNames)
                    {
                        yield return "NewLine|25|10|180,210,226";
                        yield return $"LineEntry|Center|{largeName}";
                    }

                    largeNames.Clear();

                    yield return "NewLine|10|10|180,210,226";
                }
            }

            yield break;
        }

        private IEnumerable<string> ObjectFileCredits(string arguments)
        {
            var split = arguments.Split('|');

            if (split.Length < 2)
            {
                yield break;
            }

            var filename = split[1];
            var iffs = EnsureObjectsByFilename();

            TextStyle measure = BaseStyle.Clone();
            measure.Size = 10;
            var maxCreditWidth = ((int)Size.X) - 20;

            bool printedFilename = false;

            float filenameWidth = measure.MeasureString(filename).X;

            // List the objects belonging to this iff file.
            if (filename.EndsWith(".iff") && iffs.TryGetValue(filename[..^4], out var iffObjs))
            {
                HashSet<uint> guidWhitelist = null;
                if (split.Length >= 3)
                {
                    var whitelistStr = split[2];
                    if (whitelistStr != "")
                    {
                        guidWhitelist = [];
                        if (whitelistStr != "x")
                        {
                            var whitelistSplit = whitelistStr.Split(",");

                            foreach (var item in whitelistSplit)
                            {
                                if (uint.TryParse(item, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint guid))
                                {
                                    guidWhitelist.Add(guid);
                                }
                            }
                        }
                    }
                }

                HashSet<string> seenNames = [];

                foreach (var obj in iffObjs)
                {
                    if ((obj.SubIndex != -1 && obj.Group != 0) || (guidWhitelist != null && !guidWhitelist.Contains((uint)obj.ID)))
                    {
                        continue;
                    }

                    // Try to get the object's CTSS.

                    var res = obj.Get();

                    var ctss = res.Resource.Get<CTSS>(res.OBJ.CatalogStringsID);
                    string name = ctss?.GetString(0);

                    if (string.IsNullOrEmpty(name) || seenNames.Contains(name))
                    {
                        continue;
                    }

                    seenNames.Add(name);

                    if (!printedFilename)
                    {
                        bool tooBig = measure.MeasureString(name).X + filenameWidth > maxCreditWidth;
                        yield return $"NewLine|{(tooBig ? "18" : "0")}|10|210,240,250";
                        yield return $"LineEntry|Left|{filename}";
                        printedFilename = true;
                    }

                    yield return "NewLine|18|10|180,210,226";
                    yield return $"LineEntry|Right|{name}";
                }
            }

            if (split.Length >= 4)
            {
                // Extra items for this iff
                var extraStr = split[3];
                var extraSplit = extraStr.Split(",");

                foreach (var extra in extraSplit)
                {
                    if (!printedFilename)
                    {
                        bool tooBig = measure.MeasureString(extra).X + filenameWidth > maxCreditWidth;
                        yield return $"NewLine|{(tooBig ? "18" : "0")}|10|210,240,250";
                        yield return $"LineEntry|Left|{filename}";
                        printedFilename = true;
                    }

                    yield return "NewLine|18|10|180,210,226";
                    yield return $"LineEntry|Right|{extra}";
                }
            }

            yield return "NewLine|8|10|180,210,226";
        }

        private IEnumerable<string> CSTCredits(string cst)
        {
            int index = 1;
            var strings = GameFacade.Strings;

            bool hasValue = true;
            do
            {
                string message = strings.GetString(cst, index.ToString());

                index++;

                if (!string.IsNullOrEmpty(message))
                {
                    if (message == "RemeshPackage")
                    {
                        foreach (var line in RemeshPackageCredits())
                        {
                            yield return line;
                        }
                    }
                    else if (message.StartsWith("ObjectFile|"))
                    {
                        foreach (var line in ObjectFileCredits(message))
                        {
                            yield return line;
                        }
                    }
                    else
                    {
                        yield return message;
                    } 
                }
                else
                {
                    hasValue = false;
                }
            }
            while (hasValue);

            yield break;
        }

        private IEnumerable<string> MaxisCredits()
        {
            return CSTCredits("242");
        }

        private IEnumerable<string> FreeSOCredits()
        {
            return CSTCredits("f200");
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
            float scrollHeight = lastBlock.Y + lastBlock.LineInfo.LineHeight + areaHeight;

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

            int i = 0;
            foreach (var block in Blocks)
            {
                i++;
                float top = block.Y + areaHeight - ActiveScroll;
                float bottom = top + block.LineInfo.LineHeight;

                if (top > areaHeight)
                {
                    break;
                }

                if (bottom > 0)
                {
                    // Draw this item
                    var opacityHeight = block.LineInfo.LineHeight;

                    if (opacityHeight == 0)
                    {
                        var nextIndex = Blocks.FindIndex(i, (x) => x.LineInfo.LineHeight != 0);

                        if (nextIndex != -1)
                        {
                            opacityHeight = Blocks[nextIndex].LineInfo.LineHeight;
                            bottom = top + opacityHeight;
                        }
                    }

                    float edgeDist = Math.Min(Math.Max(top - opacityHeight, 0), Math.Max(areaHeight - bottom, 0));
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
