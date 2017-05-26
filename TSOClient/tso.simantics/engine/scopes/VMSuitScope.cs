/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.SimAntics.Engine.Scopes
{
    public enum VMSuitScope
    {
        Global = 0,
        Person = 1,
        Object = 2
    }

    public enum VMPersonSuits
    {
        DefaultDaywear = 0,
        Naked = 1,
        DefaultSwimwear = 2,
        JobOutfit = 3,
        
        DefaultSleepwear = 5,
        SkeletonPlus = 6,
        SkeletonMinus = 7,
        DecorationHead = 8,
        DecorationBack = 9,
        DecorationShoes = 10,
        DecorationTail = 11,

        TeleporterMishap = 20,
        CockroachHead = 21,
        DynamicDaywear = 22,
        DynamicSwimwear = 23,
        DynamicSleepwear = 24,
        DynamicCostume = 25,
        signnotepad = 26
    }

    public class VMPersonSuitsUtils
    {
        public static bool IsDefaultSuit(VMPersonSuits type)
        {
            if (type != VMPersonSuits.DefaultDaywear &&
                type != VMPersonSuits.DefaultSleepwear &&
                type != VMPersonSuits.DefaultSwimwear)
            {
                return false;
            }
            return true;
        }

        public static ulong GetValue(VMAvatar avatar, VMPersonSuits type)
        {
            switch (type)
            {
                case VMPersonSuits.DefaultDaywear:
                    return avatar.DefaultSuits.Daywear.ID;
                case VMPersonSuits.DefaultSleepwear:
                    return avatar.DefaultSuits.Sleepwear.ID;
                case VMPersonSuits.DefaultSwimwear:
                    return avatar.DefaultSuits.Swimwear.ID;
                case VMPersonSuits.DynamicCostume:
                    return avatar.DynamicSuits.Costume;
                case VMPersonSuits.DynamicDaywear:
                    return avatar.DynamicSuits.Daywear;
                case VMPersonSuits.DynamicSleepwear:
                    return avatar.DynamicSuits.Sleepwear;
                case VMPersonSuits.DynamicSwimwear:
                    return avatar.DynamicSuits.Swimwear;
            }
            return 0;
        }

        public static bool IsDecoration(VMPersonSuits suit)
        {
            if (suit == VMPersonSuits.DecorationHead ||
                suit == VMPersonSuits.DecorationBack ||
                suit == VMPersonSuits.DecorationShoes ||
                suit == VMPersonSuits.DecorationTail)
            {
                return true;
            }
            return false;
        }
    }
}
