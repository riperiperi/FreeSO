using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.Utils
{
    public class TimedReferenceController
    {
        private int CurRingNum = 0;
        private List<HashSet<object>> ReferenceRing;
        private Dictionary<object, int> ObjectToRing;
        private int CheckFreq = 0;
        private int TicksToNextCheck;
        private CacheType Type;
        public CacheType CurrentType { get { return Type; } }

        public TimedReferenceController()
        {
            SetMode(CacheType.ACTIVE);
        }

        public void SetMode(CacheType type)
        {
            lock (this) {
                ObjectToRing = new Dictionary<object, int>();
                switch (type)
                {
                    case CacheType.AGGRESSIVE:
                        ReferenceRing = new List<HashSet<object>>();
                        CheckFreq = 1 * 60;
                        for (int i = 0; i < 5; i++) ReferenceRing[i] = new HashSet<object>();
                        break;
                    case CacheType.ACTIVE:
                        ReferenceRing = new List<HashSet<object>>();
                        CheckFreq = 10 * 60;
                        for (int i = 0; i < 10; i++) ReferenceRing[i] = new HashSet<object>();
                        break;
                    case CacheType.PERMANENT:
                    case CacheType.PASSIVE:
                        ReferenceRing = new List<HashSet<object>>();
                        CheckFreq = int.MaxValue;
                        for (int i = 0; i < 1; i++) ReferenceRing[i] = new HashSet<object>();
                        break;
                }
            }
        }

        public void Tick()
        {
            if (Type == CacheType.PERMANENT) return;
            if (TicksToNextCheck-- <= 0)
            {
                lock (this)
                {
                    var toDereference = ReferenceRing[CurRingNum];
                    foreach (var obj in toDereference) ObjectToRing.Remove(obj);
                    toDereference.Clear();
                    CurRingNum = (CurRingNum + 1) % ReferenceRing.Count;
                }
                TicksToNextCheck = CheckFreq;
            }
        }

        public void KeepAlive(object o, KeepAliveType type)
        {
            //should be called whenever the object is referenced
            lock (this)
            {
                var offset = ReferenceRing.Count - 1;
                var becomes = (CurRingNum + offset) % ReferenceRing.Count;
                int oldring;
                if (ObjectToRing.TryGetValue(o, out oldring))
                {
                    if (becomes != oldring)
                    {
                        ReferenceRing[oldring].Remove(o);
                        ObjectToRing.Remove(oldring);
                    }
                }
                else
                {
                    ReferenceRing[becomes].Add(o);
                    ObjectToRing.Add(o, becomes);
                }
            }
        }
    }

    public enum KeepAliveType
    {
        ACCESS,
        DEREFERENCED
    }

    public enum CacheType
    {
        AGGRESSIVE,
        ACTIVE,
        PASSIVE,
        PERMANENT
    }


    public class TimedReferenceCache<KEY, VALUE>
    {
        private ConcurrentDictionary<KEY, WeakReference> Cache = new ConcurrentDictionary<KEY, WeakReference>();

        private VALUE GetOrAddInternal(KEY key, Func<KEY, VALUE> valueFactory)
        {
            bool didCreate = false;
            VALUE created = default(VALUE);
            var value = Cache.GetOrAdd(key, (k) =>
            {
                created = valueFactory(k);
                didCreate = true;
                return new WeakReference(created, true);
            });

            var refVal = value.Target;
            if (didCreate)
                return created;
            else if (refVal != null || value.IsAlive)
                return (VALUE)refVal;
            else
            {
                //refrerence died. must recache.
                WeakReference resultRemove;
                var removed = Cache.TryRemove(key, out resultRemove);
                return GetOrAddInternal(key, valueFactory);
            }
        }

        public VALUE GetOrAdd(KEY key, Func<KEY, VALUE> valueFactory)
        {
            var result = GetOrAddInternal(key, valueFactory);
            if (result != null)
            {
                GameThread.Caching.KeepAlive(result, KeepAliveType.ACCESS);
            }
            return result;
        }

        public bool TryRemove(KEY key, out VALUE value)
        {
            WeakReference resultRef;
            if (Cache.TryRemove(key, out resultRef))
            {
                value = (VALUE)resultRef.Target;
                return true;
            } else
            {
                value = default(VALUE);
                return false;
            }
        }
    }
}
