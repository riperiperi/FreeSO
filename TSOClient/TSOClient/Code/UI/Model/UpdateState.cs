using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TSOClient.Code.UI.Framework;

namespace TSOClient.Code.UI.Model
{
    /// <summary>
    /// Contains common information used in the update loop
    /// </summary>
    public class UpdateState
    {
        public GameTime Time;
        public MouseState MouseState;
        public KeyboardState KeyboardState;

        public List<UIMouseEventRef> MouseEvents = new List<UIMouseEventRef>();
    }
}
