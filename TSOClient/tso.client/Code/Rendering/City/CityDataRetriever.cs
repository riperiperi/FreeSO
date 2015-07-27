/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
Rhys Simpson. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using TSO.Files;

namespace TSOClient.Code.Rendering.City
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

        public CityDataRetriever()
        {
            LotTileData = new LotTileEntry[] {  //fun, exciting data to use as a placeholder
                new LotTileEntry(44630806, "Testlot", 176, 296, 0, 2000),
                new LotTileEntry(44696332, "Testlot", 177, 309, 1, 2000),
                new LotTileEntry(44761872, "Testlot", 161, 221, 0, 2000),
                new LotTileEntry(44827399, "Testlot", 159, 221, 0, 2000),
                new LotTileEntry(44827403, "Testlot", 157, 223, 1, 2000),
                new LotTileEntry(44827406, "Testlot", 159, 224, 1, 2000),
                new LotTileEntry(44827409, "Testlot", 159, 228, 1, 2000),
                new LotTileEntry(44827410, "Testlot", 322, 198, 3, 2000),
                new LotTileEntry(44827410, "Testlot", 324, 197, 0, 2000),
                new LotTileEntry(44827417, "Testlot", 329, 201, 0, 2000),
                new LotTileEntry(44892941, "Testlot", 331, 196, 1, 2000),
                new LotTileEntry(44892953, "Testlot", 322, 186, 0, 2000),
                new LotTileEntry(44958480, "Testlot", 310, 194, 0, 2000),
                new LotTileEntry(45024008, "Testlot", 178, 309, 1, 2000),
                new LotTileEntry(45024018, "Testlot", 180, 307, 3, 2000),
                new LotTileEntry(45220628, "Testlot", 175, 305, 0, 2000),
                new LotTileEntry(45286160, "Testlot", 176, 296, 1, 2000),
                new LotTileEntry(45417213, "Testlot", 148, 273, 0, 2000),
                new LotTileEntry(45744910, "Testlot", 145, 272, 3, 2000),
                new LotTileEntry(45744911, "Testlot", 146, 270, 0, 2000),
                new LotTileEntry(45810447, "Testlot", 143, 270, 1, 2000),
                new LotTileEntry(46596866, "Testlot", 144, 268, 1, 2000),
                new LotTileEntry(46727935, "Testlot", 146, 267, 0, 2000),
                new LotTileEntry(46793470, "Testlot", 143, 266, 1, 2000),
                new LotTileEntry(46793471, "Testlot", 226, 172, 0, 2000),
                new LotTileEntry(46859005, "Testlot", 231, 167, 0, 2000),
                new LotTileEntry(49808168, "Testlot", 232, 176, 1, 2000),
                new LotTileEntry(49808169, "Testlot", 234, 172, 0, 2000),
                new LotTileEntry(49873702, "Testlot", 235, 171, 3, 2000),
                new LotTileEntry(49939238, "Testlot", 236, 173, 1, 2000),
                new LotTileEntry(50004754, "Testlot", 238, 168, 1, 2000),
                new LotTileEntry(50070305, "Testlot", 238, 175, 0, 2000),
            };
        }
    }
}
