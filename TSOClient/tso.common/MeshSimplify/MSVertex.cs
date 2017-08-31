using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.MeshSimplify
{
    public class MSVertex
    {
        public Vector3 p;
        public Vector2 t; //texcoord
        public int tstart, tcount;
        public SymmetricMatrix q;
        public bool border;
    }
}
