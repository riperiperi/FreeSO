using FSO.Client.UI.Framework;
using Microsoft.Xna.Framework;
using FSO.Client.UI.Model;
using FSO.Client.GameContent;

namespace FSO.Client.UI.Controls
{
    public class UITextBox : UITextEdit
    {
        public static ITextureRef StandardBackground;
        public bool HasText => !string.IsNullOrWhiteSpace(CurrentText);
        static UITextBox()
        {
            var tex = UIElement.GetTexture((ulong)FileIDs.UIFileIDs.dialog_textboxbackground);
            if (tex.Width == 1) return;
            StandardBackground = new SlicedTextureRef(
                UIElement.GetTexture((ulong)FileIDs.UIFileIDs.dialog_textboxbackground),
                new Microsoft.Xna.Framework.Rectangle(13, 13, 13, 13)
            );
        }

        public UITextBox() : base()
        {
            MaxLines = 1;
            BackgroundTextureReference = UITextBox.StandardBackground;
            TextMargin = new Rectangle(8, 2, 8, 3);
        }

        public void Clear()
        {
            SelectionEnd = -1;
            SelectionStart = -1;
            m_SBuilder.Clear();
        }
       
    }
}
