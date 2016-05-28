using FSO.LotView;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.Client
{
    public class GameStartProxy
    {
        public void Start(bool useDX)
        {
            using (TSOGame game = new TSOGame())
            {
                GameFacade.DirectX = useDX;
                World.DirectX = useDX;
                game.Run(Microsoft.Xna.Framework.GameRunBehavior.Synchronous);
            }
        }
    }
}
