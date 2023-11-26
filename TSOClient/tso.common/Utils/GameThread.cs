using FSO.Common.Rendering.Framework.Model;
using System;
using System.Collections.Generic;
using System.Threading;
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
        public static bool Killed;
        public static bool NoGame;
        public static EventWaitHandle OnKilled = new EventWaitHandle(false, EventResetMode.ManualReset);
        public static event Action KilledEvent;
        public static Thread Game;
        public static bool UpdateExecuting;
        private static List<UpdateHook> _UpdateHooks = new List<UpdateHook>();
        private static UpdateHook[] _UpdateHooksCopy = new UpdateHook[0];
        private static List<UpdateHook> _UpdateHooksRemove = new List<UpdateHook>();
        private static Queue<Callback<UpdateState>> _UpdateCallbacks = new Queue<Callback<UpdateState>>();
        private static Queue<Callback<UpdateState>> _UpdateCallbacksSwap = new Queue<Callback<UpdateState>>();
        public static AutoResetEvent OnWork = new AutoResetEvent(false);

        public static void SetKilled()
        {
            Killed = true;
            OnKilled.Set();
            KilledEvent?.Invoke();
        }

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
                _UpdateCallbacks.Enqueue(callback);
            }
            OnWork.Set();
        }

        public static void InUpdate(Callback callback)
        {
            if (IsInGameThread() && UpdateExecuting){
                callback();
            }else{
                NextUpdate(x => callback());
            }
        }

        public static bool IsInGameThread()
        {
            var thread = Thread.CurrentThread;
            if (thread == Game || NoGame)
            {
                return true;
            }
            return false;
        }

        public static Task<T> NextUpdate<T>(Func<UpdateState, T> callback)
        {
            TaskCompletionSource<T> task = new TaskCompletionSource<T>();
            lock (_UpdateCallbacks)
            {
                _UpdateCallbacks.Enqueue(x =>
                {
                    task.SetResult(callback(x));
                });
            }
            return TimeoutAfter(task.Task, new TimeSpan(0, 0, 5));
        }

        public static async Task<TResult> TimeoutAfter<TResult>(Task<TResult> task, TimeSpan timeout)
        {
            using (var timeoutCancellationTokenSource = new CancellationTokenSource())
            {

                var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
                if (completedTask == task)
                {
                    timeoutCancellationTokenSource.Cancel();
                    return await task;  // Very important in order to propagate exceptions
                }
                else
                {
                    return default(TResult);
                }
            }
        }

        public static void DigestUpdate(UpdateState state)
        {
            Queue<Callback<UpdateState>> _callbacks;

            lock (_UpdateCallbacks)
            {
                // Swap the active callbacks queue with the second one, so we can
                // process entries without fear of more being added.

                _callbacks = _UpdateCallbacks;
                _UpdateCallbacks = _UpdateCallbacksSwap;
                _UpdateCallbacksSwap = _callbacks;
            }

            while (_callbacks.Count > 0)
            {
                _callbacks.Dequeue()(state);
            }

            int hookCount;
            UpdateHook[] _hooks;
            List<UpdateHook> toRemove = _UpdateHooksRemove;

            lock (_UpdateHooks)
            {
                hookCount = _UpdateHooks.Count;

                if (_UpdateHooksCopy.Length < _UpdateHooks.Count)
                {
                    _UpdateHooksCopy = _UpdateHooks.ToArray();
                }
                else
                {
                    _UpdateHooks.CopyTo(_UpdateHooksCopy);
                }

                _hooks = _UpdateHooksCopy;
            }

            for (int i = 0; i < hookCount; i++)
            {
                var item = _hooks[i];
                item.Callback(state);
                if (item.RemoveNext)
                {
                    toRemove.Add(item);
                }
            }

            if (toRemove.Count > 0)
            {
                lock (_UpdateHooks)
                {
                    foreach (var rem in toRemove) _UpdateHooks.Remove(rem);
                }

                toRemove.Clear();
            }

            //finally, check cache controller
            TimedReferenceController.Tick();
        }
    }
}
