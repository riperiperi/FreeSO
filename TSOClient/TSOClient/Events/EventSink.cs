using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSOClient.Events
{
    public class EventSink
    {
        public static List<EventObject> EventQueue = new List<EventObject>();

        public static void RegisterEvent(EventObject Event)
        {
            EventQueue.Add(Event);
        }
    }
}
