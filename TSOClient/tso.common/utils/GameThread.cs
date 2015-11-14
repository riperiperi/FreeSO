using FSO.Common.Rendering.Framework.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.Utils
{
    public class GameThreadInterval
    {
        private Callback Callback;
        private long Interval;
        private double EndTime = -1;

        private UpdateHook _TickHook;
        private bool _Clear;

        public GameThreadInterval(Callback callback, long interval)
        {
            Callback = callback;
            Interval = interval;
            _TickHook = GameThread.EveryUpdate(Tick);
        }

        public void Clear()
        {
            _Clear = true;
        }

        private void Tick(UpdateState state)
        {
            if (_Clear)
            {
                _TickHook.Remove();
                return;
            }

            var now = state.Time.TotalGameTime.TotalMilliseconds;
            if (EndTime == -1)
            {
                EndTime = now + Interval;
            }

            if (EndTime <= now)
            {
                Callback();
                EndTime = now + Interval;
            }
        }
    }

    public class GameThreadTimeout
    {
        private Callback Callback;
        private long Delay;
        private double EndTime = -1;

        private UpdateHook _TickHook;
        private bool _Clear;

        public GameThreadTimeout(Callback callback, long delay)
        {
            Callback = callback;
            Delay = delay;
            _TickHook = GameThread.EveryUpdate(Tick);
        }

        public void Clear(){
            _Clear = true;
        }

        private void Tick(UpdateState state)
        {
            if (_Clear){
                _TickHook.Remove();
                return;
            }

            var now = state.Time.TotalGameTime.TotalMilliseconds;
            if (EndTime == -1){
                EndTime = now + Delay;
            }

            if(EndTime <= now){
                _TickHook.Remove();
                Callback();
            }
        }
    }

    public class UpdateHook
    {
        public bool RemoveNext = false;
        public Callback<UpdateState> Callback;

        public void Remove(){
            RemoveNext = true;
        }
    }

    public class GameThread
    {
        private static List<UpdateHook> _UpdateHooks = new List<UpdateHook>();
        private static List<Callback<UpdateState>> _UpdateCallbacks = new List<Callback<UpdateState>>();

        public static GameThreadTimeout SetTimeout(Callback callback, long delay)
        {
            var result = new GameThreadTimeout(callback, delay);
            return result;
        }

        public static GameThreadInterval SetInterval(Callback callback, long delay)
        {
            var result = new GameThreadInterval(callback, delay);
            return result;
        }

        //I know we already have a way to do this with IUIProcess but we need a way for other libs that dont
        //have the UI code for reference
        public static UpdateHook EveryUpdate(Callback<UpdateState> callback)
        {
            var newHook = new UpdateHook()
            {
                Callback = callback
            };

            lock (_UpdateHooks)
            {
                _UpdateHooks.Add(newHook);
            }

            return newHook;
        }

        public static void NextUpdate(Callback<UpdateState> callback)
        {
            lock (_UpdateCallbacks)
            {
                _UpdateCallbacks.Add(callback);
            }
        }

        public static Task<T> NextUpdate<T>(Func<UpdateState, T> callback)
        {
            TaskCompletionSource<T> task = new TaskCompletionSource<T>();
            lock (_UpdateCallbacks)
            {
                _UpdateCallbacks.Add(x =>
                {
                    task.SetResult(callback(x));
                });
            }
            return task.Task;
        }

        public static void DigestUpdate(UpdateState state)
        {
            lock (_UpdateCallbacks)
            {
                foreach (var callback in _UpdateCallbacks)
                {
                    callback(state);
                }
                _UpdateCallbacks.Clear();
            }
            lock (_UpdateHooks)
            {
                for(int i=0; i < _UpdateHooks.Count; i++)
                {
                    var item = _UpdateHooks[i];
                    item.Callback(state);
                    if (item.RemoveNext){
                        _UpdateHooks.RemoveAt(i);
                        i--;
                    }
                }
            }
        }
    }
}
