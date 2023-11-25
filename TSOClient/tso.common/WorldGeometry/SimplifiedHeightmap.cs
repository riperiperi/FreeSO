using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FSO.Common.WorldGeometry
{
    public class SimplifiedHeightmap
    {
        public float HeightMultiplier = 1 / 40f;
        public int Size;
        public int Size1;
        public ushort[] Map; //10x resolution of ts1 terrain.
        public ushort[][] SecondDerivativePyramid;
        public SimplifiedHeightmap(int size, ushort[] data)
        {
            Size = size;
            Size1 = size - 1;
            Map = data;
            BuildSecondDerivative();
        }

        public void BuildSecondDerivative()
        {
            //first, build the full res second derivative map
            var sd = new ushort[Map.Length];
            int i = 0;

            //x derivative
            for (int y = 0; y < Size; y++)
            {
                ushort lastValue = Map[i++];
                ushort firstDerivative = 0;
                for (int x = 1; x < Size; x++)
                {
                    ushort value = Map[i];
                    ushort newFirstDerivative = (ushort)Math.Abs(value - lastValue);
                    sd[(i++) - 1] = (ushort)Math.Abs(newFirstDerivative - firstDerivative);
                    firstDerivative = newFirstDerivative;
                    lastValue = value;
                }
            }

            //y derivative
            i = 0;
            for (int y = 0; y < Size; y++)
            {
                i = y;
                ushort lastValue = Map[i];
                i += Size;
                ushort firstDerivative = 0;
                for (int x = 1; x < Size; x++)
                {
                    ushort value = Map[i];
                    ushort newFirstDerivative = (ushort)Math.Abs(value - lastValue);
                    sd[i-Size] = Math.Max(sd[i-Size], (ushort)Math.Abs(newFirstDerivative - firstDerivative));
                    i += Size;
                    firstDerivative = newFirstDerivative;
                    lastValue = value;
                }
            }

            //build mipLevels
            var levels = 7; //gen 2x2 through 64x64
            SecondDerivativePyramid = new ushort[levels][];
            SecondDerivativePyramid[0] = sd;

            var curLevel = sd;
            for (int mip = 1; mip < levels; mip++)
            {
                var size = (int)Math.Sqrt(curLevel.Length);
                var mipLevel = new ushort[curLevel.Length / 4];
                i = 0;
                for (int y = 0; y < size; y++)
                {
                    var target = (y / 2) * size / 2;
                    for (int x = 0; x < size; x += 2)
                    {
                        mipLevel[target] = Math.Max(mipLevel[target], Math.Max(curLevel[i++], curLevel[i++]));
                        target++;
                    }
                }
                SecondDerivativePyramid[mip] = mipLevel;
                curLevel = mipLevel;
            }
        }

        //second derivitive required to subdivide further.
        public short[] HighDetailThresholds =
        {
            20, //1x1
            16, //2x2: at least one derivative about half as tall as a block
            10, //4x4
            7, //8x8
            4, //16x16
            2, //32x32
            1, //64x64
        };

        List<HeightmapChunk> Chunks;

        private Dictionary<Point, int> PointToIndex;
        public List<int> Indices;
        public List<Vector3> Vertices;

        //how the simplification works:
        //we generate a "maximum second derivative" map from the heightmap.
        //we then create a maximum value image pyramid with the maximum value in the 4 pixels beneath
        //when generating the mesh, we fall into quadrants below if the second derivative is above a threshold.

        public void GenerateFullTree()
        {
            //how the algorithm works:
            //build a base structure with quad trees
            Chunks = new List<HeightmapChunk>();

            var levels = SecondDerivativePyramid.Length;
            var chunkSize = 1 << levels;

            var cw = Size / chunkSize;

            for (int y = 0; y < Size; y += chunkSize)
            {
                for (int x = 0; x < Size; x += chunkSize)
                {
                    var chunk = new HeightmapChunk(new Rectangle(x, y, chunkSize, chunkSize), levels,
                        (x == 0) ? null : Chunks.Last(),
                        (y == 0) ? null : Chunks[Chunks.Count - cw]);
                    Chunks.Add(chunk);
                }
            }

            var thresholds = HighDetailThresholds;
            var toTraverse = new Queue<HeightmapQuadTreeNode>(Chunks);
            while (toTraverse.Count > 0)
            {
                var node = toTraverse.Dequeue();
                var mipLevel = node.MipLevel;
                var sd = SecondDerivativePyramid[mipLevel-1];
                var mipWidth = Size >> (mipLevel-1);
                var pos = (node.Range.X >> (mipLevel - 1)) + (node.Range.Y >> (mipLevel - 1)) * mipWidth;

                //check the max second derivative of the 4 potential derivatives.
                var threshold = HighDetailThresholds[mipLevel - 1];
                if (sd[pos] >= threshold) //top left
                {
                    var newNode = node.GetOrAdd(0, true);
                    if (mipLevel > 1) toTraverse.Enqueue(newNode);
                }
                if (sd[pos+1] >= threshold) //top right
                {
                    var newNode = node.GetOrAdd(1, true);
                    if (mipLevel > 1) toTraverse.Enqueue(newNode);
                }
                if (sd[pos + mipWidth] >= threshold) //bottom left
                {
                    var newNode = node.GetOrAdd(2, true);
                    if (mipLevel > 1) toTraverse.Enqueue(newNode);
                }
                if (sd[pos + mipWidth + 1] >= threshold) //top right
                {
                    var newNode = node.GetOrAdd(3, true);
                    if (mipLevel > 1) toTraverse.Enqueue(newNode);
                }
            }
        }

        public void GenerateMesh()
        {
            //traverse the chunk tree, generating meshes for each.
            PointToIndex = new Dictionary<Point, int>();
            Vertices = new List<Vector3>();
            Indices = new List<int>();

            foreach (var chunk in Chunks)
            {
                chunk.Triangulate(this);
            }
        }

        public int GetVertex(Point pt)
        {
            int index;
            if (!PointToIndex.TryGetValue(pt, out index))
            {
                index = Vertices.Count;
                var x = Math.Min(Size1, pt.X);
                var y = Math.Min(Size1, pt.Y);
                Vertices.Add(new Vector3(pt.X, Map[x + y * Size] * HeightMultiplier, pt.Y));
                PointToIndex[pt] = index;
            }
            return index;
        }

        public void AddTri(int i1, int i2, int i3)
        {
            Indices.Add(i1);
            Indices.Add(i2);
            Indices.Add(i3);
        }
    }

    public class HeightmapQuadTreeNode
    {
        public bool Reduced;
        public int MipLevel;
        public int ParentInd = -1; //the 
        HeightmapQuadTreeNode Parent;
        public Rectangle Range;
        public HeightmapQuadTreeNode[] Children = new HeightmapQuadTreeNode[]
        {
            null, null, null, null //top left, top right, bottom left, bottom right (row order)
        };

        public HeightmapQuadTreeNode(HeightmapQuadTreeNode parent, Rectangle range)
        {
            Parent = parent;
            Range = range;
            MipLevel = (parent?.MipLevel ?? 6) - 1;
        }

        public HeightmapQuadTreeNode GetOrAdd(int index, bool doSpread)
        {
            HeightmapQuadTreeNode result;
            if (Children[index] == null)
            {
                var rect = Range;
                rect.Width /= 2;
                rect.Height /= 2;
                if ((index % 2) == 1) rect.X += rect.Width;
                if (index > 1) rect.Y += rect.Height;
                result = new HeightmapQuadTreeNode(this, rect);
                result.ParentInd = index;
                Children[index] = result;
                doSpread = true;
            } else {
                result = Children[index];
            }

            if (doSpread) {
                if (Parent != null)
                {
                    //find adjacent quad to add to. 
                    //for example if we are in index 0 (top left), make sure there is:
                    // - a subdivision in the top right (1) of the tile to our left,
                    // - a subdivision in the bottom left (2) of the tile above us,
                    // - a subdivision in the bottom right (3) of the tile above and left

                    //index 1 (top right
                    switch (index)
                    {
                        case 0: //top left
                            {
                                var left = result.FindOrCreateQuadInDirection(3);
                                var up = result.FindOrCreateQuadInDirection(0);
                                if (up != null) up.FindOrCreateQuadInDirection(3);
                                break;
                            }
                        case 1: //top right
                            {
                                var right = result.FindOrCreateQuadInDirection(1);
                                var up = result.FindOrCreateQuadInDirection(0);
                                if (up != null) up.FindOrCreateQuadInDirection(1);
                                break;
                            }
                        case 2: //bottom left
                            {
                                var left = result.FindOrCreateQuadInDirection(3);
                                var bottom = result.FindOrCreateQuadInDirection(2);
                                if (bottom != null) bottom.FindOrCreateQuadInDirection(3);
                                break;
                            }
                        case 3: //bottom right
                            {
                                var right = result.FindOrCreateQuadInDirection(1);
                                var bottom = result.FindOrCreateQuadInDirection(2);
                                if (bottom != null) bottom.FindOrCreateQuadInDirection(1);
                                break;
                            }
                    }
                }
            }
            return result;
        }

        public virtual HeightmapQuadTreeNode FindOrCreateQuadInDirection(int dir)
        {
            //dir: up, right, down, left

            if (Parent == null) return null;
            switch (dir)
            {
                case 0: //up
                    //if we're on the bottom row, finding the quad is easy.
                    if (ParentInd > 1)
                    {
                        return Parent.GetOrAdd(ParentInd - 2, false);
                    }
                    else
                    {
                        //on the top row. we need to break out to add a quad above.
                        var aboveParent = Parent.FindOrCreateQuadInDirection(dir);
                        //our adjacent should be on the above parent's bottom row.
                        return aboveParent?.GetOrAdd(ParentInd + 2, false);
                    }
                case 1: //right
                    //if we're on the left row, finding the quad is easy.
                    if ((ParentInd % 2) == 0)
                    {
                        return Parent.GetOrAdd(ParentInd + 1, false);
                    }
                    else
                    {
                        //on the right row. we need to break out to add a quad above.
                        var rightParent = Parent.FindOrCreateQuadInDirection(dir);
                        //our adjacent should be on the right parent's left row.
                        return rightParent?.GetOrAdd(ParentInd - 1, false);
                    }
                case 2: //down
                    //if we're on the top row, finding the quad is easy.
                    if (ParentInd < 2)
                    {
                        return Parent.GetOrAdd(ParentInd + 2, false);
                    }
                    else
                    {
                        //on the right row. we need to break out to add a quad above.
                        var belowParent = Parent.FindOrCreateQuadInDirection(dir);
                        //our adjacent should be on the below parent's top row.
                        return belowParent?.GetOrAdd(ParentInd - 2, false);
                    }
                case 3: //left
                    //if we're on the right row, finding the quad is easy.
                    if ((ParentInd % 2) == 1)
                    {
                        return Parent.GetOrAdd(ParentInd - 1, false);
                    }
                    else
                    {
                        //on the left row. we need to break out to add a quad above.
                        var leftParent = Parent.FindOrCreateQuadInDirection(dir);
                        //our adjacent should be on the left parent's right row.
                        return leftParent?.GetOrAdd(ParentInd + 1, false);
                    }
            }
            return null;
        }

        public void Triangulate(SimplifiedHeightmap parent)
        {
            var cTriangulated = 0;
            foreach (var child in Children)
            {
                if (child != null)
                {
                    child.Triangulate(parent);
                    cTriangulated++;
                }
            }
            if (cTriangulated == 0)
            {
                //no children means we are a leaf. triangulate, cause nobody else is doing it for me.
                var lt = parent.GetVertex(Range.Location);
                var rt = parent.GetVertex(Range.Location + new Point(Range.Width, 0));
                var rb = parent.GetVertex(Range.Location + Range.Size);
                var lb = parent.GetVertex(Range.Location + new Point(0, Range.Height));

                parent.AddTri(lt, rt, rb);
                parent.AddTri(lt, rb, lb);
            }
            else if (cTriangulated < 4)
            {
                //complex: we have children, but we also need to make our own geometry.
                var ctr = parent.GetVertex(Range.Location + new Point(Range.Width/2, Range.Height/2));
                var lt = parent.GetVertex(Range.Location);
                var rt = parent.GetVertex(Range.Location + new Point(Range.Width, 0));
                var rb = parent.GetVertex(Range.Location + Range.Size);
                var lb = parent.GetVertex(Range.Location + new Point(0, Range.Height));
                if (Children[0] == null) //from top left
                {
                    if (Children[1] == null) //top right
                    {
                        //triangle lt to rt: \/
                        parent.AddTri(lt, rt, ctr);
                    }
                    else
                    {
                        //triangle lt to mt: \|
                        var mt = parent.GetVertex(Range.Location + new Point(Range.Width / 2, 0));
                        parent.AddTri(lt, mt, ctr);
                    }

                    if (Children[2] == null) //bottom left
                    {
                        //triangle lt to lb: \
                        //                   /
                        parent.AddTri(lt, ctr, lb);
                    }
                    else
                    {
                        //triangle lt to lm: _\
                        var lm = parent.GetVertex(Range.Location + new Point(0, Range.Height / 2));
                        parent.AddTri(lt, ctr, lm);
                    }
                }
                else
                {
                    if (Children[1] == null) //top right but no top left
                    {
                        //triangle mt to rt: |/
                        var mt = parent.GetVertex(Range.Location + new Point(Range.Width / 2, 0));
                        parent.AddTri(mt, rt, ctr);
                    }

                    if (Children[2] == null) //bottom left but no top left
                    {
                        //triangle lm to lb: _
                        //                   /
                        var lm = parent.GetVertex(Range.Location + new Point(0, Range.Height / 2));
                        parent.AddTri(lm, ctr, lb);
                    }
                }

                if (Children[3] == null) //from bottom right
                {
                    if (Children[1] == null) //top right
                    {
                        //triangle rt to rb: /
                        //                   \
                        parent.AddTri(rt, rb, ctr);
                    }
                    else
                    {
                        //triangle rm to rb: _
                        //                   \
                        var rm = parent.GetVertex(Range.Location + new Point(Range.Width, Range.Height / 2));
                        parent.AddTri(rm, rb, ctr);
                    }

                    if (Children[2] == null) //bottom left
                    {
                        //triangle lb to rb: /\
                        parent.AddTri(lb, ctr, rb);
                    }
                    else
                    {
                        //triangle mb to rb: |\
                        var mb = parent.GetVertex(Range.Location + new Point(Range.Width / 2, Range.Height));
                        parent.AddTri(mb, ctr, rb);
                    }
                }
                else
                {
                    if (Children[1] == null) //top right, no bottom right
                    {
                        //triangle rt to rm: /_
                        var rm = parent.GetVertex(Range.Location + new Point(Range.Width, Range.Height / 2));
                        parent.AddTri(rt, rm, ctr);
                    }

                    if (Children[2] == null) //bottom left, no bottom right
                    {
                        //triangle mb to lb: /|
                        var mb = parent.GetVertex(Range.Location + new Point(Range.Width / 2, Range.Height));
                        parent.AddTri(mb, lb, ctr);
                    }
                }
            }
        }
    }

    public class HeightmapChunk : HeightmapQuadTreeNode
    {
        public HeightmapChunk[] Adjacent = new HeightmapChunk[]
        {
            null, null, null, null //up, right, down, left
        };

        public HeightmapChunk(Rectangle range, int mipLevel, HeightmapChunk left, HeightmapChunk top) : base(null, range)
        {
            Adjacent[3] = left;
            Adjacent[0] = top;
            if (left != null) left.Adjacent[1] = this;
            if (top != null) top.Adjacent[2] = this;
            MipLevel = mipLevel;
        }

        public override HeightmapQuadTreeNode FindOrCreateQuadInDirection(int dir)
        {
            return Adjacent[dir];
        }
    }
}
