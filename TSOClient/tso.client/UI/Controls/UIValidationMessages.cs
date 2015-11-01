using FSO.Client.UI.Framework;
using FSO.Client.Utils;
using FSO.Common.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Controls
{
    public class UIValidationMessages<T> : UIElement
    {
        private List<UIValidationMessage<T>> Messages = new List<UIValidationMessage<T>>();
        public string ErrorPrefix;
        public int Width = 300;
        public int Padding = 10;
        public TextStyle Style = TextStyle.DefaultButton;

        private Color _Background = new Color(49, 73, 99);
        public Color Background
        {
            get { return _Background; }
            set{
                _Background = value;
                BackgroundTexture = null;
            }
        }

        private Texture2D BackgroundTexture;

        private UIWordWrapOutput Text;

        public UIValidationMessages()
        {
            Opacity = 0.8f;
        }

        public UIValidationMessages<T> WithValidation(string message, Func<T, bool> validator)
        {
            Messages.Add(new UIValidationMessage<T>(validator, message));
            return this;
        }

        public bool Validate(T value)
        {
            var messages = new List<string>();

            foreach(var msg in Messages){
                var isInvalid = msg.Validator(value);
                if (isInvalid){
                    messages.Add(msg.Message);
                }
            }

            if(messages.Count > 0){
                var text = ErrorPrefix + "\n";
                foreach(var msg in messages){
                    text += msg + "\n";
                }

                Text = UIUtils.WordWrap(text, Width - (Padding*2), Style, new Microsoft.Xna.Framework.Vector2(Style.Scale));
                return false;
            }
            else
            {
                Text = null;
                return true;
            }
        }

        public override void Draw(UISpriteBatch batch)
        {
            if (Text == null) { return; }

            if(BackgroundTexture == null){
                BackgroundTexture = TextureUtils.TextureFromColor(batch.GraphicsDevice, _Background);
            }

            DrawLocalTexture(batch, BackgroundTexture, new Rectangle(0, 0, 1, 1), Vector2.Zero, new Vector2(Width, Text.Height + Padding));

            var y = Padding;
            var rect = new Rectangle(0, 0, Width, Style.LineHeight);

            for (int i=0; i < Text.Lines.Count; i++){
                var text = Text.Lines[i];
                DrawLocalString(batch, text, new Vector2(0, y), Style, rect, TextAlignment.Center);
                y += Style.LineHeight;
            }
        }
    }

    public class UIValidationMessage<T>
    {
        public Func<T, bool> Validator { get; internal set; }
        public string Message { get; internal set; }

        public UIValidationMessage(Func<T, bool> validator, string message)
        {
            Validator = validator;
            Message = message;
        }
    }
}
