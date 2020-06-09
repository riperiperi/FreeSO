/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Client.UI.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FSO.Common.Utils;

namespace FSO.Client.UI.Framework
{
    public class TextRenderer
    {
        public static void DrawText(List<ITextDrawCmd> cmds, UIElement target, SpriteBatch batch)
        {
            /**
             * Draw text
             */
            foreach (var cmd in cmds)
            {
                cmd.Draw(target, batch);
            }
        }

        /// <summary>
        /// Computes drawing commands to layout a block of text within
        /// certain constraints
        /// </summary>
        /// <returns></returns>
        public static TextRendererResult ComputeText(string text, TextRendererOptions options, UIElement target)
        {
            var TextStyle = options.TextStyle;
            var _Scale = options.Scale;
            var txtScale = TextStyle.Scale * _Scale;

            var m_LineHeight = TextStyle.MeasureString("W").Y - (2 * txtScale.Y);
            var spaceWidth = TextStyle.MeasureString(" ").X;

            text = text.Replace("\r", "");
            var bbCommands = new List<BBCodeCommand>();
            if (options.BBCode)
            {
                var parsed = new BBCodeParser(text);
                text = parsed.Stripped;
                bbCommands = parsed.Commands;
            }
            var words = text.Split(' ').ToList();
            var newWordsArray = TextRenderer.ExtractLineBreaks(words);

            var m_Lines = new List<UITextEditLine>();
            TextRenderer.CalculateLines(m_Lines, newWordsArray, TextStyle, options.MaxWidth, spaceWidth, options.TopLeftIconSpace, m_LineHeight);

            var topLeft = options.Position;
            var position = topLeft;

            var result = new TextRendererResult();
            var drawCommands = new List<ITextDrawCmd>();
            result.DrawingCommands = drawCommands;

            var yPosition = topLeft.Y;
            var numLinesAdded = 0;
            var realMaxWidth = 0;

            var bbIndex = 0;
            var bbColorStack = new Stack<Color>();
            var lastColor = TextStyle.Color;
            var shadowApplied = false;

            for (var i = 0; i < m_Lines.Count; i++)
            {
                var lineOffset = (i*m_LineHeight < options.TopLeftIconSpace.Y) ? options.TopLeftIconSpace.X : 0;
                var line = m_Lines[i];
                var segments = CalculateSegments(line, bbCommands, ref bbIndex);

                var xPosition = topLeft.X+lineOffset;

                segments.ForEach(x => x.Size = TextStyle.MeasureString(x.Text));
                var thisLineWidth = segments.Sum(x => x.Size.X);

                if (thisLineWidth > realMaxWidth) realMaxWidth = (int)thisLineWidth;

                /** Alignment **/
                if (options.Alignment == TextAlignment.Center)
                {
                    xPosition += (int)Math.Round(((options.MaxWidth-lineOffset) - thisLineWidth) / 2);
                }


                foreach (var segment in segments)
                {
                    var segmentSize = segment.Size;
                    var segmentPosition = target.LocalPoint(new Vector2(xPosition, yPosition));

                    if (segment.Text.Length > 0)
                    {
                        drawCommands.Add(new TextDrawCmd_Text
                        {
                            Selected = segment.Selected,
                            Text = segment.Text,
                            Style = TextStyle,
                            Position = segmentPosition,
                            Scale = txtScale
                        });
                        xPosition += segmentSize.X;
                    }

                    if (segment.StartCommand != null)
                    {
                        var cmd = segment.StartCommand;
                        switch (cmd.Type)
                        {
                            case BBCodeCommandType.color:
                                if (cmd.Close)
                                {
                                    //pop a color off our stack
                                    if (bbColorStack.Count > 0)
                                    {
                                        lastColor = bbColorStack.Pop();
                                        drawCommands.Add(new TextDrawCmd_Color(TextStyle, lastColor));
                                    }
                                }
                                else
                                {
                                    bbColorStack.Push(lastColor);
                                    lastColor = cmd.ParseColor();
                                    drawCommands.Add(new TextDrawCmd_Color(TextStyle, lastColor));
                                }
                                break;
                            case BBCodeCommandType.s:
                                if (cmd.Close)
                                {
                                    drawCommands.Add(new TextDrawCmd_Shadow(TextStyle, false));
                                    shadowApplied = false;
                                }
                                else
                                {
                                    drawCommands.Add(new TextDrawCmd_Shadow(TextStyle, true));
                                    shadowApplied = true;
                                }
                                break;
                            case BBCodeCommandType.emoji:
                                if (segment.BBCatchup) break;
                                drawCommands.Add(new TextDrawCmd_Emoji(TextStyle, cmd.Parameter, target.LocalPoint(new Vector2(xPosition, yPosition)), _Scale));
                                break;
                        }
                    }
                }

                /*
                var segmentPosition = target.LocalPoint(new Vector2(xPosition, yPosition));
                drawCommands.Add(new TextDrawCmd_Text
                {
                    Selected = false,
                    Text = line.Text,
                    Style = TextStyle,
                    Position = segmentPosition,
                    Scale = txtScale
                });
                */

                numLinesAdded++;

                yPosition += m_LineHeight;
                position.Y += m_LineHeight;
            }

            if (shadowApplied) drawCommands.Add(new TextDrawCmd_Shadow(TextStyle, false));

            while (bbColorStack.Count > 0)
            {
                lastColor = bbColorStack.Pop();
                drawCommands.Add(new TextDrawCmd_Color(TextStyle, lastColor));
            }

            result.BoundingBox = new Rectangle((int)topLeft.X, (int)topLeft.Y, (int)options.MaxWidth, (int)(yPosition-(m_LineHeight + topLeft.Y)));
            result.MaxWidth = realMaxWidth;
            foreach (var cmd in drawCommands)
            {
                cmd.Init();
            }

            return result;
        }

