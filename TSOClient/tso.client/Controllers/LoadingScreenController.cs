using FSO.Client.UI.Screens;
using FSO.Common.Content;
using FSO.Common.Utils.Cache;
using FSO.Content;
using FSO.SimAntics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.Controllers
{
    public class LoadingScreenController
    {
        public ContentPreloader Loader;

        public LoadingScreenController(LoadingScreen view, Content.Content content, ICache cache)
        {
            Loader = new ContentPreloader();

            Loader.MainContentAction = (Action donePart) =>
            {
                VMContext.InitVMConfig(false);
                FSO.Content.Content.Init(GlobalSettings.Default.StartupPath, GameFacade.GraphicsDevice);
            };

            /** Init cache **/
            Loader.Add(new CacheInit((FileSystemCache)cache));

            /*
            // UI Textures
            Loader.Add(content.UIGraphics.List());
            //Sim stuff
            Loader.Add(content.AvatarOutfits.List());
            Loader.Add(content.AvatarAppearances.List());
            Loader.Add(content.AvatarPurchasables.List());
            Loader.Add(content.AvatarThumbnails.List());
            */
        }

        public void Preload()
        {
            Loader.Preload(GameFacade.GraphicsDevice);
        }
    }

    /// <summary>
    /// Not really content, but allows us to keep the loading UI going until init is done
    /// </summary>
    public class CacheInit : IContentReference
    {
        private FileSystemCache Cache;

        public CacheInit(FileSystemCache cache)
        {
            this.Cache = cache;
        }

        public object GetGeneric()
        {
            Cache.Init();
            return Cache;
        }

        public object GetThrowawayGeneric()
        {
            throw new NotImplementedException();
        }
    }
}
