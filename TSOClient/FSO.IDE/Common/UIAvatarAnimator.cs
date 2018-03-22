﻿using FSO.SimAntics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Common.Rendering.Framework.Model;
using FSO.SimAntics.Model;

namespace FSO.IDE.Common
{
    public class UIAvatarAnimator : UIInteractiveDGRP
    {
        private Queue<string> AnimationRequests = new Queue<string>();

        public UIAvatarAnimator() : base(Content.Content.Get().TS1?
            (uint)Content.Content.Get().WorldObjects.Entries.FirstOrDefault(x => x.Value.Source == Content.GameObjectSource.User).Key
            : VMAvatar.TEMPLATE_PERSON)
        {

        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
            lock (AnimationRequests)
            {
                while (TargetTile != null && AnimationRequests.Count > 0)
                {
                    var anim = AnimationRequests.Dequeue();

                    var animation = FSO.Content.Content.Get().AvatarAnimations.Get(anim + ".anim");
                    if (animation != null)
                    {
                        var astate = new VMAnimationState(animation, false);
                        astate.Speed = 30/25f;
                        astate.Loop = true;
                        ((VMAvatar)TargetTile).Animations.Clear();
                        ((VMAvatar)TargetTile).Animations.Add(astate);
                    }
                }
                
            }

            state.SharedData["ExternalDraw"] = true;
            Invalidate();
        }

        public void SetAnimation(string anim)
        {
            lock (AnimationRequests)
            {
                AnimationRequests.Enqueue(anim);
            }
        }

    }
}
