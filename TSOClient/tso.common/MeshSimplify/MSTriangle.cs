using Microsoft.Xna.Framework;

namespace FSO.Common.MeshSimplify
{
    public class MSTriangle
    {
        public int[] v = new int[3];
        public double[] err = new double[4];
        public bool deleted, dirty;
        public Vector3 n;
    }
}
