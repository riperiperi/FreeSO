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
                var item = ContentManager.GetResourceInfo(id);

                switch (workItem.Type)
                {
                    case ContentPreloadType.UITexture:
                        /** Apply alpha channel masking & load into GD **/
                        UIElement.StoreTexture(id, item);
                        break;

                    case ContentPreloadType.UITexture_NoMask:
                        UIElement.StoreTexture(id, item, false, true);
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
