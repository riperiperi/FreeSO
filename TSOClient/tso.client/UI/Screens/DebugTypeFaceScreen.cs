using System;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Controls;
using Microsoft.Xna.Framework;
using FSO.Common.Utils;

namespace FSO.Client.UI.Screens
{
    public class DebugTypeFaceScreen : GameScreen
    {
        public DebugTypeFaceScreen()
        {
            var msg = "The quick brown fox jumps over the lazy dog";
            var sizes = new int[] { 10, 12, 14, 16, 20 };

            this.Add(new UILabel()
            {
                X = 10.0f,
                Y = 30.0f,
                Caption = "Metric calculation test: Green bar is Y 0, Red bar is calculated baseline"
            });

            var greenTexture = TextureUtils.TextureFromColor(GameFacade.GraphicsDevice, Color.Green);
            var redTexture = TextureUtils.TextureFromColor(GameFacade.GraphicsDevice, Color.Red);
            var grayTexture = TextureUtils.TextureFromColor(GameFacade.GraphicsDevice, Color.Gray);

            var yPosition = 100.0f;
            for (var i = 0; i < sizes.Length; i++)
            {
                var pxSize = sizes[i];

                var label = new UILabel();
                label.Caption = msg;
                label.CaptionStyle = TextStyle.DefaultLabel.Clone();
                label.CaptionStyle.Size = pxSize;

                label.X = 10;
                label.Y = yPosition;

                /** Origin line **/
                var origLine = new UIImage(greenTexture);
                origLine.SetSize(800.0f, 1.0f);
                origLine.X = 10;
                origLine.Y = yPosition;
                this.Add(origLine);


                var baseLine = new UIImage(redTexture);
                baseLine.SetSize(800.0f, 1.0f);
                baseLine.X = 10;
                baseLine.Y = yPosition + label.CaptionStyle.BaselineOffset;
                this.Add(baseLine);

                yPosition += (float)Math.Round(label.CaptionStyle.MeasureString(msg).Y);
                yPosition += 20.0f;
                this.Add(label);
            }

            this.Add(new UILabel()
            {
                X = 10.0f,
                Y = yPosition + 30.0f,
                Caption = "Alignment calculation test"
            });

            var alignments = new TextAlignment[]{
                TextAlignment.Center,
                TextAlignment.Right,
                TextAlignment.Middle,
                TextAlignment.Middle | TextAlignment.Center,
                TextAlignment.Middle | TextAlignment.Right
            };

            yPosition += 60.0f;
            sizes = new int[] { 10 };

            for (var i = 0; i < sizes.Length; i++)
            {
                var pxSize = sizes[i];

                foreach (var align in alignments)
                {
                    var label = new UILabel();
                    label.Caption = msg;
                    label.CaptionStyle = TextStyle.DefaultLabel.Clone();
                    label.CaptionStyle.Size = pxSize;
                    label.Size = new Vector2(800.0f, 50.0f);
                    label.Alignment = align;

                    label.X = 10;
                    label.Y = yPosition;

                    var area = new UIImage(grayTexture);
                    area.SetSize(800.0f, 50.0f);
                    area.X = 10;
                    area.Y = yPosition;
                    this.Add(area);

                    ///** Origin line **/
                    //var origLine = new UIImage(greenTexture);
                    //origLine.SetSize(800.0f, 1.0f);
                    //origLine.X = 10;
                    //origLine.Y = yPosition;
                    //this.Add(origLine);


                    //var baseLine = new UIImage(redTexture);
                    //baseLine.SetSize(800.0f, 1.0f);
                    //baseLine.X = 10;
                    //baseLine.Y = yPosition + label.CaptionStyle.BaselineOffset;
                    //this.Add(baseLine);

                    yPosition += 50.0f;//(float)Math.Round(label.CaptionStyle.MeasureString(msg).Y);
                    yPosition += 20.0f;
                    this.Add(label);
                }
            }




        }
    }
}
