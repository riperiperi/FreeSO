using System.Collections;
using System.Runtime.InteropServices;

namespace FSO.SimAntics.Model
{
    public struct VMObjectListEntry<T> where T : VMEntity
    {
        public readonly short ObjectID;
        public readonly T Object;

        public VMObjectListEntry(T obj)
        {
            ObjectID = obj.ObjectID;
            Object = obj;
        }
    }

    public class VMObjectList<T> : IList<T> where T : VMEntity
    {
        private readonly List<VMObjectListEntry<T>> List = [];

        public int Count => List.Count;

        public bool IsReadOnly => false;

        public T this[int index] { get => List[index].Object; set => List[index] = new VMObjectListEntry<T>(value); }

        public void AddToObjList(T entity)
        {
            var raw = CollectionsMarshal.AsSpan(List);
            if (raw.Length == 0) { Add(entity); return; }

            int id = entity.ObjectID;
            int max = raw.Length;
            int min = 0;
            while (max > min)
            {
                int mid = (max + min) / 2;
                int nid = raw[mid].ObjectID;
                if (id < nid) max = mid;
                else if (id == nid) return; //do not add dupes
                else min = mid + 1;
            }
            Insert(min, entity);
        }

        public int FindInObjList(T entity)
        {
            var raw = CollectionsMarshal.AsSpan(List);
            if (raw.Length == 0) { return -1; }

            int id = entity.ObjectID;
            int max = raw.Length;
            int min = 0;
            while (max > min)
            {
                int mid = (max + min) / 2;
                int nid = raw[mid].ObjectID;
                if (id < nid) max = mid;
                else if (id == nid)
                {
                    return mid;
                }
                else min = mid + 1;
            }

            return -1;
        }

        public bool DeleteFromObjList(T entity)
        {
            var raw = CollectionsMarshal.AsSpan(List);
            if (raw.Length == 0) { return false; }

            int id = entity.ObjectID;
            int max = raw.Length;
            int min = 0;
            while (max > min)
            {
                int mid = (max + min) / 2;
                int nid = raw[mid].ObjectID;
                if (id < nid) max = mid;
                else if (id == nid)
                {
                    RemoveAt(mid); //found it
                    return true;
                }
                else min = mid + 1;
            }
            return false;
        }

        public int FindNextIndexInObjList(short targId)
        {
            var raw = CollectionsMarshal.AsSpan(List);
            int count = raw.Length;
            if (count == 0) return 0;

            int max = count;
            int min = 0;
            while (max > min)
            {
                int mid = (max + min) / 2;
                int nid = raw[mid].ObjectID;
                if (targId < nid) max = mid; //target object is below us
                else if (targId == nid)
                {
                    //found it. find NEXT!
                    return mid + 1;
                }
                else min = mid + 1; //target object is above us
            }
            if (min >= count) return count;
            return raw[min].ObjectID > targId ? min : min + 1;
        }

        public int IndexOf(T item)
        {
            return FindInObjList(item);
        }

        public void Insert(int index, T item)
        {
            List.Insert(index, new(item));
        }

        public void RemoveAt(int index)
        {
            List.RemoveAt(index);
        }

        public void Add(T item)
        {
            List.Add(new(item));
        }

        public void Clear()
        {
            List.Clear();
        }

        public bool Contains(T item)
        {
            return FindInObjList(item) != -1;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            ArgumentNullException.ThrowIfNull(array);
            ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);

            if (arrayIndex + Count > array.Length)
            {
                throw new ArgumentException(null, nameof(array));
            }

            foreach (var entry in List)
            {
                array[arrayIndex++] = entry.Object;
            }
        }

        public bool Remove(T item)
        {
            return DeleteFromObjList(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return List.Select(x => x.Object).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
