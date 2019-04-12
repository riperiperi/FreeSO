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
using FSO.LotView.Model;

namespace FSO.SimAntics.Primitives
{
    // Finds a suitable action and queues it onto this sim. Used for pet free will.

    public class VMFindBestAction : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            //if we already have some action, do nothing.
            if (context.Caller.Thread.Queue.Any(x => x.Mode != VMQueueMode.Idle)) return VMPrimitiveExitCode.GOTO_TRUE;

            var ents = new List<VMEntity>(context.VM.Entities);
            var processed = new HashSet<short>();
            var pos1 = context.Caller.Position;

            List<VMPieMenuInteraction> validActions = new List<VMPieMenuInteraction>();
            List<VMPieMenuInteraction> betterActions = new List<VMPieMenuInteraction>();
            foreach (var iobj in ents)
            {
                if (iobj.Position == LotTilePos.OUT_OF_WORLD) continue;
                var obj = iobj.MultitileGroup.GetInteractionGroupLeader(iobj);
                if (processed.Contains(obj.ObjectID) || (obj is VMGameObject && ((VMGameObject)obj).Disabled > 0)) continue;
                processed.Add(obj.ObjectID);

                var pos2 = obj.Position;
                var distance = (short)Math.Floor(Math.Sqrt(Math.Pow(pos1.x - pos2.x, 2) + Math.Pow(pos1.y - pos2.y, 2)) / 16.0);

                if (obj.TreeTable == null) continue;
                foreach (var entry in obj.TreeTable.Interactions)
                {
                    //motive scores must be above their threshold.
                    //pick maximum motive score as our base score
                    int baseScore = 0;
                    int negScore = 0;
                    bool hasAuto = false;
                    for (int i = 0; i < entry.MotiveEntries.Length; i++)
                    {
                        var motiveScore = entry.MotiveEntries[i];
                        if (motiveScore.EffectRangeMaximum == 0 && motiveScore.EffectRangeMinimum == 0) continue;
                        hasAuto = true;
                        //LINEAR INTERPOLATE MIN SCORE TO MAX, using motive data of caller
                        //MAX is when motive is the lowest.
                        //can be in reverse too!
                        var myMotive = ((VMAvatar)context.Caller).GetMotiveData((VMMotive)i);

                        var rangeScore = motiveScore.EffectRangeMinimum + motiveScore.EffectRangeMaximum - myMotive; //0 at max, EffectRangeMaximum at minimum.
                        if (motiveScore.EffectRangeMaximum > 0) rangeScore = Math.Max(0, Math.Min(motiveScore.EffectRangeMaximum, rangeScore)); //enforce range
                        else rangeScore = Math.Max(motiveScore.EffectRangeMaximum, Math.Max(0, rangeScore)); //also for negative
                        //todo: personality ads add values 0-100 for their given personality. makes things like viewing flamingo much more common.
                        baseScore = Math.Max(baseScore, rangeScore);
                        negScore = Math.Min(negScore, rangeScore);

                        //int interpScore = (myMotive*motiveScore.EffectRangeMinimum + (100-myMotive)*(motiveScore.EffectRangeMinimum+motiveScore.EffectRangeMaximum)) / 100;
                        //if (interpScore > baseScore) baseScore = interpScore;
                    }
                    baseScore -= negScore;
                    float atten = (entry.AttenuationCode == 0) ? entry.AttenuationValue : (entry.AttenuationCode < TTAB.AttenuationValues.Length) ? TTAB.AttenuationValues[entry.AttenuationCode] : 0f;
                    int attenScore = (int)Math.Max(0, baseScore * (1f - (distance * atten)));
                    if (attenScore != 0) attenScore += (int)context.VM.Context.NextRandom(31) - 15;



                    if (hasAuto) {
                        var id = entry.TTAIndex;
                        var caller = context.Caller;
                        var action = obj.GetAction((int)id, caller, context.VM.Context, false);
                        TTAs ttas = obj.TreeTableStrings;

                        caller.ObjectData[(int)VMStackObjectVariable.HideInteraction] = 0;
                        if (action != null) action.Flags &= ~TTABFlags.MustRun;
                        var actionStrings = caller.Thread.CheckAction(action);

                        var pie = new List<VMPieMenuInteraction>();
                        if (actionStrings != null)
                        {
                            if (actionStrings.Count > 0)
                            {
                                foreach (var actionS in actionStrings)
                                {
                                    actionS.ID = (byte)id;
                                    actionS.Entry = entry;
                                    actionS.Global = false;
                                    pie.Add(actionS);
                                }
                            }
                            else
                            {
                                if (ttas != null)
                                {
                                    pie.Add(new VMPieMenuInteraction()
                                    {
                                        Name = ttas.GetString((int)id),
                                        ID = (byte)id,
                                        Entry = entry,
                                        Global = false
                                    });
                                }
                            }
                        }
                        foreach (var item in pie)
                        {
                            item.Score = attenScore;
                            item.Callee = obj;
                            validActions.Add(item);
                        }
                    }
                    //TODO: Lockout interactions that have been used before for a few sim hours (in ts1 ticks. same # of ticks for tso probably)
                    //TODO: special logic for socials?
                }
            }

            var sorted = validActions.OrderBy(x => -x.Score).ToList();
            var selection = sorted.FirstOrDefault();
            if (selection == null) return VMPrimitiveExitCode.GOTO_FALSE;
            if (!selection.Entry.AutoFirst)
            {
                selection = sorted[(int)context.VM.Context.NextRandom((ulong)Math.Min(4, sorted.Count))];
            }

            var qaction = selection.Callee.GetAction(selection.ID, context.Caller, context.VM.Context, false, new short[] { selection.Param0, 0, 0, 0 });
            if (qaction != null)
            {
                qaction.Priority = (short)VMQueuePriority.Autonomous;
                context.Caller.Thread.EnqueueAction(qaction);
            }
            return VMPrimitiveExitCode.GOTO_TRUE;
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
