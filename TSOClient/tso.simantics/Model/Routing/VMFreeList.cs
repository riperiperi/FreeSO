/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.SimAntics.Model.Routing
{
    //used for keeping track of free edges on walkable rectangles (to extend further rectangles out of) 

    public class VMFreeList
    {
        public LinkedList<VMFreeListRegion> List;

        public VMFreeList(int point) //forces a free section with 0 interior.
        {
            List = new LinkedList<VMFreeListRegion>();
            List.AddFirst(new VMFreeListRegion(point, point));
        }

        public VMFreeList(int start, int end)
        {
            List = new LinkedList<VMFreeListRegion>();
            
            if (start != end) List.AddFirst(new VMFreeListRegion(start, end));
        }

        public VMFreeList(VMFreeListRegion region)
        {
            List = new LinkedList<VMFreeListRegion>();

            if (region.a != region.b) List.AddFirst(region);
        }

        public void Subtract(VMFreeListRegion region)
        {
            var elem = List.First;
            while (elem != null)
            {
                var part = elem.Value;
                var next = elem.Next;

                if (!(region.a >= part.b || region.b <= part.a)) {

                    if (part.a >= region.a && region.b < part.b)
                    {
                        //erases starting from left
                        part.a = region.b;
                        if (part.a == part.b) List.Remove(elem);
                    }
                    else if (region.b >= part.b && region.a > part.a)
                    {
                        //erases starting from right
                        part.b = region.a;
                        if (part.a == part.b) List.Remove(elem);
                    }
                    else if (region.a <= part.a && region.b >= part.b)
                    {
                        //region entirely erases part
                        List.Remove(elem);
                    }
                    else if (region.a > part.a && region.b < part.b)
                    {
                        //region internally erases part (result is two)
                        var temp = part.b;
                        part.b = region.a;
                        List.AddAfter(elem, new VMFreeListRegion(region.b, temp));
                    }
                }
                elem = next;
            }
        }
    }

    public class VMFreeListRegion
    {
        public int a;
        public int b;

        public VMFreeListRegion(int a, int b)
        {
            this.a = a;
            this.b = b;
        }
    }
}
