/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using FSO.Files;

namespace FSO.Client.Rendering.City
{
    public class CityDataRetriever
    {
        public LotTileEntry[] LotTileData; //the renderer requests this on entry, so it must be populated before initilizing the CityRenderer!

        public void RetrieveHouseGFX(int id, Dictionary<int, Texture2D> destination, GraphicsDevice GfxDevice) {
            try
            {
                destination[id] = ImageLoader.FromStream(GfxDevice, new FileStream("housethumb\\" + id.ToString() + ".png", FileMode.Open, FileAccess.Read, FileShare.Read)); //interesting challenge o'clock - this will need to be an asynchronous load from the server in the real game!
                //the lot will display as the default house until the Texture2D is set otherwise. Right now it is set synchronously above, so you
                //cannot see this behaviour. If the lot image cannot load (right now if the file doesn't exist, in practice probably if image 
                //download somehow fails) it will be stuck as the default image and will not load again.

            } catch (Exception) {
                //do nothing... It will stay as the old image
            }
        }
    }
}
