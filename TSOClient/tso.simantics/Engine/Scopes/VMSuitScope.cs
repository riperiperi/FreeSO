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
        TS1Formal = 4,
        DefaultSleepwear = 5,
        SkeletonPlus = 6,
        SkeletonMinus = 7,
        DecorationHead = 8,
        DecorationBack = 9,
        DecorationShoes = 10,
        DecorationTail = 11,

        TS1Toga = 8,
        TS1Country = 9,
        TS1Luau = 10,
        TS1Rave = 11,
        TS1Costume = 12, //person strings 27. hands: 28, 29
        TS1ExpandedFormal = 13, //person strings 30 (f###)
        TS1ExpandedSwimsuit = 14, //person strings 31 (s###)
        TS1ExpandedPajamas = 15, //person strings 32 (l###)
        TS1Disco = 16, //???
        TS1Winter = 17, //person strings 33 (w###)
        TS1HighFashion = 18, //person strings 34 (h###)

        TeleporterMishap = 20,
        CockroachHead = 21,
        DynamicDaywear = 22,
        DynamicSwimwear = 23,
        DynamicSleepwear = 24,
        DynamicCostume = 25,
        signnotepad = 26,

        FSOInvisible = 128,

        Head = 65535 //internal
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
