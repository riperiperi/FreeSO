using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using FSO.Common.Rendering.Framework.IO;
using FSO.Common.Rendering.Framework.Model;

namespace FSO.Client.UI.Controls
{
    public class UIClickableLabel : UILabel, IFocusableUI
    {
        private UIMouseEventRef ClickHandler;
        public event ButtonClickDelegate OnButtonClick;
        public event UIMouseEvent OnMouseEvtExt;

        public bool IsFocused { get; set; }
        public int TabIndex { get; set; }

        public UIClickableLabel()
        {
            ClickHandler =
                ListenForMouse(new Rectangle(0, 0, 10, 10), new UIMouseEvent(OnMouseEvent));
        }


        public override Vector2 Size
        {
            get
            {
                return base.Size;
            }
            set
            {
                base.Size = value;
                ClickHandler.Region = new Rectangle(0, 0, (int)value.X, (int)value.Y);
            }
        }



        //private bool m_isOver;
        //todo - use m_isOver to show diff colour for hovering over labels. 
        private bool m_isDown;

        private void OnMouseEvent(UIMouseEventType type, UpdateState state)
        {
            switch (type)
            {
                case UIMouseEventType.MouseOver:
                    //m_isOver = true;
                    break;

                case UIMouseEventType.MouseOut:
                    //m_isOver = false;
                    break;

                case UIMouseEventType.MouseDown:
                    m_isDown = true;
                    state.InputManager.SetFocus(this);
                    break;

                case UIMouseEventType.MouseUp:
                    if (m_isDown)
                    {
                        if (OnButtonClick != null)
                        {
                            OnButtonClick(this);
                            //GameFacade.SoundManager.PlayUISound(1);
                        }
                    }
                    m_isDown = false;
                    break;
            }
            OnMouseEvtExt?.Invoke(type, state);
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
            if (IsFocused && state.ActivationKeyPressed)
                OnButtonClick?.Invoke(this);
        }
    }
}
