/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Vitaboy;
using FSO.SimAntics.Utils;
using FSO.SimAntics.Marshals;

namespace FSO.SimAntics.Model
{
    public class VMAnimationState {
        public Animation Anim;
        public float CurrentFrame;
        public List<short> EventQueue = new List<short>();
        public byte EventsRun; //total # of xevts fired during anim. up to short if anything uses more than 255.
        public bool EndReached;
        public bool PlayingBackwards;
        public float Speed = 1.0f;
        public float Weight = 1.0f; //For animation blending. All active animations should add up to 1 but won't break if it doesn't.
        public bool Loop = false;
        public List<TimePropertyListItem> TimePropertyLists = new List<TimePropertyListItem>();

        public VMAnimationState(Animation animation, bool backwards)
        {
            Anim = animation;

            if (backwards)
            {
                PlayingBackwards = true;
                CurrentFrame = Anim.NumFrames;
            }

            foreach (var motion in animation.Motions)
            {
                if (motion.TimeProperties == null) { continue; }

                foreach (var tp in motion.TimeProperties)
                {
                    foreach (var item in tp.Items)
                    {
                        TimePropertyLists.Add(item);
                    }
                }
            }

            /** Sort time property lists by time **/
            TimePropertyLists.Sort(new TimePropertyListItemSorter());
        }

        #region VM Marshalling Functions
        public VMAnimationStateMarshal Save()
        {
            return new VMAnimationStateMarshal
            {
                Anim = Anim.Name,
                CurrentFrame = CurrentFrame,
                EventQueue = EventQueue.ToArray(),
                EventsRun = EventsRun,
                EndReached = EndReached,
                PlayingBackwards = PlayingBackwards,
                Speed = Speed,
                Weight = Weight,
                Loop = Loop
            };
        }

        public virtual void Load(VMAnimationStateMarshal input)
        {
            Anim = FSO.Content.Content.Get().AvatarAnimations.Get(input.Anim + ".anim");
            CurrentFrame = input.CurrentFrame;
            EventQueue = new List<short>(input.EventQueue);
            EventsRun = input.EventsRun;
            EndReached = input.EndReached;
            PlayingBackwards = input.PlayingBackwards;
            Speed = input.Speed;
            Weight = input.Weight;
            Loop = input.Loop;
        }

        public VMAnimationState(VMAnimationStateMarshal input)
        {
            Load(input);
        }
        #endregion
    }
}
