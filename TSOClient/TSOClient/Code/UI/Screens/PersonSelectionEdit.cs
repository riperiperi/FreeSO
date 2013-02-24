using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Framework;
using Microsoft.Xna.Framework.Graphics;
using TSOClient.Code.UI.Controls;

namespace TSOClient.Code.UI.Screens
{
    public class PersonSelectionEdit : GameScreen
    {
        public Texture2D BackgroundImage { get; set; }


        public PersonSelectionEdit()
        {
            this.RenderScript("personselectionedit.uis");
            this.AddAt(0, new UIImage(BackgroundImage));
        }
    }
}
