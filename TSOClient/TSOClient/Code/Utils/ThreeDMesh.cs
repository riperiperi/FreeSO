using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSOClient.Code.Utils
{
    public class ThreeDMesh<T>
    {
        private List<T> Vertexes = new List<T>();
        private List<int> Indexes = new List<int>();
        private int IndexOffset = 0;
        private int _PrimitiveCount = 0;

        public void AddQuad(T tl, T tr, T br, T bl)
        {
            Vertexes.Add(tl);
            Vertexes.Add(tr);
            Vertexes.Add(br);
            Vertexes.Add(bl);

            Indexes.Add(IndexOffset);
            Indexes.Add(IndexOffset + 1);
            Indexes.Add(IndexOffset + 2);
            Indexes.Add(IndexOffset + 2);
            Indexes.Add(IndexOffset + 3);
            Indexes.Add(IndexOffset);

            IndexOffset += 4;
            _PrimitiveCount += 2;
        }

        public T[] GetVertexes()
        {
            return Vertexes.ToArray();
        }

        public int[] GetIndexes()
        {
            return Indexes.ToArray();
        }

        public int PrimitiveCount
        {
            get
            {
                return _PrimitiveCount;
            }
        }
    }
}
