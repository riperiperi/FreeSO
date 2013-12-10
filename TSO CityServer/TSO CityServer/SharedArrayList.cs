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
