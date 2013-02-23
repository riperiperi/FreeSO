using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSOClient.Code.UI.Framework
{
    public class InputManager
    {
        private UIMouseEventRef LastMouseOver;
        private UIMouseEventRef LastMouseDown;
        private bool LastMouseDownState = false;


        public void HandleMouseEvents(TSOClient.Code.UI.Model.UpdateState state)
        {
            var mouseBtnDown = state.MouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;
            var mouseDif = mouseBtnDown != LastMouseDownState;
            LastMouseDownState = mouseBtnDown;

            if (mouseDif)
            {
                if (mouseBtnDown)
                {
                    if (LastMouseDown != null)
                    {
                        /** We already have mouse down on an object **/
                        return;
                    }
                    if (LastMouseOver != null)
                    {
                        LastMouseDown = LastMouseOver;
                        LastMouseDown.Callback(UIMouseEventType.MouseDown, state.MouseState);
                    }
                }
                else
                {
                    if (LastMouseDown != null)
                    {
                        LastMouseDown.Callback(UIMouseEventType.MouseUp, state.MouseState);
                        LastMouseDown = null;
                    }
                }
            }

            if (state.MouseEvents.Count > 0)
            {
                var topMost =
                    state.MouseEvents.OrderBy(x => x.Element.Depth).Last();


                /** Same element **/
                if (LastMouseOver == topMost)
                {
                    return;
                }

                if (LastMouseOver != null)
                {
                    LastMouseOver.Callback(UIMouseEventType.MouseOut, state.MouseState);
                }

                topMost.Callback(UIMouseEventType.MouseOver, state.MouseState);
                LastMouseOver = topMost;
            }
            else
            {
                if (LastMouseOver != null)
                {
                    LastMouseOver.Callback(UIMouseEventType.MouseOut, state.MouseState);
                    LastMouseOver = null;
                }
            }

        }

    }
}
