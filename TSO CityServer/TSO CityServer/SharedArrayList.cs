/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSO CityServer.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections;
using System.Text;

namespace TSO_CityServer
{
    public class SharedArrayList
    {
        private ArrayList m_List = new ArrayList();

        public void AddItem(object Item)
        {
            lock (m_List)
            {
                if(!m_List.Contains(Item))
                    m_List.Add(Item);
            }
        }

        public ArrayList GetList()
        {
            ArrayList List;

            //This has to be a copy.
            lock (m_List)
            {
                List = m_List;
            }

            return List;
        }
    }
}