        /// <summary>
        /// Creates a list of segments to split the line into
        /// in order to draw selection boxes
        /// </summary>
        /// <returns></returns>
        protected static List<UITextEditLineSegment> CalculateSegments(UITextEditLine line, List<BBCodeCommand> bbcmds, ref int bbind)
        {
            var result = new List<UITextEditLineSegment>();

            var points = new List<Tuple<int, BBCodeCommand>>();

            var lineStart = line.StartIndex;
            var lineEnd = lineStart + line.Text.Length;

            var lastMod = 0;
            while (true)
            {
                //check for selection first
                var nextBB = (bbind == bbcmds.Count) ? lineEnd : bbcmds[bbind].Index;

                if (nextBB < lineEnd)
                {
                    //this bbcmd happens on this line (or before we even drew anything (scroll))
                    //
                    if (bbcmds[bbind].Index - lineStart < 0)
                    {
                        result.Add(new UITextEditLineSegment
                        {
                            Selected = false,
                            Text = "",
                            BBCatchup = true,
                            StartCommand = bbcmds[bbind++]
                        });
                    }
                    else
                    {
                        result.Add(new UITextEditLineSegment
                        {
                            Selected = false,
                            Text = line.Text.Substring(lastMod, (bbcmds[bbind].Index - lineStart) - lastMod),
                            StartCommand = bbcmds[bbind++]
                        });
                        lastMod = (bbcmds[bbind - 1].Index - lineStart);
                    }
                }
                else
                {
                    //remainder of the line
                    result.Add(new UITextEditLineSegment
                    {
                        Selected = false,
                        Text = line.Text.Substring(lastMod, (lineEnd - lineStart) - lastMod),
                    });
                    break;
                }
            }
            return result;


        }

