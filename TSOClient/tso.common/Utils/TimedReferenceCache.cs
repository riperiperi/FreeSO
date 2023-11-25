using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace FSO.Common.Utils
{
    public static class TimedReferenceController
    {
        private static int CurRingNum = 0;
        private static List<HashSet<object>> ReferenceRing;
        private static Dictionary<object, int> ObjectToRing;
        private static int CheckFreq = 0;
        private static int TicksToNextCheck;
        private static CacheType Type;
        private static object InternalLock = new object { };
        public static CacheType CurrentType { get { return Type; } }

        static TimedReferenceController()
        {
            SetMode(CacheType.ACTIVE);
        }

        public static void Clear()
        {
            lock (InternalLock)
            {
                ObjectToRing = new Dictionary<object, int>();
                foreach (var item in ReferenceRing)
                {
                    foreach (var obj in item)
                    {
                        (obj as ITimedCachable)?.Rereferenced(false);
                    }
                    item.Clear();
                }
                GC.Collect(2);
            }
        }

        public static void SetMode(CacheType type)
        {
            lock (InternalLock) {
                ObjectToRing = new Dictionary<object, int>();
                switch (type)
                {
                    case CacheType.AGGRESSIVE:
                        ReferenceRing = new List<HashSet<object>>();
                        CheckFreq = 1 * 60;
                        for (int i = 0; i < 5; i++) ReferenceRing.Add(new HashSet<object>());
                        break;
                    case CacheType.ACTIVE:
                        ReferenceRing = new List<HashSet<object>>();
                        CheckFreq = 5 * 60;
                        for (int i = 0; i < 3; i++) ReferenceRing.Add(new HashSet<object>());
                        break;
                    case CacheType.PERMANENT:
                    case CacheType.PASSIVE:
                        ReferenceRing = new List<HashSet<object>>();
                        CheckFreq = int.MaxValue;
                        ReferenceRing.Add(new HashSet<object>());
                        break;
                }
                Type = type;
            }
        }

        public static void Tick()
        {
            if (Type == CacheType.PERMANENT) return;
            if (TicksToNextCheck-- <= 0)
            {
                lock (InternalLock)
                {
                    var toDereference = ReferenceRing[CurRingNum];
                    foreach (var obj in toDereference) ObjectToRing.Remove(obj);
                    toDereference.Clear();
                    CurRingNum = (CurRingNum + 1) % ReferenceRing.Count;
                }
                TicksToNextCheck = CheckFreq;
                //GC.Collect();
                if (CurRingNum == 0) GC.Collect();
            }
        }

        public static void KeepAlive(object o, KeepAliveType type)
        {
            if (type == KeepAliveType.ACCESS && (o is ITimedCachable)) ((ITimedCachable)o).Rereferenced(true);
            //should be called whenever the object is referenced
            lock (InternalLock)
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
        private bool PermaMode = false;
        private List<VALUE> PermaRef = new List<VALUE>();
        private VALUE GetOrAddInternal(KEY key, Func<KEY, VALUE> valueFactory)
        {
            bool didCreate = false;
            VALUE created = default(VALUE);
            var value = Cache.GetOrAdd(key, (k) =>
            {
                //ConcurrentDictionary does not ensure we don't accidentally create things twice. This lock will help us, but I don't think it ensures a perfect world.
                lock (this) {
                    WeakReference prev;
                    if (Cache.TryGetValue(key, out prev) && prev.IsAlive) return prev; //already created this value.
                    created = valueFactory(k);
                    didCreate = true;
                    return new WeakReference(created, true);
                }
            });

            var refVal = value.Target;
            if (didCreate && refVal == (object)created)
            {
                //i made this. if we're perma cache ensure a permanent reference.
                if (TimedReferenceController.CurrentType == CacheType.PERMANENT)
                {
                    PermaRef.Add(created);
                    PermaMode = true;
                }
                return created;
            }
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
            if (result != null && !PermaMode)
            {
                TimedReferenceController.KeepAlive(result, KeepAliveType.ACCESS);
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
