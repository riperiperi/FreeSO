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
			TSOGame game = new TSOGame();
            GameFacade.DirectX = useDX;
			World.DirectX = useDX;
            game.Run();
        }

		public void SetPath(string path)
		{
			GlobalSettings.Default.StartupPath = path;
		}
	}
}
