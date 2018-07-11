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

        public static VMOutfitReference GetPersonSuitTS1(VMAvatar avatar, ushort id)
        {
            var type = (VMPersonSuits)id;
            var bodyStrings = avatar.Object.Resource.Get<STR>(avatar.Object.OBJ.BodyStringID);
            bool male = (avatar.GetPersonData(VMPersonDataVariable.Gender) == 0);
            var age = avatar.GetPersonData(VMPersonDataVariable.PersonsAge);
            var skn = bodyStrings.GetString(14).ToLowerInvariant();
            var child = (age < 18 && age != 0);

            var code = "fit";
            if (bodyStrings.GetString(1).ToLowerInvariant().Contains("fat"))
                code = "fat";
            else if (bodyStrings.GetString(1).ToLowerInvariant().Contains("skn"))
                code = "skn";
            if (child) code = "uchd";
            else code = (male ? "m" : "f") + code;

            VMOutfitReference toHandCopy = null;
            switch (type)
            {
                //todo: (tail etc), cockroach head

                case VMPersonSuits.DefaultDaywear:
                    return new VMOutfitReference(bodyStrings, false);
                case VMPersonSuits.Naked:
                    toHandCopy = new VMOutfitReference("n"+code+"_01,BODY="+"n"+code+skn+"_01", false);
                    break;
                case VMPersonSuits.DefaultSwimwear:
                    if (IsValid(bodyStrings.GetString(31))) goto case VMPersonSuits.TS1ExpandedSwimsuit;
                    toHandCopy = new VMOutfitReference("n" + code + "_01,BODY=" + "u" + code + skn + "_"+((male && !child)?"briefs":"undies")+"01", false);
                    break;
                case VMPersonSuits.TS1Formal:
                    if (IsValid(bodyStrings.GetString(30))) goto case VMPersonSuits.TS1ExpandedFormal;
                    toHandCopy = new VMOutfitReference("f" + code + "_01,BODY=" + "f" + code + skn + "_01", false);
                    break;
                case VMPersonSuits.JobOutfit:
                    var jtype = avatar.GetPersonData(FSO.SimAntics.Model.VMPersonDataVariable.JobType);
                    var level = avatar.GetPersonData(FSO.SimAntics.Model.VMPersonDataVariable.JobPromotionLevel);

                    var job = Content.Content.Get().Jobs.GetJobLevel(jtype, level);

                    var bskn = job.MaleUniformMesh;
                    if (bskn == "") return new VMOutfitReference(bodyStrings, false);

                    if (!male && job.FemaleUniformMesh != null) bskn = job.FemaleUniformMesh;
                    bskn = bskn.ToLowerInvariant().Replace("$g", code[0].ToString()).Replace("$b", code.Substring(1)).Replace("$c", skn);
                    var btex = job.UniformSkin;
                    btex = btex.ToLowerInvariant().Replace("$g", code[0].ToString()).Replace("$b", code.Substring(1)).Replace("$c", skn);

                    toHandCopy = new VMOutfitReference(bskn+",BODY=" + btex, false);
                    break;
                case VMPersonSuits.DefaultSleepwear:
                    if (IsValid(bodyStrings.GetString(32))) goto case VMPersonSuits.TS1ExpandedPajamas;
                    var pj = (child ? "pjs" : "pajama");
                    var gen = (child ? (male ? "m" : "f") : (code));
                    var gen2 = (male ? "m" : "f");
                    toHandCopy = new VMOutfitReference(pj + gen + "_01,BODY=" + pj + gen2 + skn + "_01", false);
                    break;
                case VMPersonSuits.SkeletonPlus:
                    toHandCopy = new VMOutfitReference((child?"skeletonchd_01": "skeleton_01")+",BODY=skeleton_01", true);
                    break;
                case VMPersonSuits.SkeletonMinus:
                    toHandCopy = new VMOutfitReference((child ? "skeletonchd_01" : "skeleton_01") + ",BODY=skeleneg_01", true);
                    break;
                case VMPersonSuits.TS1Toga:
                case VMPersonSuits.TS1Country:
                case VMPersonSuits.TS1Luau:
                case VMPersonSuits.TS1Rave:
                case VMPersonSuits.TS1Costume: //person strings 27. hands: 28, 29
                case VMPersonSuits.TS1ExpandedFormal: //person strings 30 (f###)
                    toHandCopy = new VMOutfitReference(bodyStrings.GetString(30), false);
                    break;
                case VMPersonSuits.TS1ExpandedSwimsuit: //person strings 31 (s###)
                    toHandCopy = new VMOutfitReference(bodyStrings.GetString(31), false);
                    break;
                case VMPersonSuits.TS1ExpandedPajamas: //person strings 32 (l###)
                    toHandCopy = new VMOutfitReference(bodyStrings.GetString(32), false);
                    break;
                case VMPersonSuits.TS1Disco: //???
                case VMPersonSuits.TS1Winter: //person strings 33 (w###)
                    toHandCopy = new VMOutfitReference(bodyStrings.GetString(33), false);
                    break;
                case VMPersonSuits.TS1HighFashion: //person strings 34 (h###)
                    toHandCopy = new VMOutfitReference(bodyStrings.GetString(34), false);
                    break;
            }

            if (toHandCopy == null) return null;
            else
            {
                if (avatar.DefaultSuits.Daywear.OftData?.LiteralHandgroup == null)
                {
                    var oft = new Vitaboy.Outfit();
                    oft.Read(bodyStrings);
                    toHandCopy.OftData.LiteralHandgroup = oft.LiteralHandgroup;
                }
                else toHandCopy.OftData.LiteralHandgroup = avatar.DefaultSuits.Daywear.OftData?.LiteralHandgroup;
            }
            return toHandCopy;
        }

        private static bool IsValid(string suitName)
        {
            return !(suitName == null || suitName == "" || suitName == "ADDED");
        }

        public static object GetSuit(VMStackFrame context, VMSuitScope scope, ushort id)
        {
            STR suitTable = null;

            var avatar = (VMAvatar)context.Caller;

            switch (scope)
            {
                case VMSuitScope.Object:
                    suitTable = context.CodeOwner.Resource.Get<STR>(304);
                    break;
                case VMSuitScope.Global:
                    suitTable = context.Global.Resource.Get<STR>(304);
                    break;
                case VMSuitScope.Person:
                    //get outfit from person
                    if (context.VM.TS1) return GetPersonSuitTS1((VMAvatar)context.Caller, id);

                    var type = (VMPersonSuits)id;
                    bool male = (avatar.GetPersonData(VMPersonDataVariable.Gender) == 0);
                    switch (type)
                    {
                        //todo: (tail etc), cockroach head

                        case VMPersonSuits.DefaultDaywear:
                            return avatar.DefaultSuits.Daywear.ID;
                        case VMPersonSuits.Naked:
                            return (ulong)(male ? 0x24E0000000D : 0x10000000D);
                        case VMPersonSuits.DefaultSwimwear:
                            return avatar.DefaultSuits.Swimwear.ID;
                        case VMPersonSuits.JobOutfit:
                            if (context.VM.TS1) return null;
                            var job = avatar.GetPersonData(VMPersonDataVariable.OnlineJobID);
                            if (job < 1 || job > 5) return null;
                            var level = Math.Max(0, Math.Min(3, (avatar.GetPersonData(VMPersonDataVariable.OnlineJobGrade) - 1) / 4));
                            return (ulong)(JobOutfits[male?0:1][job-1][level]);
                        case VMPersonSuits.DefaultSleepwear:
                            return avatar.DefaultSuits.Sleepwear.ID;
                        case VMPersonSuits.SkeletonPlus:
                            return (ulong)(0x5750000000D);
                        case VMPersonSuits.SkeletonMinus:
                            return (ulong)(0x5740000000D);
                        case VMPersonSuits.TeleporterMishap:
                          return (ulong)(male ? 0x2900000000D : 0x4A0000000D);


                        case VMPersonSuits.DynamicDaywear:
                            return avatar.DynamicSuits.Daywear;
                        case VMPersonSuits.DynamicSleepwear:
                            return avatar.DynamicSuits.Sleepwear;
                        case VMPersonSuits.DynamicSwimwear:
                            return avatar.DynamicSuits.Swimwear;
                        case VMPersonSuits.DynamicCostume:
                            return avatar.DynamicSuits.Costume;

                        case VMPersonSuits.DecorationHead:
                            return avatar.Decoration.Head;
                        case VMPersonSuits.DecorationBack:
                            return avatar.Decoration.Back;
                        case VMPersonSuits.DecorationShoes:
                            return avatar.Decoration.Shoes;
                        case VMPersonSuits.DecorationTail:
                            return avatar.Decoration.Tail;
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
