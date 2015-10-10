/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using FSO.Common.Content;
using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics.Engine.Scopes;
using FSO.SimAntics.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.SimAntics.Engine
{
    public class VMSuitProvider
    {
        private static long[][][] JobOutfits = new long[][][] {
                        new long[][]{ //male
                new long[] { //robot
                    0x5870000000D,
                    0x5880000000D,
                    0x5890000000D,
                },

                new long[] { //waiter
                    0x58B0000000D,
                    0x58D0000000D,
                    0x58E0000000D,
                },

                new long[] { //cook
                    0x58A0000000D,
                    0x58C0000000D,
                    0x58F0000000D,
                },

                new long[] { //dj
                    0x5900000000D,
                    0x5920000000D,
                    0x5940000000D,
                },

                new long[] { //dancer
                    0x5910000000D,
                    0x5930000000D,
                    0x5950000000D,
                }
            },
            new long[][] { //female
                new long[] { //robot
                    0x5780000000D,
                    0x5790000000D,
                    0x57A0000000D
                },

                new long[] { //waiter
                    0x57C0000000D,
                    0x57E0000000D,
                    0x57F0000000D,
                },

                new long[] { //cook
                    0x57B0000000D,
                    0x57D0000000D,
                    0x5800000000D,
                },

                new long[] { //dj
                    0x5810000000D,
                    0x5830000000D,
                    0x5850000000D,
                },

                new long[] { //dancer
                    0x5820000000D,
                    0x5840000000D,
                    0x5860000000D,
                }
            }
        };

        public static object GetSuit(VMStackFrame context, VMSuitScope scope, ushort id)
        {
            STR suitTable = null;

            var avatar = (VMAvatar)context.Caller;

            switch (scope)
            {
                case VMSuitScope.Object:
                    suitTable = context.Callee.Object.Resource.Get<STR>(304);
                    break;
                case VMSuitScope.Global:
                    suitTable = context.Global.Resource.Get<STR>(304);
                    break;
                case VMSuitScope.Person:
                    //get outfit from person
                    var type = (VMPersonSuits)id;
                    bool male = (avatar.GetPersonData(VMPersonDataVariable.Gender) == 0);
                    switch (type)
                    {
                        //todo: Dynamic Daywear

                        case VMPersonSuits.DefaultDaywear:
                            return avatar.DefaultSuits.Daywear;
                        case VMPersonSuits.Naked:
                            return (ulong)(male ? 0x24E0000000D : 0x10000000D);
                        case VMPersonSuits.DefaultSwimwear:
                            return avatar.DefaultSuits.Swimwear;
                        case VMPersonSuits.JobOutfit:
                            var job = avatar.GetPersonData(VMPersonDataVariable.OnlineJobID);
                            if (job < 1 || job > 5) return null;
                            var level = Math.Max(0, Math.Min(3, (avatar.GetPersonData(VMPersonDataVariable.OnlineJobGrade) - 1) / 4));
                            return (ulong)(JobOutfits[male?0:1][job-1][level]);
                        case VMPersonSuits.DefaultSleepwear:
                            return avatar.DefaultSuits.Sleepwear;
                        case VMPersonSuits.SkeletonPlus:
                            return (ulong)(0x5750000000D);
                        case VMPersonSuits.SkeletonMinus:
                            return (ulong)(0x5740000000D);
                        case VMPersonSuits.TeleporterMishap:
                          return (ulong)(male ? 0x2B70000000D : 0x620000000D);
                    }

                    return null;
            }

            if (suitTable != null)
            {
                var suitFile = suitTable.GetString(id) + ".apr";

                return suitFile;
                //var apr = FSO.Content.Content.Get().AvatarAppearances.Get(suitFile);
                //return apr;
            }
            return null;
        }
    }
}
