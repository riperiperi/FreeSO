using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using FSO.Common.Rendering.Framework.IO;

namespace FSO.Common.Rendering.Framework.Model
{
    public class MultiMouse
    {
        public int ID;
        public MouseState MouseState;
        public UIMouseEventRef LastMouseDown;
        public UIMouseEventRef LastMouseOver;
        public bool LastMouseDownState = false;
        public bool NewMultiMouse = true;
        public bool Dead = false;
    }

    /// <summary>
    /// Contains common information used in the update loop
    /// </summary>
    public class UpdateState
    {
        public GameTime Time;
        public List<MultiMouse> MouseStates = new List<MultiMouse>();
        public MouseState MouseState;
        public int CurrentMouseID;
        public KeyboardState KeyboardState;
        public bool ShiftDown
        {
            get { return KeyboardState.IsKeyDown(Keys.LeftShift) || KeyboardState.IsKeyDown(Keys.RightShift); }
        }
        /// <summary>
        /// Right alt is treated as LeftCtrl+RightAlt so while right Alt is down, you cannot predict if left Ctrl is also down.
        /// For that reason, this variable is false when left Ctrl and right Alt are down, and right Ctrl is not down.
        /// </summary>
        public bool CtrlDown
        {
            get { return (KeyboardState.IsKeyDown(Keys.LeftControl) && !KeyboardState.IsKeyDown(Keys.RightAlt)) || KeyboardState.IsKeyDown(Keys.RightControl); }
        }
        public bool AltDown
        {
            get { return KeyboardState.IsKeyDown(Keys.LeftAlt) || KeyboardState.IsKeyDown(Keys.RightAlt); }
        }

        public UIState UIState = new UIState();
        public InputManager InputManager;
        public bool TouchMode;

        public KeyboardState PreviousKeyboardState;
        public List<char> FrameTextInput;

        /** A Place to keep shared variables, clears every update cycle **/
        public Dictionary<string, object> SharedData = new Dictionary<string, object>();
        public List<Tuple<int, UIMouseEventRef>> MouseEvents = new List<Tuple<int, UIMouseEventRef>>();

        private Dictionary<Keys, long> KeyDownTime = new Dictionary<Keys, long>();
        private List<Keys> KeyInRepeatMode = new List<Keys>();

        public List<Keys> NewKeys = new List<Keys>();
        public int Depth;

        public bool WindowFocused;
        public bool MouseOverWindow;

        public bool ProcessMouseEvents
        {
            get
            {
                return WindowFocused && MouseOverWindow;
            }
        }

        public void Update()
        {
            NewKeys.Clear();
            Depth = 0;

            /**
             * If a key has been held down for X duration, treat it as if it is newly
             * pressed
             */
            for(var i=0; i < KeyInRepeatMode.Count; i++){

                if (!KeyboardState.IsKeyDown(KeyInRepeatMode[i]))
                {
                    KeyInRepeatMode.RemoveAt(i);
                    i--;
                }
            }

            var now = Time.TotalGameTime.Ticks;
            var keys = KeyboardState.GetPressedKeys();

            foreach (var key in keys)
            {
                var newPress = PreviousKeyboardState.IsKeyUp(key);
                if (newPress)
                {
                    KeyDownTime[key] = now;
                    NewKeys.Add(key);
                }
                else
                {
                    if (KeyInRepeatMode.Contains(key))
                    {

                        /** How long has it been down? **/
                        if (now - KeyDownTime[key] > 400000)
                        {
                            /** Its been down long enough, consider it a new key **/
                            KeyDownTime[key] = now;
                            NewKeys.Add(key);
                        }
                    }
                    else
                    {
                        /** How long has it been down? **/
                        if (now - KeyDownTime[key] > 9000000)
                        {
                            /** Its been down long enough, consider it in repeat mode **/
                            KeyDownTime[key] = now;
                            KeyInRepeatMode.Add(key);
                        }
                    }
                }
            }
        }
    }
}
