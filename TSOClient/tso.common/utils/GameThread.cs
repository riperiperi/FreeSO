using FSO.Common.Rendering.Framework.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.Utils
{
    public class GameThread
    {
        private static List<Callback<UpdateState>> _UpdateCallbacks = new List<Callback<UpdateState>>();

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
            
        }
    }
}
