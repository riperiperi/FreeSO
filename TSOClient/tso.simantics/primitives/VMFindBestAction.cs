/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.SimAntics.Engine;
using FSO.Files.Utils;
using FSO.SimAntics.Model;
using FSO.Files.Formats.IFF.Chunks;

namespace FSO.SimAntics.Primitives
{
    // Finds a suitable action and queues it onto this sim. Used for pet free will.

    public class VMFindBestAction : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var ents = new List<VMEntity>(context.VM.Entities);
            var processed = new HashSet<short>();
            var pos1 = context.Caller.Position;

            VMEntity bestActor = null;
            VMPieMenuInteraction bestAction = null;
            int bestScore = 0;
            foreach (var iobj in ents)
            {
                var obj = iobj.MultitileGroup.GetInteractionGroupLeader(iobj);
                if (processed.Contains(obj.ObjectID) || (obj is VMGameObject && ((VMGameObject)obj).Disabled > 0)) continue;
                processed.Add(obj.ObjectID);

                var pos2 = obj.Position;
                var distance = (short)Math.Floor(Math.Sqrt(Math.Pow(pos1.x - pos2.x, 2) + Math.Pow(pos1.y - pos2.y, 2)) / 16.0);

                var pie = obj.GetPieMenu(context.VM, context.Caller, true);
                int bestMyScore = 0;
                VMPieMenuInteraction bestMyAction = null;
                foreach (var item in pie)
                {
                    //motive scores must be above their threshold.
                    //pick maximum motive score as our base score
                    int baseScore = 0;
                    for (int i = 0; i < item.Entry.MotiveEntries.Length; i++)
                    {
                        var motiveScore = item.Entry.MotiveEntries[i];
                        if (motiveScore.EffectRangeMaximum == 0 && motiveScore.EffectRangeMinimum == 0) continue;
                        //LINEAR INTERPOLATE MIN SCORE TO MAX, using motive data of caller
                        //MAX is when motive is the lowest.
                        //can be in reverse too!
                        var myMotive = Math.Max((short)0, ((VMAvatar)context.Caller).GetMotiveData((VMMotive)i)); //below mid motives causes max atten right now.

                        int interpScore = (myMotive*motiveScore.EffectRangeMinimum + (100-myMotive)*(motiveScore.EffectRangeMinimum+motiveScore.EffectRangeMaximum)) / 100;
                        if (interpScore > baseScore) baseScore = interpScore;
                    }
                    float atten = (item.Entry.AttenuationCode == 0) ? item.Entry.AttenuationValue : TTAB.AttenuationValues[item.Entry.AttenuationCode];
                    int attenScore = (int)Math.Max(0, baseScore * (1f - (distance * atten)));
                    if (attenScore != 0) attenScore += (int)context.VM.Context.NextRandom(31) - 15;
                    if (attenScore > item.Entry.AutonomyThreshold && attenScore > bestMyScore)
                    {
                        bestMyScore = attenScore;
                        bestMyAction = item;
                    }
                }

                if (bestMyAction != null && bestMyScore > bestScore)
                {
                    bestActor = obj;
                    bestAction = bestMyAction;
                    bestScore = bestMyScore;
                }
            }

            if (bestActor != null)
            {
                bestActor.PushUserInteraction(bestAction.ID, context.Caller, context.VM.Context, new short[] { bestAction.Param0, 0, 0, 0 });
                return VMPrimitiveExitCode.GOTO_TRUE;
            }

            return VMPrimitiveExitCode.GOTO_FALSE; //we couldn't find anything... because we didn't check! TODO!!
        }
    }

    public class VMFindBestActionOperand : VMPrimitiveOperand
    {
        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
            }
        }

        public void Write(byte[] bytes) { }
        #endregion
    }
}
