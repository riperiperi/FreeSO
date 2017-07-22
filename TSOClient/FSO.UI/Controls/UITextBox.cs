/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Client.UI.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using FSO.Client.UI.Model;
using FSO.Client.Utils;
using FSO.Common.Rendering.Framework.IO;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Utils;
using FSO.Client.GameContent;
using FSO.Client.UI.Framework.Parser;
using Microsoft.Xna.Framework.GamerServices;
using FSO.Common;
using System.Threading;

namespace FSO.Client.UI.Controls
{
    public class UITextBox : UITextEdit
    {
        public static ITextureRef StandardBackground;

        static UITextBox()
        {
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
