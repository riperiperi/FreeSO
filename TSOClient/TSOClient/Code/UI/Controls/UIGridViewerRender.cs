/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Framework;
using TSOClient.LUI;

namespace TSOClient.Code.UI.Controls
{
    public class UIGridViewerRender : UIContainer
    {
        private UIButton button;
        private UIImage image;
        private UIGridViewer owner;
        private object data;

        public UIGridViewerRender(UIGridViewer owner)
        {
            this.owner = owner;

            button = new UIButton(owner.ThumbButtonImage);
            button.Size = owner.ThumbSize;
            button.OnButtonClick += new ButtonClickDelegate(button_OnButtonClick);
            this.Add(button);

            image = new UIImage();
            //image.ScaleX = owner.ThumbImageSize.X / (owner.ThumbSize.X - (owner.ThumbImageOffsets.X * 2));
            image.SetSize(owner.ThumbSize.X - (owner.ThumbImageOffsets.X * 2),
                          owner.ThumbSize.Y - (owner.ThumbImageOffsets.Y * 2));
            image.Position = owner.ThumbImageOffsets;
            this.Add(image);
        }


        void button_OnButtonClick(UIElement button)
        {
            if (data != null)
            {
                owner.SelectedItem = data;
            }
        }

        /// <summary>
        /// Sets the data object for this item render
        /// </summary>
        /// <param name="data"></param>
        public void SetData(object data)
        {
            this.data = data;

            if (data is UIGridViewerItem)
            {
                var castData = ((UIGridViewerItem)data);
                image.Texture = castData.Thumb.Get();
            }
        }


        public void SetSelected(bool selected)
        {
            button.Selected = selected;
        }
    }
}
