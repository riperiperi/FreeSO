using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Framework;

namespace TSOClient
{
    public class ContentPreloadThread
    {
        private List<ContentPreloadThread> ActiveWorkers;
        private List<ContentPreload> Work;

        public ContentPreloadThread(List<ContentPreloadThread> activeWorkers, List<ContentPreload> work)
        {
            this.ActiveWorkers = activeWorkers;
            this.Work = work;
        }



        private void Preload(ContentPreload workItem)
        {
            var id = workItem.ID;

            try
            {
                var binaryData = ContentManager.GetResourceFromLongID(id);

                switch (workItem.Type)
                {
                    case ContentPreloadType.UITexture:
                        /** Apply alpha channel masking & load into GD **/
                        UIElement.StoreTexture(id, binaryData);
                        break;

                    case ContentPreloadType.Other:
                        ContentManager.TryToStoreResource(id, binaryData);
                        break;
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Failed to load file: " + id + ", " + e.Message);
            }
        }



        public void Run()
        {
            while (true)
            {
                ContentPreload workItem = null;

                lock (Work)
                {
                    if (Work.Count == 0)
                    {
                        break;
                    }
                    else
                    {
                        workItem = Work[0];
                        Work.RemoveAt(0);
                    }
                }


                if (workItem != null)
                {
                    Preload(workItem);
                }
            }

                
            /** All done **/
            ActiveWorkers.Remove(this);
        }
    }
}
