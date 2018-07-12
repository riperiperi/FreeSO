using System;
using System.Runtime.CompilerServices;

namespace VoronoiLib.Structures
{
    public class MinHeap<T> where T : IComparable<T>
    {
        private readonly T[] items;
        public int Capacity { get; }
        public int Count { get; private set; }

        public MinHeap(int capacity)
        {
            if (capacity < 2)
            {
                capacity = 2;
            }
            Capacity = capacity;
            items = new T[Capacity];
            Count = 0;
        }

        public bool Insert(T obj)
        {
            if (Count == Capacity)
                return false;
            items[Count] = obj;
            Count++;
            PercolateUp(Count - 1);
            return true;
        }

        public T Pop()
        {
            if (Count == 0)
                throw new InvalidOperationException("Min heap is empty");
            if (Count == 1)
            {
                Count--;
                return items[Count];
            }

            var min = items[0];
            items[0] = items[Count - 1];
            Count--;
            PercolateDown(0);
            return min;
        }

        public T Peek()
        {
            if (Count == 0)
                throw new InvalidOperationException("Min heap is empty");
            return items[0];
        }

        //TODO: stop using the remove on the heap as it goes o(N^2)

        public bool Remove(T item)
        {
            int index = -1;
            for (var i = 0; i < Count; i++)
            {
                if (items[i].Equals(item))
                {
                    index = i;
                    break;
                }
            }

            if (index == -1)
                return false;

            Count--;
            Swap(index, Count);
            if (LeftLessThanRight(index, (index - 1)/2))
                PercolateUp(index);
            else
                PercolateDown(index);
            return true;
        }

        private void PercolateDown(int index)
        {
            while (true)
            {
                var left = 2*index + 1;
                var right = 2*index + 2;
                var largest = index;

                if (left < Count && LeftLessThanRight(left, largest))
                    largest = left;
                if (right < Count && LeftLessThanRight(right, largest))
                    largest = right;
                if (largest == index)
                    return;
                Swap(index, largest);
                index = largest;
            }
        }

        private void PercolateUp(int index)
        {
            while (true)
            {
                if (index >= Count || index <= 0)
                    return;
                var parent = (index - 1)/2;

                if (LeftLessThanRight(parent, index))
                    return;

                Swap(index, parent);
                index = parent;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool LeftLessThanRight(int left, int right)
        {
            return items[left].CompareTo(items[right]) < 0;
        }

        private void Swap(int left, int right)
        {
            var temp = items[left];
            items[left] = items[right];
            items[right] = temp;
        }
    }
}