        public static void CalculateLines(List<UITextEditLine> m_Lines, List<string> newWordsArray, TextStyle TextStyle, float lineWidth, float spaceWidth, Vector2 topLeftIconSpace, float lineHeight)
        {
            var currentLine = new StringBuilder();
            var currentLineWidth = 0.0f;
            var currentLineNum = 0;

            for (var i = 0; i < newWordsArray.Count; i++)
            {
                var allowedWidth = (currentLineNum*lineHeight<topLeftIconSpace.Y)?lineWidth-topLeftIconSpace.X:lineWidth;
                var word = newWordsArray[i];

                if (word == "\n")
                {
                    /** Line break **/
                    m_Lines.Add(new UITextEditLine
                    {
                        Text = currentLine.ToString(),
                        LineWidth = currentLineWidth,
                        LineNumber = currentLineNum,
                        WhitespaceSuffix = 0
                    });
                    currentLineNum++;
                    currentLine = new StringBuilder();

                    currentLineWidth = 0;
                }
                else// if (word.Length > 0)
                {
                    bool wordWritten = false;
                    while (!wordWritten) //repeat until the full word is written (as part of it can be written each pass if it is too long)
                    {
                        var wordSize = TextStyle.MeasureString(word);

                        if (wordSize.X > allowedWidth)
                        {
                            //SPECIAL CASE, word is bigger than line width and cannot fit on its own line
                            if (currentLineWidth > 0)
                            {
                                //if there are words on this line, we'll start this one on the next to get the most space for it
                                m_Lines.Add(new UITextEditLine
                                {
                                    Text = currentLine.ToString(),
                                    LineWidth = currentLineWidth,
                                    LineNumber = currentLineNum
                                });
                                currentLineNum++;
                                currentLine = new StringBuilder();
                                currentLineWidth = 0;
                            }

                            // binary search, makes this a bit faster?
                            // we can safely say that no character is thinner than 4px, so set max substring to maxwidth/4
                            float width = allowedWidth + 1;
                            int min = 1;
                            int max = Math.Min(word.Length, (int)allowedWidth / 4);
                            int mid = (min + max) / 2;
                            while (max-min > 1)
                            {
                                width = TextStyle.MeasureString(word.Substring(0, mid)).X;                    
                                if (width > allowedWidth)
                                    max = mid;
                                else
                                    min = mid;
                                mid = (max + min) / 2;
                            }
                            //min = Math.Min(min, word.Length);
                            currentLine.Append(word.Substring(0, min));
                            currentLineWidth += width;
                            word = word.Substring(min);

                            m_Lines.Add(new UITextEditLine
                            {
                                Text = currentLine.ToString(),
                                LineWidth = currentLineWidth,
                                LineNumber = currentLineNum,
                                WhitespaceSuffix = 0
                            });

                            currentLineNum++;
                            currentLine = new StringBuilder();
                            currentLineWidth = 0;
                            if (word.Length == 0) wordWritten = true;
                        }
                        else if (currentLineWidth + wordSize.X < allowedWidth)
                        {
                            currentLine.Append(word);
                            if (i != newWordsArray.Count - 1) { currentLine.Append(' '); currentLineWidth += spaceWidth; }
                            currentLineWidth += wordSize.X;
                            wordWritten = true;
                        }
                        else
                        {
                            /** New line **/
                            m_Lines.Add(new UITextEditLine
                            {
                                Text = currentLine.ToString(),
                                LineWidth = currentLineWidth,
                                LineNumber = currentLineNum,
                                WhitespaceSuffix = 0
                            });
                            currentLineNum++;
                            currentLine = new StringBuilder();
                            currentLine.Append(word);
                            currentLineWidth = wordSize.X;
                            if (i != newWordsArray.Count - 1) { currentLine.Append(' '); currentLineWidth += spaceWidth; }
                            wordWritten = true;
                        }
                    }
                }
            }

            m_Lines.Add(new UITextEditLine //add even if length is 0, so we can move the cursor down!
            {
                Text = currentLine.ToString(),
                LineWidth = currentLineWidth,
                LineNumber = currentLineNum
            });

            var currentIndex = 0;
            foreach (var line in m_Lines)
            {
                line.StartIndex = currentIndex;
                currentIndex += line.Text.Length + line.WhitespaceSuffix;
            }
        }

        public static List<string> ExtractLineBreaks(List<string> words)
        {
            /**
             * Modify the array to make manual line breaks their own segment
             * in the array
             */
            var newWordsArray = new List<string>();
            for (var i = 0; i < words.Count; i++)
            {
                var word = words[i];
                var breaks = word.Split(new string[] { "\n" }, StringSplitOptions.None);

                for (var x = 0; x < breaks.Length; x++)
                {
                    newWordsArray.Add(breaks[x]);
                    if (x != breaks.Length - 1)
                    {
                        newWordsArray.Add("\n");
                    }
                }
            }

            return newWordsArray;
        }
    }

    public class TextRendererResult
    {
        public List<ITextDrawCmd> DrawingCommands;
        public Rectangle BoundingBox;
        public int MaxWidth;
        public int Lines;
    }

    public class TextRendererOptions
    {
        public bool WordWrap;
        public bool BBCode;
        public int MaxWidth;
        public TextStyle TextStyle;
        public Vector2 Position;
        public Vector2 Scale;
        public TextAlignment Alignment;
        public Vector2 TopLeftIconSpace; //space to wrap around where an icon should be.
    }
}