/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace tso.world
{
    public class WorldContent
    {
        public static ContentManager ContentManager;

        public static void Init(GameServiceContainer serviceContainer, string rootDir){
            ContentManager = new ContentManager(serviceContainer);
            ContentManager.RootDirectory = rootDir;
        }

        public static Effect _2DWorldBatchEffect
        {
            get{
                return ContentManager.Load<Effect>("Effects/2DWorldBatch");
            }
        }

        public static Texture2D GridTexture
        {
            get
            {
                return ContentManager.Load<Texture2D>("Textures/gridTexture");
            }
        }
    }
}
