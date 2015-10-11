using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using XKeys = Microsoft.Xna.Framework.Input.Keys;

namespace MGWinForms
{
    public abstract class GameControl : GraphicsDeviceControl
    {
        private GameTime _gameTime;
        private Stopwatch _timer;
        private TimeSpan _elapsed;

        private List<XKeys> _keys;

        private bool _active;

        public GameControl ()
        {
            SuspendOnFormInactive = true;
        }

        public bool SuspendOnFormInactive { get; set; }

        protected override void Initialize ()
        {
            _keys = new List<XKeys>();
            _timer = Stopwatch.StartNew();

            Application.Idle += delegate { GameLoop(); };

            if (ParentForm != null) {
                ParentForm.Activated += HandleFormActivated;
                ParentForm.Deactivate += HandleFormDeactivated;
            }
        }

        protected override List<XKeys> KeyState
        {
            get { return _keys ?? base.KeyState; }
        }

        protected override void Draw ()
        {
            Draw(_gameTime);
        }

        private void GameLoop ()
        {
            if (SuspendOnFormInactive && !_active) {
                return;
            }

            _keys.Clear();
            _keys.AddRange(base.KeyState);

            _gameTime = new GameTime(_timer.Elapsed, _timer.Elapsed - _elapsed);
            _elapsed = _timer.Elapsed;

            Update(_gameTime);
            Invalidate();
        }

        protected abstract void Update (GameTime gameTime);
        protected abstract void Draw (GameTime gameTime);

        private void HandleFormActivated (object sender, EventArgs e)
        {
            _active = true;
            _elapsed = _timer.Elapsed;
        }

        private void HandleFormDeactivated (object sender, EventArgs e)
        {
            _active = false;
        }
    }
}
