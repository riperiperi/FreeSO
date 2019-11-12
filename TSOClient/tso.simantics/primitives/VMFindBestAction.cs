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
using FSO.SimAntics.Engine.Utils;

namespace FSO.SimAntics.Primitives
{
    // Finds a suitable action and queues it onto this sim. Used for pet free will.

    // How autonomy works in The Sims:
    // 
    // Each interaction has "motive advertisements" which determine how many autonomy points are given for each interaction.
    //
    // Advertisements show the Minimum and Maximum points that should be awarded for each motive. 
    // Zero motives are ignored. Interactions with ALL-ZERO attenuation are never considered for autonomy.
    // The Maximum is awarded when the motive is at -100. The Minimum is awarded when the motive is at 100 (full)
    // "Vary By Personality" controls how the autonomy score varies with the sim personality. Likely a 0-1 multiplication based on the personality percent out of max?
    // Attenuation subtracts from the score multiplied by distance, but the object can still be used with negative score.
    // 
    // Each interaction has an "autonomy threshold", which determines when the action becomes much more important to perform.
    // If one or more actions are above their autonomy threshold, they are ordered by how much above the threshold they are and a random one from the top 4 is chosen.
    // If an interaction has "Auto first-select" active, then it is instantly chosen if it is the top of the list, with no random component.
    //
    // Autonomy threshold is not a strict cap - if the autonomy score is below it, using the object will now happen less frequently. 
    // (TS1 v1.0 seems to skip one or two automony, with a regular kind of period. what does complete collection do?)
    // I'm not sure how this exactly works - it may pick more than the top 4, but it is definitely a different category from being above the threshold.


    // 

    public class VMFindBestAction : VMPrimitiveHandler
    {
        public static VMPersonDataVariable[] VaryByTypes = new VMPersonDataVariable[]
        {
            VMPersonDataVariable.UnusedAndDoNotUse, //0
            VMPersonDataVariable.NicePersonality,
            VMPersonDataVariable.NicePersonality, //inverted
            VMPersonDataVariable.ActivePersonality,
            VMPersonDataVariable.ActivePersonality, //inverted
            VMPersonDataVariable.GenerousPersonality,
            VMPersonDataVariable.GenerousPersonality, //inverted
            VMPersonDataVariable.PlayfulPersonality,
            VMPersonDataVariable.PlayfulPersonality, //inverted
            VMPersonDataVariable.OutgoingPersonality,
            VMPersonDataVariable.OutgoingPersonality, //inverted
            VMPersonDataVariable.NeatPersonality,
            VMPersonDataVariable.NeatPersonality, //inverted


            VMPersonDataVariable.UnusedAndDoNotUse, // cleaning skill -- 13
            VMPersonDataVariable.CookingSkill, // cooking skill
            VMPersonDataVariable.CharismaSkill, // social skill *
            VMPersonDataVariable.MechanicalSkill, // repair skill *
            VMPersonDataVariable.UnusedAndDoNotUse, // gardening skill
            VMPersonDataVariable.UnusedAndDoNotUse, // music skill
            VMPersonDataVariable.CreativitySkill, // creativity skill
            VMPersonDataVariable.UnusedAndDoNotUse, // literacy skill
            VMPersonDataVariable.BodySkill, // physical skill *
            VMPersonDataVariable.LogicSkill // logic skill
        };

        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            //if we already have some action, do nothing.
            if (context.Caller.Thread.Queue.Any(x => x.Mode != VMQueueMode.Idle)) return VMPrimitiveExitCode.GOTO_TRUE;

            var ents = new List<VMEntity>(context.VM.Entities);
            var processed = new HashSet<short>();
            var caller = (VMAvatar)context.Caller;
            var pos1 = caller.Position;

            var attenTable = (caller.GetPersonData(VMPersonDataVariable.PersonType) == 1) ? TTAB.VisitorAttenuationValues : TTAB.AttenuationValues;

