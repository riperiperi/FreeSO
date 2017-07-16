/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using FSO.LotView.Model;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.LotView.LMap
{
    public class GPURoomMaps : IDisposable
    {
        public List<Texture2D> RoomMaps;
        private GraphicsDevice GD;

        public GPURoomMaps(GraphicsDevice device)
        {
            GD = device;
        }

        public void Init(Blueprint blueprint)
        {
            var w = blueprint.Width;
            var h = blueprint.Height;

            RoomMaps = new List<Texture2D>();
            for (int i = 0; i < 5; i++) RoomMaps.Add(new Texture2D(GD, w, h, false, SurfaceFormat.Color));
        }

        public void SetRoomMap(sbyte floor, uint[] map)
        {
            RoomMaps[floor].SetData(map);
        }

        public void Dispose()
        {
            if (RoomMaps != null)
            {
                foreach (var map in RoomMaps)
                {
                    map?.Dispose();
                }
            }
        }
    }
}
