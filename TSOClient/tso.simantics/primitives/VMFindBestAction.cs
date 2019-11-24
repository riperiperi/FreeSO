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
    // First, each motive is converted into an "effective motive" using the interaction contribution curves.
    // These curves generally make motive changes more evident at lower values, and cap them at a specific value.
    // This prevents people from considering sleeping at 50% energy, but also allows other motives like fun to use a much higher cap.
    // "Happy" is calculated as an average of all "effective motive"s, including mood.
    // (In Hot Date and onwards this may be weighted by the Happy Weight curves)
    //
    // For each interaction, we only evaluate it if it has the following properties:
    // - non-zero motive range for any advertisement
    // - object is not "occupied"
    // - object has use count of 0 (this will be really slow in freeso so we will skip it for now, TODO)
    // - if any advertisements have a non-zero minimum, they are only considered if the motive is below that minimum (TODO: VERIFY)
    // Interaction check trees are evaluated to see if they are usable. "Auto" is passed to the tree as 1 in param 0, indicating this is an autonomous check.
    // When evaluating an interaction's check tree, it may change the motive ad range and minimum.
    // I don't know if anything changes from 0, but if it does then that would really suck for performance.
    //
    // For each interaction a new Happy score is calculated based on the motive deltas applied to the sim's current motives.
    // Motive delta is divided by 1000 for some reason. I don't really agree with this, but it matches numbers with TS 1.0. May be different in HD+.
    // Personality is applied by **some factor** based on your personality, if present. Likely 0-1 multiplier for personality, 0-2 for skill.
    // Remember to re-apply the interaction curves to the new motives here.Room for optimisation to only update the motives that changed.
    //
    // Attenuation is very simple: Score / (1 + (Attenuation* Distance)). It is applied to the *distance from base happy*, which results in the final score.
    // Individual motive scores can be negative to discourage use of interactions at high motives.These are combined with positives from other motives.
    //
    // Final scores below certain values are ignored:
    // 1E-07 for visitors and family, 1E-06 for sims who are sitting. These constants are in FCNS 2 in global.iff.
    // That means negative scores are ignored. Motive ads when you're in the flat part of the curve are ignored.
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

        public static VMMotive[] WeightMotives = new VMMotive[]
        {
            VMMotive.Energy,
            VMMotive.Comfort,
            VMMotive.Hunger,
            VMMotive.Hygiene,
            VMMotive.Bladder,
            VMMotive.Mood,
            VMMotive.Room,
            VMMotive.Social,
            VMMotive.Fun
        };

        public static int[] MotiveToWeight = Enumerable.Range(0, 16).Select(motive => Array.IndexOf(WeightMotives, (VMMotive)motive)).ToArray();

        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            //if we already have some action, do nothing.
            if (context.Caller.Thread.Queue.Any(x => x.Priority > (context.Caller as VMAvatar).GetPersonData(VMPersonDataVariable.Priority))) return VMPrimitiveExitCode.GOTO_TRUE;

            var ents = new List<VMEntity>(context.VM.Entities);
            var processed = new HashSet<short>();
            var caller = (VMAvatar)context.Caller;
            var pos1 = caller.Position;

            var visitor = (caller.GetPersonData(VMPersonDataVariable.PersonType) == 1);
            var child = (caller.IsChild && context.VM.TS1);
            var attenTable = visitor ? TTAB.VisitorAttenuationValues : TTAB.AttenuationValues;
            var global = Content.Content.Get().WorldObjectGlobals;
            var interactionCurve = child ? global.InteractionScoreChild : global.InteractionScore;
            var happyCurve = child ? global.HappyWeightChild : global.HappyWeight;

            var canUseIndoors = !caller.IsPet || !context.VM.TS1 || caller.GetPersonData(VMPersonDataVariable.GreetStatus) > 0 || caller.GetPersonData(VMPersonDataVariable.PersonType) != 1;

            // === HAPPY CALCULATION ===

            var newStyle = false;
            // TODO: new style
            var weights = newStyle ? new float[9] : new float[9] { 1, 1, 1, 1, 1, 1, 1, 1, 1 }; //TODO: weights from curves for new
            var totalWeight = weights.Sum();
            for (int i = 0; i < 9; i++) weights[i] /= totalWeight;
            var minScore = (caller.GetPersonData(VMPersonDataVariable.Posture) > 0) ? 1e-6 : 1e-7;

            var motives = WeightMotives.Select(x => caller.GetMotiveData(x));
            var happyParts = motives
                .Zip(interactionCurve, (motive, curve) => curve.GetPoint(motive))
                .Zip(weights, (motive, weight) => motive * weight)
                .ToArray();

            float baseHappy = happyParts.Sum();

            List<VMPieMenuInteraction> validActions = new List<VMPieMenuInteraction>();
            foreach (var iobj in ents)
            {
                if (iobj.Position == LotTilePos.OUT_OF_WORLD) continue;
                var obj = iobj.MultitileGroup.GetInteractionGroupLeader(iobj);
                if (processed.Contains(obj.ObjectID) || (obj is VMGameObject && ((VMGameObject)obj).Disabled > 0)) continue;
                processed.Add(obj.ObjectID);

                if (!canUseIndoors)
                {
                    //determine if the object is indoors
                    var roomID = context.VM.Context.GetObjectRoom(obj);
                    roomID = (ushort)Math.Max(0, Math.Min(context.VM.Context.RoomInfo.Length - 1, roomID));
                    var room = context.VM.Context.RoomInfo[roomID];
                    if (!room.Room.IsOutside) continue;
                }
                var pos2 = obj.Position;
                var distance = (float)Math.Sqrt(Math.Pow(pos1.x - pos2.x, 2) + Math.Pow(pos1.y - pos2.y, 2) + Math.Pow((pos1.Level - pos2.Level) * 320, 2.0)) / 16.0f;
                var inUse = obj.GetFlag(VMEntityFlags.Occupied);

                if (obj.TreeTable == null) continue;
                foreach (var entry in obj.TreeTable.AutoInteractions)
                {
                    var id = entry.TTAIndex;
                    if (inUse && !obj.TreeTable.Interactions.Any(x => x.JoiningIndex == id)) continue;
                    var advertisements = entry.ActiveMotiveEntries; //TODO: cache this
                    if (advertisements.Length == 0) continue; //no ads on this object.

                    var action = obj.GetAction((int)id, caller, context.VM.Context, false);
                    TTAs ttas = obj.TreeTableStrings;

                    caller.ObjectData[(int)VMStackObjectVariable.HideInteraction] = 0;
                    var actionStrings = caller.Thread.CheckAction(action, true);

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
                        // calculate score for this tree.
                        // start with the base happy value, and modify it for each motive changed.
                        float score = baseHappy;
                        for (int i = 0; i < advertisements.Length; i++)
                        {
                            var motiveI = advertisements[i].MotiveIndex;
                            var motiveScore = entry.MotiveEntries[motiveI];
                            short min = motiveScore.EffectRangeMinimum;
                            short max = motiveScore.EffectRangeDelta;
                            short personality = (short)motiveScore.PersonalityModifier;
                            if (first.MotiveAdChanges != null)
                            {
                                first.MotiveAdChanges.TryGetValue((0 << 16) | motiveI, out min);
                                first.MotiveAdChanges.TryGetValue((1 << 16) | motiveI, out max);
                                first.MotiveAdChanges.TryGetValue((2 << 16) | motiveI, out personality);
                            }

                            if (max == 0 && min > 0)
                            {
                                //invalid delta. do from 0..delta instead (fix child-talk preference?)
                                max = min;
                                min = 0;
                            }
                            max += min; //it's a delta, add min to it

                            var myMotive = caller.GetMotiveData((VMMotive)motiveI);
                            if (min != 0 && myMotive > min) continue;

                            // subtract the base contribution for this motive from happy
                            var weightInd = MotiveToWeight[motiveI];
                            if (weightInd == -1) continue;
                            score -= happyParts[weightInd];

                            float personalityMul = 1;
                            if (personality > 0 && personality < VaryByTypes.Length)
                            {
                                personalityMul = caller.GetPersonData(VaryByTypes[personality]);
                                personalityMul /= 1000f;
                                if (personality < 13)
                                {
                                    if ((personality & 1) == 0)
                                    {
                                        personalityMul = 1 - personalityMul;
                                    }
                                } else
                                {
                                    personalityMul *= 2;
                                }
                            }

                            // then add the new contribution for this motive.
                            score += interactionCurve[weightInd].GetPoint(myMotive + (max * personalityMul) / 1000f) * weights[weightInd];
                        }

                        // score relative to base
                        score -= baseHappy;
                        // modify score using attenuation
                        float atten = (entry.AttenuationCode == 0 || entry.AttenuationCode >= attenTable.Length) ?
                            entry.AttenuationValue : attenTable[entry.AttenuationCode];

                        score = score / (1 + atten * distance);

                        if (score > minScore)
                        {
                            foreach (var item in pie)
                            {
                                item.Score = score;
                                item.Callee = obj;
                                validActions.Add(first);
                            }
                        }
                    }
                    //if (attenScore != 0) attenScore += (int)context.VM.Context.NextRandom(31) - 15;
                    
                    //TODO: Lockout interactions that have been used before for a few sim hours (in ts1 ticks. same # of ticks for tso probably)
                    //TODO: special logic for socials?
                }
            }
            List<VMPieMenuInteraction> sorted = validActions.OrderByDescending(x => x.Score).ToList();
            sorted = TakeTopActions(sorted, 4);
            var selection = sorted.FirstOrDefault();
            if (selection == null) return VMPrimitiveExitCode.GOTO_FALSE;
            if (!selection.Entry.AutoFirst)
            {
                // weighted random selection
                //var slice = sorted.Take(Math.Min(4, sorted.Count)).ToList();
                var totalScore = sorted.Sum(x => x.Score);
                var random = context.VM.Context.NextRandom(10000);

                float randomTotal = 0;
                for (int i=0; i < sorted.Count; i++)
                {
                    var action = sorted[i];
                    randomTotal += (sorted[i].Score / totalScore) * 10000;
                    if (random <= randomTotal)
                    {
                        selection = action;
                        break;
                    }
                }
            }

            var qaction = selection.Callee.GetAction(selection.ID, context.Caller, context.VM.Context, false, new short[] { selection.Param0, 0, 0, 0 });
            if (qaction != null)
            {
                qaction.Priority = (short)VMQueuePriority.Autonomous;
                context.Caller.Thread.EnqueueAction(qaction);
            }
            return VMPrimitiveExitCode.GOTO_TRUE;
        }

        private List<VMPieMenuInteraction> TakeTopActions(List<VMPieMenuInteraction> list, int count)
        {
            var result = new List<VMPieMenuInteraction>();
            foreach (var action in list)
            {
                var users = action.Callee.GetValue(VMStackObjectVariable.UseCount);
                if (users == 0 || action.Callee.TreeTable.Interactions.Any(x => x.JoiningIndex == action.ID))
                {
                    result.Add(action);
                    if (result.Count >= count) break;
                }
            }
            return result;
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
