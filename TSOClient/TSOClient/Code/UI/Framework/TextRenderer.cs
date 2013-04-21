using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TSOClient.Code.UI.Framework
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

            var words = text.Split(' ').ToList();
            var newWordsArray = TextRenderer.ExtractLineBreaks(words);

            var m_Lines = new List<UITextEditLine>();
            TextRenderer.CalculateLines(m_Lines, newWordsArray, TextStyle, options.MaxWidth, spaceWidth);


            var topLeft = options.Position;
            var position = topLeft;

            var result = new TextRendererResult();
            var drawCommands = new List<ITextDrawCmd>();
            result.DrawingCommands = drawCommands;

            var yPosition = topLeft.Y;
            var numLinesAdded = 0;
            for (var i = 0; i < m_Lines.Count; i++)
            {
                var line = m_Lines[i];
                var xPosition = topLeft.X;

                /** Alignment **/
                if (options.Alignment == TextAlignment.Center)
                {
                    xPosition += (int)Math.Round((options.MaxWidth - line.LineWidth) / 2);
                }

                var segmentPosition = target.LocalPoint(new Vector2(xPosition, yPosition));
                drawCommands.Add(new TextDrawCmd_Text
                {
                    Selected = false,
                    Text = line.Text,
                    Style = TextStyle,
                    Position = segmentPosition,
                    Scale = txtScale
                });
                numLinesAdded++;


                yPosition += m_LineHeight;
                position.Y += m_LineHeight;
            }

            result.BoundingBox = new Rectangle((int)topLeft.X, (int)topLeft.Y, (int)options.MaxWidth, (int)yPosition);

            return result;
        }


        public static void CalculateLines(List<UITextEditLine> m_Lines, List<string> newWordsArray, TextStyle TextStyle, float lineWidth, float spaceWidth)
        {
            var currentLine = new StringBuilder();
            var currentLineWidth = 0.0f;
            var currentLineNum = 0;

            for (var i = 0; i < newWordsArray.Count; i++)
            {
                var word = newWordsArray[i];

                if (word == "\r\n")
                {
                    /** Line break **/
                    m_Lines.Add(new UITextEditLine
                    {
                        Text = currentLine.ToString(),
                        LineWidth = currentLineWidth,
                        LineNumber = currentLineNum,
                        WhitespaceSuffix = 2
                    });
                    currentLineNum++;
                    currentLine = new StringBuilder();

                    currentLineWidth = 0;
                }
                else
                {
                    var wordSize = TextStyle.MeasureString(word);

                    if (currentLineWidth + wordSize.X < lineWidth)
                    {
                        currentLine.Append(word);
                        currentLine.Append(' ');
                        currentLineWidth += wordSize.X;
                        currentLineWidth += spaceWidth;
                    }
                    else
                    {
                        /** New line **/
                        m_Lines.Add(new UITextEditLine
                        {
                            Text = currentLine.ToString(),
                            LineWidth = currentLineWidth,
                            LineNumber = currentLineNum
                        });
                        currentLineNum++;
                        currentLine = new StringBuilder();
                        currentLine.Append(word);
                        currentLine.Append(' ');

                        currentLineWidth = wordSize.X + spaceWidth;
                    }
                }
            }

            if (currentLine.Length > 0)
            {
                m_Lines.Add(new UITextEditLine
                {
                    Text = currentLine.ToString(),
                    LineWidth = currentLineWidth,
                    LineNumber = currentLineNum
                });
            }

            var currentIndex = 0;
            foreach (var line in m_Lines)
            {
                line.StartIndex = currentIndex;
                currentIndex += (line.Text.Length - 1) + line.WhitespaceSuffix;
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
                var breaks = word.Split(new string[] { "\r\n" }, StringSplitOptions.None);

                for (var x = 0; x < breaks.Length; x++)
                {
                    newWordsArray.Add(breaks[x]);
                    if (x != breaks.Length - 1)
                    {
                        newWordsArray.Add("\r\n");
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
    }

    public class TextRendererOptions
    {
        public bool WordWrap;
        public int MaxWidth;
        public TextStyle TextStyle;
        public Vector2 Position;
        public Vector2 Scale;
        public TextAlignment Alignment;
    }
}
