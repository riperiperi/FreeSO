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

        public UIGridViewerRender(UICollectionViewer owner)
        {
            button = new UIButton(owner.ThumbButtonImage);
            button.Size = owner.ThumbSize;
            this.Add(button);

            image = new UIImage();
            //image.ScaleX = owner.ThumbImageSize.X / (owner.ThumbSize.X - (owner.ThumbImageOffsets.X * 2));
            image.SetSize(owner.ThumbSize.X - (owner.ThumbImageOffsets.X * 2),
                          owner.ThumbSize.Y - (owner.ThumbImageOffsets.Y * 2));
            image.Position = owner.ThumbImageOffsets;
            this.Add(image);
        }


        /// <summary>
        /// Sets the data object for this item render
        /// </summary>
        /// <param name="data"></param>
        public void SetData(object data)
        {
            if (data is UIGridViewerItem)
            {
                var castData = ((UIGridViewerItem)data);
                image.Texture = castData.Thumb;
            }
        }
    }
}
