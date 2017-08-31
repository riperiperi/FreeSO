using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
