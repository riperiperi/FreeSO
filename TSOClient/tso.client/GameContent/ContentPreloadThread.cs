/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Client.UI.Framework;

namespace FSO.Client.GameContent
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
                var item = ContentManager.GetResourceInfo(id);

                switch (workItem.Type)
                {
                    case ContentPreloadType.UITexture:
                        /** Apply alpha channel masking & load into GD **/
                        //UIElement.StoreTexture(id, item);
                        break;

                    case ContentPreloadType.UITexture_NoMask:
                        //UIElement.StoreTexture(id, item, false);
                        break;

                    case ContentPreloadType.Other:
                        ContentManager.TryToStoreResource(id, item);
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
