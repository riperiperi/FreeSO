using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WinFormsGraphicsDevice;
using Microsoft.Xna.Framework;
using System.Timers;
using System.Threading;

namespace TSO.Common.rendering.framework.winforms
{
    public class WinFormsGameWindow : GraphicsDeviceControl
    {
        protected GameTime GameTime;
        public GameScreen Screen;

        private System.Timers.Timer Timer;
        private long TimerStartTime;

        public WinFormsGameWindow()
        {
        }

        protected override void Initialize(){
            GameTime = new GameTime();
            Screen = new GameScreen(GraphicsDevice);
            //canvas.Refresh();
            if (Timer == null)
            {
                TimerStartTime = DateTime.Now.Ticks;

                Timer = new System.Timers.Timer(100);
                Timer.Elapsed += new ElapsedEventHandler(Timer_Elapsed);
                Timer.Enabled = true;
            }
        }

        void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.Invoke(new ThreadStart(this.Refresh), null);
        }

        protected override void Draw(){
            var now = TimeSpan.FromTicks(DateTime.Now.Ticks - TimerStartTime);

            GameTime = new GameTime(now, now, now, now);
            Screen.Update(GameTime);
            Screen.Draw(GameTime);
        }

    }
}
