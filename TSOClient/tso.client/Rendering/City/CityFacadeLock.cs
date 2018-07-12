using FSO.Client.Controllers;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.Rendering.City
{
    /// <summary>
    /// Data structure for locking facades near the current lot into memory.
    /// Not doing this would require a scan around the current lot of like 48x48 tiles every frame, which is really just wasting cpu cycles.
    /// Also includes bounding boxes for them, so they can be frustrum culled for MAXIMUM PERFORMANCE.
    /// </summary>
    public class CityFacadeLock : IDisposable
    {
        public List<CityFacadeEntry> Entries = new List<CityFacadeEntry>();

        public CityFacadeLock()
        {
        }

        public void Dispose()
        {
            foreach (var entry in Entries)
            {
                entry.LotImg.Held--;
            }
            Entries.Clear();
        }
    }

    public class CityFacadeEntry
    {
        public LotThumbEntry LotImg;
        public Vector3 Position;
        public BoundingBox Bounds;
        public Vector2 Location;
    }
}
