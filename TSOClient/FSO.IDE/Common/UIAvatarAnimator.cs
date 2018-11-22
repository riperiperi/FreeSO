using FSO.SimAntics;
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
        private char LastType = 'a';

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
                    var ava = ((VMAvatar)TargetTile);
                    var anim = AnimationRequests.Dequeue();

                    var skels = FSO.Content.Content.Get().AvatarSkeletons;
                    var ofts = FSO.Content.Content.Get().AvatarOutfits;
                    if (anim.Length == 0) continue;
                    if (LastType != anim[0])
                    {
                        ava.Avatar.Head = null;
                        ava.Avatar.Handgroup = null;
                        switch (anim[0])
                        {
                            case 'a':
                                ava.Avatar.Skeleton = skels.Get("adult.skel");
                                ava.Avatar.Head = FSO.Content.Content.Get().AvatarOutfits.Get(0x000003a00000000D);
                                ava.Avatar.Body = ofts.Get("mab002_slob.oft");
                                ava.Avatar.Handgroup = ava.Avatar.Body;
                                break;
                            case 'c':
                                ava.Avatar.Skeleton = skels.Get("child.skel");
                                break;
                            case 'd':
                                ava.Avatar.Skeleton = skels.Get("dog.skel");
                                ava.Avatar.Body = ofts.Get("uaa012dog_scottish.oft");
                                
                                break;
                            case 'k':
                                ava.Avatar.Skeleton = skels.Get("cat.skel");
                                ava.Avatar.Body = ofts.Get("uaa002cat_calico.oft");
                                break;
                        }
                        
                        ava.Avatar.Skeleton = ava.Avatar.Skeleton.Clone();
                        ava.Avatar.BaseSkeleton = ava.Avatar.Skeleton;

                        LastType = anim[0];
                    }

                    var animation = FSO.Content.Content.Get().AvatarAnimations.Get(anim + ".anim");
                    if (animation != null)
                    {
                        var astate = new VMAnimationState(animation, false);
                        astate.Speed = 30/25f;
                        astate.Loop = true;
                        ava.Animations.Clear();
                        ava.Animations.Add(astate);
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
