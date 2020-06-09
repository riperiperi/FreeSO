using FSO.SimAntics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Common.Rendering.Framework.Model;
using FSO.SimAntics.Model;
using FSO.Vitaboy;
using FSO.SimAntics.Engine.Scopes;

namespace FSO.IDE.Common
{
    public class UIAvatarAnimator : UIInteractiveDGRP
    {
        private Queue<UIAvatarAnimatorRequest> AnimationRequests = new Queue<UIAvatarAnimatorRequest>();
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
                    var request = AnimationRequests.Dequeue();

                    switch (request.Type)
                    {
                        case UIAvatarAnimatorRequestType.Animation:
                            var anim = request.Obj as string;
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
                                        ava.Avatar.Skeleton = skels.Get("adult.skel").Clone();
                                        ava.Avatar.Head = FSO.Content.Content.Get().AvatarOutfits.Get(0x000003a00000000D);
                                        ava.Avatar.Body = ofts.Get("mab002_slob.oft");
                                        ava.Avatar.Handgroup = ava.Avatar.Body;
                                        break;
                                    case 'c':
                                        ava.Avatar.Skeleton = skels.Get("child.skel").Clone();
                                        break;
                                    case 'd':
                                        ava.Avatar.Skeleton = skels.Get("dog.skel").Clone();
                                        ava.Avatar.Body = ofts.Get("uaa012dog_scottish.oft");

                                        break;
                                    case 'k':
                                        ava.Avatar.Skeleton = skels.Get("cat.skel").Clone();
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
                                astate.Speed = 30 / 25f;
                                astate.Loop = true;
                                ava.Animations.Clear();
                                ava.Animations.Add(astate);
                            }
                            break;
                        case UIAvatarAnimatorRequestType.Outfit:
                            var tuple = request.Obj as Tuple<VMPersonSuits, Outfit>;
                            var oft = tuple.Item2;
                            switch (tuple.Item1)
                            {
                                case VMPersonSuits.DefaultDaywear:
                                    ava.Avatar.Body = oft;
                                    ava.Avatar.Handgroup = oft;
                                    break;
                                case VMPersonSuits.Head:
                                    ava.Avatar.Head = oft;
                                    break;
                                case VMPersonSuits.DecorationBack:
                                    if (ava.Avatar.DecorationBack == oft) oft = null;
                                    ava.Avatar.DecorationBack = oft;
                                    break;
                                case VMPersonSuits.DecorationHead:
                                    if (ava.Avatar.DecorationHead == oft) oft = null;
                                    ava.Avatar.DecorationHead = oft;
                                    break;
                                case VMPersonSuits.DecorationTail:
                                    if (ava.Avatar.DecorationTail == oft) oft = null;
                                    ava.Avatar.DecorationTail = oft;
                                    break;
                                case VMPersonSuits.DecorationShoes:
                                    if (ava.Avatar.DecorationShoes == oft) oft = null;
                                    ava.Avatar.DecorationShoes = oft;
                                    break;
                            }
                            break;

                        case UIAvatarAnimatorRequestType.AddAccessory:
                            {
                                var dress = request.Obj as String + ".apr";
                                var apr = FSO.Content.Content.Get().AvatarAppearances.Get(dress);
                                ava.BoundAppearances.Add(dress);
                                ava.Avatar.AddAccessory(apr);
                                break;
                            }
                        case UIAvatarAnimatorRequestType.RemoveAccessory:
                            {
                                var undress = request.Obj as String + ".apr";
                                var apr = FSO.Content.Content.Get().AvatarAppearances.Get(undress);
                                ava.BoundAppearances.Remove(undress);
                                ava.Avatar.RemoveAccessory(apr);
                                break;
                            }
                        case UIAvatarAnimatorRequestType.ClearAccessories:
                            foreach (var aprN in ava.BoundAppearances)
                            {
                                var apr = FSO.Content.Content.Get().AvatarAppearances.Get(aprN);
                                if (apr != null) ava.Avatar.RemoveAccessory(apr);
                            }
                            break;
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
                AnimationRequests.Enqueue(new UIAvatarAnimatorRequest(UIAvatarAnimatorRequestType.Animation, anim));
            }
        }

        public void BindOutfit(VMPersonSuits type, Outfit oft)
        {
            lock (AnimationRequests)
            {
                AnimationRequests.Enqueue(new UIAvatarAnimatorRequest(UIAvatarAnimatorRequestType.Outfit, new Tuple<VMPersonSuits, Outfit>(type, oft)));
            }
        }

        public void AddAccessory(string dress)
        {
            lock (AnimationRequests)
            {
                AnimationRequests.Enqueue(new UIAvatarAnimatorRequest(UIAvatarAnimatorRequestType.AddAccessory, dress));
            }
        }

        public void RemoveAccessory(string undress)
        {
            lock (AnimationRequests)
            {
                AnimationRequests.Enqueue(new UIAvatarAnimatorRequest(UIAvatarAnimatorRequestType.RemoveAccessory, undress));
            }
        }

        public void ClearAccessories()
        {
            lock (AnimationRequests)
            {
                AnimationRequests.Enqueue(new UIAvatarAnimatorRequest(UIAvatarAnimatorRequestType.ClearAccessories, null));
            }
        }
    }

    public enum UIAvatarAnimatorRequestType
    {
        Animation,
        Outfit,
        AddAccessory,
        RemoveAccessory,
        ClearAccessories
    }

    public class UIAvatarAnimatorRequest
    {
        public UIAvatarAnimatorRequestType Type;
        public object Obj;

        public UIAvatarAnimatorRequest(UIAvatarAnimatorRequestType type, object obj)
        {
            Type = type;
            Obj = obj;
        }
    }
}