            List<VMPieMenuInteraction> validActions = new List<VMPieMenuInteraction>();
            List<VMPieMenuInteraction> betterActions = new List<VMPieMenuInteraction>();
            foreach (var iobj in ents)
            {
                if (iobj.Position == LotTilePos.OUT_OF_WORLD) continue;
                var obj = iobj.MultitileGroup.GetInteractionGroupLeader(iobj);
                if (processed.Contains(obj.ObjectID) || (obj is VMGameObject && ((VMGameObject)obj).Disabled > 0)) continue;
                processed.Add(obj.ObjectID);

                var pos2 = obj.Position;
                var distance = (short)Math.Floor(Math.Sqrt(Math.Pow(pos1.x - pos2.x, 2) + Math.Pow(pos1.y - pos2.y, 2) + Math.Pow((pos1.Level - pos2.Level) * 320, 2.0)) / 16.0);

                if (obj.TreeTable == null) continue;
                foreach (var entry in obj.TreeTable.Interactions)
                {
                    var advertisements = entry.MotiveEntries.Where(x => x.EffectRangeDelta + x.EffectRangeMinimum > 0).ToList(); //TODO: cache this
                    if (advertisements.Count == 0) continue; //no ads on this object.

                    var id = entry.TTAIndex;
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
                                if (actionS.Name == null) actionS.Name = ttas?.GetString((int)id) ?? "***MISSING***";
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
                                    Global = false,
                                });
                            }
                        }
                    }
                    var first = pie.FirstOrDefault();
                    if (first != null)
                    {
                        //calculate score for this tree
                        int score = 0;
                        for (int i = 0; i < entry.MotiveEntries.Length; i++)
                        {
                            var motiveScore = entry.MotiveEntries[i];
                            short min = motiveScore.EffectRangeMinimum;
                            short max = motiveScore.EffectRangeDelta;
                            short personality = (short)motiveScore.PersonalityModifier;
                            if (first.MotiveAdChanges != null)
                            {
                                first.MotiveAdChanges.TryGetValue((0 << 16) | i, out min);
                                first.MotiveAdChanges.TryGetValue((1 << 16) | i, out max);
                                first.MotiveAdChanges.TryGetValue((2 << 16) | i, out personality);
                            }

                            if (max == 0 && min > 0)
                            {
                                //invalid delta. do from 0..delta instead (fix child-talk preference?)
                                max = min;
                                min = 0;
                            }

                            max += min; //it's a delta, add min to it
                            if (max <= 0) continue;
                            //LINEAR INTERPOLATE MIN SCORE TO MAX, using motive data of caller
                            //MAX is when motive is the lowest.
                            //can be in reverse too!
                            var myMotive = caller.GetMotiveData((VMMotive)i);
                            int personalityMul = 1000;
                            if (personality != 0)
                            {
                                if (personality >= 0 && personality < VaryByTypes.Length)
                                {
                                    personalityMul = caller.GetPersonData(VaryByTypes[personality]);
                                    if (personality < 13 && (personality & 1) == 0)
                                    {
                                        personalityMul = 1000 - personalityMul;
                                    }
                                }
                            }

                            int motivePct = (myMotive + 100) / 2;
                            int interpScore = ((motivePct * min + (100 - motivePct) * max) * personalityMul) / (1000 * 100);
                            score += interpScore;
                        }

                        // modify score using attenuation
                        float atten = (entry.AttenuationCode == 0 || entry.AttenuationCode >= attenTable.Length) ? 
                            entry.AttenuationValue : attenTable[entry.AttenuationCode];
                        score = (int)Math.Max(0, score - (distance * atten));

                        foreach (var item in pie)
                        {
                            item.Score = score;
                            item.Callee = obj;
                            if (score > entry.AutonomyThreshold) betterActions.Add(first);
                            validActions.Add(first);
                        }
                    }
                    //if (attenScore != 0) attenScore += (int)context.VM.Context.NextRandom(31) - 15;
                    
                    //TODO: Lockout interactions that have been used before for a few sim hours (in ts1 ticks. same # of ticks for tso probably)
                    //TODO: special logic for socials?
                }
            }

            if (betterActions.Count != 0) validActions = betterActions;
            var sorted = validActions.OrderByDescending(x => x.Score).ToList();
            var selection = sorted.FirstOrDefault();
            if (selection == null) return VMPrimitiveExitCode.GOTO_FALSE;
            if (!selection.Entry.AutoFirst || validActions != betterActions)
            {
                if (selection.Score > 0 && sorted[Math.Min(4, sorted.Count) - 1].Score == 0)
                {
                    //prefer a non-zero score.
                    sorted = sorted.Where(x => x.Score > 0).ToList();
                }
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
