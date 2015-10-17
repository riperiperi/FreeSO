using FSO.Client.UI.Screens;
using FSO.Content;
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

        public LoadingScreenController(LoadingScreen view, Content.Content content)
        {
            Loader = new ContentPreloader();
            /** UI Textures **/
            Loader.Add(content.UIGraphics.List());
            /** Sim stuff **/
            Loader.Add(content.AvatarOutfits.List());
            Loader.Add(content.AvatarAppearances.List());
            Loader.Add(content.AvatarPurchasables.List());
            Loader.Add(content.AvatarThumbnails.List());
        }

        public void Preload()
        {
            Loader.Preload(GameFacade.GraphicsDevice);
        }
    }
}
