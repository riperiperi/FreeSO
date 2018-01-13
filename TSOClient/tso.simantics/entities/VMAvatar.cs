/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.LotView.Components;
using FSO.Vitaboy;
using FSO.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FSO.SimAntics.Model;
using FSO.LotView.Model;
using FSO.Files.Formats.IFF.Chunks;
using FSO.Common.Utils;
using FSO.SimAntics.Model.Routing;
using FSO.HIT;
using FSO.SimAntics.NetPlay.Model;
using System.IO;
using FSO.SimAntics.Marshals;
using FSO.SimAntics.Entities;
using FSO.SimAntics.Model.TSOPlatform;
using FSO.SimAntics.Model.Sound;
using FSO.SimAntics.Engine;
using FSO.SimAntics.Primitives;

namespace FSO.SimAntics
{
    public class VMAvatar : VMEntity
    {
        public static uint TEMPLATE_PERSON = 0x7FD96B54;

        public SimAvatar Avatar;

        /** Animation vars **/

        public List<VMAnimationState> Animations;
        public VMAnimationState CurrentAnimationState
        {
            get
            {
                return (Animations.Count == 0) ? null : Animations[0];
            }
        }
        public VMAnimationState CarryAnimationState;

        private string m_Message = "";
        public string Message
        {
            get { return m_Message; }
            set
            {
                m_Message = value;
                SetPersonData(VMPersonDataVariable.ChatBaloonOn, 1);
                MessageTimeout = 150 + value.Length / 2;
            }
        }

        public int MessageTimeout;
        public Vector3? VisualPositionStart;
        public Vector3 Velocity; //used for 60 fps walking animation
        public double TurnVelocity;

        private VMMotiveChange[] MotiveChanges = new VMMotiveChange[16];
        private VMIMotiveDecay MotiveDecay;
        private short[] PersonData = new short[101];
        private short[] MotiveData = new short[16];
        private VMEntity HandObject;
        private float _RadianDirection;

        public int KillTimeout = -1;
        private static readonly int FORCE_DELETE_TIMEOUT = 60 * 30;
        private readonly ushort LEAVE_LOT_TREE = 8373;

        /*
            APPEARANCE DATA
        */
        public VMAvatarDefaultSuits DefaultSuits = new VMAvatarDefaultSuits(false);
        public VMAvatarDynamicSuits DynamicSuits = new VMAvatarDynamicSuits(false);
        public VMAvatarDecoration Decoration = new VMAvatarDecoration();
        public HashSet<string> BoundAppearances = new HashSet<string>();

        private VMOutfitReference _BodyOutfit;
        public VMOutfitReference BodyOutfit
        {
            set
            {
                _BodyOutfit = value;
                if (UseWorld)
                {
                    Avatar.Body = value.GetContent();
                    if (AvatarType == VMAvatarType.Adult || AvatarType == VMAvatarType.Child) Avatar.Handgroup = Avatar.Body;
                }
            }
            get
            {
                return _BodyOutfit;
            }
        }

        private VMOutfitReference _HeadOutfit;
        public VMOutfitReference HeadOutfit
        {
            set
            {
                _HeadOutfit = value;
                if (UseWorld)
                {
                    Avatar.Head = value.GetContent();
                }
            }
            get
            {
                return _HeadOutfit;
            }
        }

        

        private AppearanceType _SkinTone;
        public AppearanceType SkinTone
        {
            set
            {
                _SkinTone = value;
                if (UseWorld) Avatar.Appearance = value;
            }
            get { return _SkinTone; }
        }

        public override Vector3 VisualPosition
        {
            get { return (UseWorld) ? ((AvatarComponent)WorldUI).StoredPosition : new Vector3(); }
            set { if (UseWorld) WorldUI.Position = value; }
        }
        public override float RadianDirection
        {
            get { return _RadianDirection; }
            set
            {
                _RadianDirection = value;
                if (UseWorld) ((AvatarComponent)WorldUI).RadianDirection = value;
            }
        }

        public override Direction Direction
        {
            get
            {
                int midPointDir = (int)DirectionUtils.PosMod(Math.Round(_RadianDirection / (Math.PI / 4f)), 8);
                return (Direction)(1 << (midPointDir));
            }
            set { RadianDirection = ((int)Math.Round(Math.Log((double)value, 2))) * (float)(Math.PI / 4.0); }
        }

        //inferred properties
        public string[] WalkAnimations = new string[50];
        public string[] SwimAnimations = new string[50];
        private STR BodyStrings;
        private VMAvatarType AvatarType;
        public override bool MovesOften
        {
            get
            {
                return true;
            }
        }

        public void SubmitHITVars(HIT.HITThread thread)
        {
            if (thread.ObjectVar == null) return;
            var age = GetPersonData(VMPersonDataVariable.PersonsAge);
            thread.ObjectVar[0] = (age < 18 && age != 0)?2:GetPersonData(VMPersonDataVariable.Gender);
            thread.ObjectVar[2] = Math.Min(100, GetPersonData(VMPersonDataVariable.CookingSkill)/10);
            thread.ObjectVar[5] = Math.Min(100, GetPersonData(VMPersonDataVariable.CreativitySkill)/10);
            //6 unknown
            thread.ObjectVar[7] = Math.Min(100, GetPersonData(VMPersonDataVariable.MechanicalSkill)/10);
            thread.ObjectVar[11] = Math.Min(100, GetPersonData(VMPersonDataVariable.BodySkill) / 10);

            thread.ObjectVar[14] = GetPersonData(VMPersonDataVariable.PersonsAge);
            thread.ObjectVar[15] = GetPersonData(VMPersonDataVariable.NicePersonality) / 10;
            thread.ObjectVar[16] = GetPersonData(VMPersonDataVariable.ActivePersonality) / 10;
            thread.ObjectVar[17] = GetPersonData(VMPersonDataVariable.GenerousPersonality) / 10;
            thread.ObjectVar[18] = GetPersonData(VMPersonDataVariable.PlayfulPersonality) / 10;
            thread.ObjectVar[19] = GetPersonData(VMPersonDataVariable.OutgoingPersonality) / 10;
            thread.ObjectVar[20] = GetPersonData(VMPersonDataVariable.NeatPersonality)/10;
        }

        public bool IsPet
        {
            get
            {
                var gender = GetPersonData(VMPersonDataVariable.Gender);
                return (gender & (8 | 16)) > 0; //flags are dog, cat.
            }
        }

        public bool IsDog
        {
            get
            {
                var gender = GetPersonData(VMPersonDataVariable.Gender);
                return (gender & 8) > 0;
            }
        }

        public bool IsCat
        {
            get
            {
                var gender = GetPersonData(VMPersonDataVariable.Gender);
                return (gender & 16) > 0; //flags are dog, cat.
            }
        }

        public VMAvatar(GameObject obj)
            : base(obj)
        {
            PlatformState = new VMTSOAvatarState(); //todo: ts1 switch
            BodyStrings = Object.Resource.Get<STR>(Object.OBJ.BodyStringID);

            SetAvatarType(BodyStrings);
            SkinTone = AppearanceType.Light;

            if (UseWorld)
            {
                WorldUI = new AvatarComponent();
                var avatarc = (AvatarComponent)WorldUI;
                avatarc.Avatar = Avatar;
                var type = BodyStrings?.GetString(0) ?? "adult";
                if (type != "adult" && type != "child") avatarc.IsPet = true;
            }


            MotiveDecay = (Content.Content.Get().TS1) ? (VMIMotiveDecay)new VMTS1MotiveDecay() : new VMAvatarMotiveDecay();
            for (int i = 0; i < 16; i++)
            {
                MotiveChanges[i] = new VMMotiveChange();
                MotiveChanges[i].Motive = (VMMotive)i;
            }
        }

        public void SetAvatarType(STR data)
        {
            if (data == null)
            {
                AvatarType = VMAvatarType.Adult;
            }
            else
            {
                var type = data.GetString(0);
                if (type == "adult") AvatarType = VMAvatarType.Adult;
                else if (type == "child") AvatarType = VMAvatarType.Child;
                else if (type == "cat" || type == "kat") AvatarType = VMAvatarType.Cat;
                else if (type == "dog") AvatarType = VMAvatarType.Dog;
            }

            Avatar = new SimAvatar(FSO.Content.Content.Get().AvatarSkeletons.Get((data?.GetString(0)??"adult")+".skel"));
            if (UseWorld && !FSO.Content.Content.Get().TS1)
            {
                switch (AvatarType)
                {
                    case VMAvatarType.Adult:
                        Avatar.Head = FSO.Content.Content.Get().AvatarOutfits.Get(0x000003a00000000D); //default to bob newbie, why not
                        Avatar.Body = FSO.Content.Content.Get().AvatarOutfits.Get("mab002_slob.oft");
                        Avatar.Handgroup = Avatar.Body;
                        break;
                    case VMAvatarType.Cat:
                        Avatar.Body = FSO.Content.Content.Get().AvatarOutfits.Get("uaa002cat_calico.oft");
                        break;
                    case VMAvatarType.Dog:
                        Avatar.Body = FSO.Content.Content.Get().AvatarOutfits.Get("uaa012dog_scottish.oft"); //;)
                        break;
                    case VMAvatarType.Child:
                        break;
                }
            }
        }

        public void SetAvatarBodyStrings(STR data, VMContext context)
        {
            if (data == null) return;

            var skinTone = data.GetString(14);
            if (skinTone.Equals("lgt", StringComparison.InvariantCultureIgnoreCase) || context.VM.TS1) SkinTone = AppearanceType.Light;
            else if (skinTone.Equals("med", StringComparison.InvariantCultureIgnoreCase)) SkinTone = AppearanceType.Medium;
            else if (skinTone.Equals("drk", StringComparison.InvariantCultureIgnoreCase)) SkinTone = AppearanceType.Dark;

            try
            {
                if (context.VM.TS1) {
                    DefaultSuits.Daywear = new VMOutfitReference(data, false);
                    HeadOutfit = new VMOutfitReference(data, true);
                    BodyOutfit = DefaultSuits.Daywear;
                } else { 
                    var body = data.GetString(1);
                    var randBody = data.GetString(10);

                    if (randBody != "")
                    {
                        var bodySpl = randBody.Split(';').Where(x => !string.IsNullOrEmpty(x)).ToArray();
                        DefaultSuits.Daywear = VMOutfitReference.Parse(bodySpl[context.NextRandom((ulong)bodySpl.Length)], context.VM.TS1);
                    }
                    else if (body != "")
                    {
                        DefaultSuits.Daywear = VMOutfitReference.Parse(body, context.VM.TS1);
                    }

                    BodyOutfit = DefaultSuits.Daywear;

                    var head = data.GetString(2);
                    var randHead = data.GetString(9);

                    if (randHead != "")
                    {
                        var headSpl = randHead.Split(';').Where(x => !string.IsNullOrEmpty(x)).ToArray();
                        HeadOutfit = VMOutfitReference.Parse(headSpl[context.NextRandom((ulong)headSpl.Length)], context.VM.TS1);
                    }
                    else if (head != "")
                    {
                        HeadOutfit = VMOutfitReference.Parse(head, context.VM.TS1);
                    }
                }
            }
            catch
            {
                //head or body invalid, resort to default.
            }

            var gender = data.GetString(12);
            var genVar = (int)VMPersonDataVariable.Gender;

            if (gender.Equals("male", StringComparison.InvariantCultureIgnoreCase)) PersonData[genVar] = 0;
            else if (gender.Equals("female", StringComparison.InvariantCultureIgnoreCase)) PersonData[genVar] = 1;
            else if (gender.Equals("dogmale", StringComparison.InvariantCultureIgnoreCase)) PersonData[genVar] = 8;
            else if (gender.Equals("dogfemale", StringComparison.InvariantCultureIgnoreCase)) PersonData[genVar] = 9;
            else if (gender.Equals("catmale", StringComparison.InvariantCultureIgnoreCase)) PersonData[genVar] = 16;
            else if (gender.Equals("catfemale", StringComparison.InvariantCultureIgnoreCase)) PersonData[genVar] = 17;

            var names = data.GetString(11);
            if (names != "")
            {
                var nameSpl = names.Split(';').Where(x => !string.IsNullOrEmpty(x)).ToArray();
                Name = nameSpl[context.NextRandom((ulong)nameSpl.Length)];
            }

            PersonData[(int)VMPersonDataVariable.PersonsAge] = Convert.ToInt16(data.GetString(13));
        }

        public void InitBodyData(VMContext context)
        {
            //init walking strings
            var age = PersonData[(int)VMPersonDataVariable.PersonsAge];
            var child = (age == 0 || age < 18) && context.VM.TS1;
            var GlobWalk = context.Globals.Resource.Get<STR>((ushort)(child?151:150));
            for (int i = 0; i < GlobWalk.Length; i++)
            {
                WalkAnimations[i] = GlobWalk.GetString(i);
            }

            var GlobSwim = context.Globals.Resource.Get<STR>((ushort)(child?160:158));
            for (int i = 0; i < GlobSwim.Length; i++)
            {
                SwimAnimations[i] = GlobSwim.GetString(i);
            }

            var SpecialWalk = Object.Resource.Get<STR>(150);
            if (SpecialWalk != null)
            {
                for (int i = 0; i < SpecialWalk.Length; i++)
                {
                    var str = SpecialWalk.GetString(i);
                    if (str != "") WalkAnimations[i] = str;
                }
            }
        }

        public override void Init(VMContext context)
        {
            if (UseWorld)
            {
                WorldUI.ObjectID = ObjectID;
                ((AvatarComponent)WorldUI).blueprint = context.Blueprint;
            }
            base.Init(context);

            Animations = new List<VMAnimationState>();

            SetAvatarBodyStrings(Object.Resource.Get<STR>(Object.OBJ.BodyStringID), context);
            InitBodyData(context);

            for (int i = 0; i < MotiveData.Length; i++)
            {
                MotiveData[i] = 100;
            }

            SetMotiveData(VMMotive.SleepState, 0); //max all motives except sleep state

            SetPersonData(VMPersonDataVariable.NeatPersonality, 1000); //for testing wash hands after toilet
        }

        public override void Reset(VMContext context)
        {
            base.Reset(context);
            if (Animations != null) Animations.Clear();
            if (Headline != null)
            {
                HeadlineRenderer?.Dispose();
                Headline = null;
                HeadlineRenderer = null;
            }
            if (VM.UseWorld)
            {
                foreach (var aprName in BoundAppearances)
                {
                    //remove all appearances, so we don't have stuff stuck to us.
                    var apr = FSO.Content.Content.Get().AvatarAppearances.Get(aprName);
                    Avatar.RemoveAccessory(apr);
                }
            }
            BoundAppearances.Clear();
            if (context.VM.EODHost != null) context.VM.EODHost.ForceDisconnect(this);
        }

        private void HandleTimePropsEvent(TimePropertyListItem tp)
        {
            VMAvatar avatar = this;
            var evt = tp.Properties["xevt"];
            if (evt != null)
            {
                var eventValue = short.Parse(evt);
                avatar.CurrentAnimationState.EventQueue.Add(eventValue);
                if (eventValue < 100) avatar.CurrentAnimationState.EventsRun++;
            }
            var rhevt = tp.Properties["righthand"];
            if (rhevt != null)
            {
                var eventValue = short.Parse(rhevt);
                avatar.Avatar.RightHandGesture = (SimHandGesture)eventValue;
            }
            var lhevt = tp.Properties["lefthand"];
            if (lhevt != null)
            {
                var eventValue = short.Parse(lhevt);
                avatar.Avatar.LeftHandGesture = (SimHandGesture)eventValue;
            }

            var soundevt = tp.Properties["sound"];
            var owner = this;
            if (UseWorld && soundevt != null && owner.SoundThreads.FirstOrDefault(x => x.Name == soundevt) == null)
            {
                var thread = FSO.HIT.HITVM.Get().PlaySoundEvent(soundevt);
                if (thread != null)
                {

                    if (thread is HITThread) SubmitHITVars((HITThread)thread);

                    if (!thread.AlreadyOwns(owner.ObjectID)) thread.AddOwner(owner.ObjectID);

                    var entry = new VMSoundEntry()
                    {
                        Sound = thread,
                        Pan = true,
                        Zoom = true,
                    };
                    owner.SoundThreads.Add(entry);
                    owner.TickSounds();
                }
            }
        }

        public override void Tick()
        {
            Velocity = new Vector3(0, 0, 0);
            TurnVelocity = 0;
            VisualPositionStart = null;
            base.Tick();

            if (Message != "")
            {
                if (MessageTimeout-- > 0)
                {
                    if (MessageTimeout == 0)
                    {
                        SetPersonData(VMPersonDataVariable.ChatBaloonOn, 0);
                        m_Message = "";
                    }
                }
            }

            if (Thread != null && Thread.ThreadBreak == Engine.VMThreadBreakMode.Pause) return;

            if (PersonData.Length > (int)VMPersonDataVariable.OnlineJobStatusFlags && PersonData[(int)VMPersonDataVariable.OnlineJobStatusFlags] == 0) PersonData[(int)VMPersonDataVariable.OnlineJobStatusFlags] = 1;
            if (Thread != null)
            {
                MotiveDecay.Tick(this, Thread.Context);
                if (Position == LotTilePos.OUT_OF_WORLD)
                {
                    //uh oh!
                    var mailbox = Thread.Context.VM.Entities.FirstOrDefault(x => (x.Object.OBJ.GUID == 0xEF121974 || x.Object.OBJ.GUID == 0x1D95C9B0));
                    if (mailbox != null) VMFindLocationFor.FindLocationFor(this, mailbox, Thread.Context, VMPlaceRequestFlags.Default);
                }
            }

            //animation update for avatars
            VMAvatar avatar = this;
            float totalWeight = 0f;
            foreach (var state in Animations)
            {
                totalWeight += state.Weight;
                if (!state.EndReached && state.Weight != 0)
                {
                    if (state.PlayingBackwards) state.CurrentFrame -= state.Speed;
                    else state.CurrentFrame += state.Speed;
                    var currentFrame = state.CurrentFrame;
                    var currentTime = (currentFrame * 1000) / 30;
                    var timeProps = state.TimePropertyLists;
                    if (!state.PlayingBackwards)
                    {
                        for (var i = 0; i < timeProps.Count; i++)
                        {
                            var tp = timeProps[i];
                            if (tp.ID > currentTime)
                            {
                                break;
                            }

                            timeProps.RemoveAt(0);
                            i--;

                            HandleTimePropsEvent(tp);
                        }
                    }
                    else
                    {
                        for (var i = timeProps.Count - 1; i >= 0; i--)
                        {
                            var tp = timeProps[i];
                            if (tp.ID < currentTime)
                            {
                                break;
                            }

                            timeProps.RemoveAt(timeProps.Count - 1);
                            HandleTimePropsEvent(tp);
                        }
                    }
                }
                var status = //(VM.UseWorld) ? Animator.RenderFrame(avatar.Avatar, state.Anim, (int)state.CurrentFrame, state.CurrentFrame % 1f, state.Weight / totalWeight) :
                                Animator.SilentFrameProgress(avatar.Avatar, state.Anim, (int)state.CurrentFrame);
                if (status != AnimationStatus.IN_PROGRESS)
                {
                    if (state.Loop)
                    {
                        if (state.PlayingBackwards) state.CurrentFrame += state.Anim.NumFrames;
                        else state.CurrentFrame -= state.Anim.NumFrames;
                    }
                    else
                        state.EndReached = true;
                }
            }

            if (avatar.CarryAnimationState != null)
            {
                var status = //(VM.UseWorld) ? Animator.RenderFrame(avatar.Avatar, avatar.CarryAnimationState.Anim, (int)avatar.CarryAnimationState.CurrentFrame, 0.0f, 1f)
                    //: 
                    Animator.SilentFrameProgress(avatar.Avatar, avatar.CarryAnimationState.Anim, (int)avatar.CarryAnimationState.CurrentFrame); //currently don't advance frames... I don't think any of them are animated anyways.
            }

            for (int i = 0; i < 16; i++)
            {
                MotiveChanges[i].Tick(this); //tick over motive changes
            }

            PersonData[(int)VMPersonDataVariable.TickCounter]++;
            if (KillTimeout > -1)
            {
                if (++KillTimeout > FORCE_DELETE_TIMEOUT)
                {
                    if (Thread.Context.VM.EODHost != null) Thread.Context.VM.EODHost.ForceDisconnect(this); //it's unsalvagable!
                    Delete(true, Thread.Context);
                }
                else
                {
                    SetPersonData(VMPersonDataVariable.RenderDisplayFlags, 1);
                    SetMotiveData(VMMotive.SleepState, 0);
                    SetValue(VMStackObjectVariable.Hidden, (short)((KillTimeout % 30) / 15));
                    if (Thread.BlockingState != null) Thread.BlockingState.WaitTime = Math.Max(Thread.BlockingState.WaitTime, 1000000); //make most things time out
                    UserLeaveLot(); //keep forcing
                }
            }
        }

        public void UserLeaveLot()
        {
            //interaction cancel should handle this
            //if (Thread.Context.VM.EODHost != null) Thread.Context.VM.EODHost.ForceDisconnect(this); //try this a lot.
            if (Thread.Queue.Exists(x => x.ActionRoutine.ID == LEAVE_LOT_TREE && Thread.Queue.IndexOf(x) <= Thread.ActiveQueueBlock+1)) return; //we're already leaving
            var actions = new List<VMQueuedAction>(Thread.Queue);
            foreach (var action in actions)
            {
                Thread.CancelAction(action.UID);
            }

            var tree = GetBHAVWithOwner(LEAVE_LOT_TREE, Thread.Context);
            var routine = Thread.Context.VM.Assemble(tree.bhav);

            Thread.EnqueueAction(
                new FSO.SimAntics.Engine.VMQueuedAction
                {
                    Callee = this,
                    CodeOwner = tree.owner,
                    ActionRoutine = routine,
                    Name = "Leave Lot",
                    StackObject = this,
                    Args = new short[4],
                    InteractionNumber = -1,
                    Priority = short.MaxValue,
                    Flags = TTABFlags.MustRun
                }
            );
            if (KillTimeout == -1) KillTimeout = 0;
        }

        public void FractionalAnim(float fraction)
        {
            var avatar = (VMAvatar)this;
            float totalWeight = 0f;
            foreach (var state in Animations)
            {
                totalWeight += state.Weight;
                if (!state.EndReached)
                {
                    float visualFrame = state.CurrentFrame;
                    if (state.PlayingBackwards) visualFrame -= state.Speed * fraction;
                    else visualFrame += state.Speed * fraction;

                    Animator.RenderFrame(avatar.Avatar, state.Anim, (int)visualFrame, visualFrame % 1, state.Weight / totalWeight);
                }
            }
            if (avatar.CarryAnimationState != null) Animator.RenderFrame(avatar.Avatar, avatar.CarryAnimationState.Anim, (int)avatar.CarryAnimationState.CurrentFrame, 0.0f, 1f);

            //TODO: if this gets changed to run at variable framerate need to "remember" visual position
            avatar.Avatar.ReloadSkeleton();
            if (VisualPositionStart != null) VisualPosition = VisualPositionStart.Value + fraction * Velocity;
            if (UseWorld) ((AvatarComponent)WorldUI).RadianDirection = (double)(_RadianDirection - TurnVelocity * fraction);
        }

        public virtual short GetPersonData(VMPersonDataVariable variable)
        {
            if ((ushort)variable > 100) throw new Exception("Person Data out of bounds!");
            VMTSOJobInfo jobInfo = null;
            switch (variable)
            {
                case VMPersonDataVariable.OnlineJobGrade:
                    if (((VMTSOAvatarState)TSOState).JobInfo.TryGetValue(GetPersonData(VMPersonDataVariable.OnlineJobID), out jobInfo))
                        return jobInfo.Level;
                    return 0;
                case VMPersonDataVariable.OnlineJobSickDays:
                    if (((VMTSOAvatarState)TSOState).JobInfo.TryGetValue(GetPersonData(VMPersonDataVariable.OnlineJobID), out jobInfo))
                        return jobInfo.SickDays;
                    return 0;
                case VMPersonDataVariable.OnlineJobStatusFlags:
                    if (((VMTSOAvatarState)TSOState).JobInfo.TryGetValue(GetPersonData(VMPersonDataVariable.OnlineJobID), out jobInfo))
                        return jobInfo.StatusFlags;
                    return 0;
                case VMPersonDataVariable.OnlineJobXP:
                    if (((VMTSOAvatarState)TSOState).JobInfo.TryGetValue(GetPersonData(VMPersonDataVariable.OnlineJobID), out jobInfo))
                        return jobInfo.Experience;
                    return 0;
                case VMPersonDataVariable.Priority:
                    return (Thread.Queue.Count == 0) ? (short)0 : Thread.Queue[0].Priority;
                case VMPersonDataVariable.IsHousemate:
                    var level = ((VMTSOAvatarState)TSOState).Permissions;
                    return (short)((level >= VMTSOAvatarPermissions.BuildBuyRoommate) ? 2 : ((level >= VMTSOAvatarPermissions.Roommate) ? 1 : 0));
                case VMPersonDataVariable.NumOutgoingFriends:
                case VMPersonDataVariable.IncomingFriends:
                    if (Thread?.Context?.VM?.TS1 == true) break;
                    return (short)(MeToPersist.Count(x => x.Key < 16777216 && x.Value.Count > 1 && x.Value[1] >= 60));
                case VMPersonDataVariable.SkillLock:
                    // this variable contains a bitmask of skills which should not decay. our skill disable system sets them all,
                    // but perhaps in the original they were used by events
                    if (Thread == null) return 0;
                    return (short)(SkillGameplayDisabled(Thread.Context.VM)?0x7FFF:0);
            }
            return PersonData[(ushort)variable];
        }

        public bool ForceEnableSkill;
        public bool SkillGameplayDisabled(VM vm)
        {
            if (ForceEnableSkill || PersistID == 0) return false;
            var mode = vm.TSOState.SkillMode;
            if (mode == 0) return false;
            else if (mode == 1)
                return ((VMTSOAvatarState)TSOState).Permissions == VMTSOAvatarPermissions.Visitor;
            else return true;
        }

        public virtual void SetMotiveChange(VMMotive motive, short PerHourChange, short MaxValue)
        {
            var temp = MotiveChanges[(int)motive];
            if (temp.Ticked)
            {
                temp.PerHourChange = PerHourChange;
                temp.MaxValue = MaxValue;
                temp.Ticked = false;
            }
        }

        public bool HasMotiveChange(VMMotive motive)
        {
            return MotiveChanges[(int)motive].PerHourChange != 0;
        }

        public virtual void ClearMotiveChanges()
        {
            for (int i = 0; i < 16; i++)
            {
                MotiveChanges[i].Clear();
            }
        }

        public short SkillLocks
        {
            get
            {
                return PersonData[70];
            }
            set
            {
                PersonData[70] = value;
            }
        }

        public virtual bool SetPersonData(VMPersonDataVariable variable, short value)
        {
            if ((ushort)variable > 100) throw new Exception("Person Data out of bounds!");
            VMTSOJobInfo jobInfo = null;
            switch (variable)
            {
                case VMPersonDataVariable.TS1ScalingSim:
                    if (Thread?.Context?.VM?.TS1 == true && VM.UseWorld)
                        ((AvatarComponent)WorldUI).Scale = value / 100f;
                    break;
                case VMPersonDataVariable.OnlineJobID:
                    if (value > 4) return false;
                    if (!((VMTSOAvatarState)TSOState).JobInfo.ContainsKey(value))
                    {
                        ((VMTSOAvatarState)TSOState).JobInfo[value] = new VMTSOJobInfo();
                        ((VMTSOAvatarState)TSOState).JobInfo[value].StatusFlags = 1;
                    }
                    break;
                case VMPersonDataVariable.OnlineJobGrade:
                    if (((VMTSOAvatarState)TSOState).JobInfo.TryGetValue(GetPersonData(VMPersonDataVariable.OnlineJobID), out jobInfo))
                        jobInfo.Level = value;
                    return true;
                case VMPersonDataVariable.OnlineJobSickDays:
                    if (((VMTSOAvatarState)TSOState).JobInfo.TryGetValue(GetPersonData(VMPersonDataVariable.OnlineJobID), out jobInfo))
                        jobInfo.SickDays = value;
                    return true;
                case VMPersonDataVariable.OnlineJobStatusFlags:
                    if (((VMTSOAvatarState)TSOState).JobInfo.TryGetValue(GetPersonData(VMPersonDataVariable.OnlineJobID), out jobInfo))
                        jobInfo.StatusFlags = value;
                    return true;
                case VMPersonDataVariable.OnlineJobXP:
                    if (((VMTSOAvatarState)TSOState).JobInfo.TryGetValue(GetPersonData(VMPersonDataVariable.OnlineJobID), out jobInfo))
                    {
                        var diff = value - jobInfo.Experience;
                        jobInfo.Experience = value;
                    }
                    return true;
                case VMPersonDataVariable.Priority:
                    if (Thread.Queue.Count != 0 && Thread.Stack.LastOrDefault().ActionTree)
                    {
                        Thread.Queue[0].Priority = value;
                        Thread.QueueDirty = true;
                    }
                    return true;
                case VMPersonDataVariable.MoneyAmmountOverHead:
                    if (value != -32768) ShowMoneyHeadline(value);
                    break;
                case VMPersonDataVariable.RenderDisplayFlags:
                    if (WorldUI != null) ((AvatarComponent)WorldUI).DisplayFlags = (AvatarDisplayFlags)value;
                    return true;
                case VMPersonDataVariable.SkillLock:
                    return true;
                case VMPersonDataVariable.IsGhost:
                    if (WorldUI != null) ((AvatarComponent)WorldUI).IsDead = value > 0;
                    break;
                case VMPersonDataVariable.BodySkill:
                case VMPersonDataVariable.CharismaSkill:
                case VMPersonDataVariable.CookingSkill:
                case VMPersonDataVariable.CreativitySkill:
                case VMPersonDataVariable.LogicSkill:
                case VMPersonDataVariable.MechanicalSkill:
                    if (Thread != null && SkillGameplayDisabled(Thread.Context.VM)) return true;
                    break;
            }
            PersonData[(ushort)variable] = value;
            return true;
        }

        public void InheritNeighbor(Neighbour neigh)
        {
            var lastGender = GetPersonData(VMPersonDataVariable.Gender);
            if (neigh.PersonData != null) PersonData = neigh.PersonData.ToArray();
            SetPersonData(VMPersonDataVariable.Gender, lastGender); //fixes cats switching to children suddenly
            SetPersonData(VMPersonDataVariable.NeighborId, neigh.NeighbourID);
        }

        public virtual short GetMotiveData(VMMotive variable) //needs special conditions for ones like Mood.
        {
            if ((ushort)variable > 15) throw new Exception("Motive Data out of bounds!");
            return MotiveData[(ushort)variable];
        }

        public virtual bool SetMotiveData(VMMotive variable, short value)
        {
            if ((ushort)variable > 15) throw new Exception("Motive Data out of bounds!");
            var old = MotiveData[(ushort)variable];
            //this run on garbage to access motive overfill is a bit stupid. Refrencing the tuning cache from each avatar might get stupid, though.
            MotiveData[(ushort)variable] = (short)Math.Max(Math.Min(value, Math.Max((int)old, Thread?.Context?.VM?.TuningCache?.GetLimit(variable) ?? 100)), -100);
            return true;
        }


        public void ReplaceMotiveData(short[] dat)
        {
            MotiveData = dat;
        }

        public override VMObstacle GetObstacle(LotTilePos pos, Direction dir)
        {
            return (KillTimeout > -1 && !GetFlag(VMEntityFlags.HasZeroExtent)) ? null :
                new VMObstacle(
                (pos.x - 3),
                (pos.y - 3),
                (pos.x + 3),
                (pos.y + 3));
        }

        public void ShowMoneyHeadline(int value)
        {
            if (HeadlineRenderer != null) HeadlineRenderer.Dispose();

            //(int)(headline.Operand.Flags2 | (ushort)(headline.Operand.Duration << 16));
            uint uval = (uint)value;
            Headline = new VMRuntimeHeadline(new VMSetBalloonHeadlineOperand
            {
                Group = VMSetBalloonHeadlineOperandGroup.Money,
                Flags2 = (ushort)(uval),
                Duration = (short)(uval >> 16)
            }, this, null, 0);
            Headline.Duration = 60;
            HeadlineRenderer = Thread?.Context.VM.Headline.Get(Headline);
        }

        public override void PositionChange(VMContext context, bool noEntryPoint)
        {
            if (GhostImage) return;

            var room = context.GetObjectRoom(this);
            SetRoom(room);

            if (HandObject != null)
            {
                context.UnregisterObjectPos(HandObject);
                HandObject.Position = Position;
                HandObject.PositionChange(context, noEntryPoint);
            }

            context.RegisterObjectPos(this);
            if (Container != null) return;
            if (Position == LotTilePos.OUT_OF_WORLD) return;

            base.PositionChange(context, noEntryPoint);
        }

        public override void PrePositionChange(VMContext context)
        {
            Footprint = null;
            if (GhostImage && UseWorld)
            {
                if (WorldUI.Container != null)
                {
                    WorldUI.Container = null;
                    WorldUI.ContainerSlot = 0;
                }
                return;
            }

            context.UnregisterObjectPos(this);
            if (Container != null)
            {
                Container.ClearSlot(ContainerSlot);
                return;
            }
            if (Position == LotTilePos.OUT_OF_WORLD) return;
            base.PrePositionChange(context);
        }

        // Begin Container SLOTs interface

        public override int TotalSlots()
        {
            return 1;
        }

        public override bool PlaceInSlot(VMEntity obj, int slot, bool cleanOld, VMContext context)
        {
            if (GetSlot(slot) == obj) return true; //already in slot
            if (GetSlot(slot) != null || obj.Dead) return false;
            if (cleanOld) obj.PrePositionChange(context);

            if (!obj.GhostImage)
            {
                HandObject = obj;

                CarryAnimationState = new VMAnimationState(FSO.Content.Content.Get().AvatarAnimations.Get("a2o-rarm-carry-loop.anim"), false); //set default carry animation

                obj.Container = this;
                obj.ContainerSlot = (short)slot;
            }
            if (UseWorld)
            {
                obj.WorldUI.Container = this.WorldUI;
                obj.WorldUI.ContainerSlot = slot;
                if (obj.WorldUI is ObjectComponent)
                {
                    var objC = (ObjectComponent)obj.WorldUI;
                    objC.ForceDynamic = true;
                }
            }
            obj.Position = Position; //TODO: is physical position the same as the slot offset position?
            if (cleanOld) obj.PositionChange(context, false);
            return true;
        }

        public override int GetSlotHeight(int slot)
        {
            return 5; //in hand
            //TODO: verify
        }

        public override VMEntity GetSlot(int slot)
        {
            return HandObject;
        }

        public override void ClearSlot(int slot)
        {
            HandObject.Container = null;
            HandObject.ContainerSlot = -1;
            CarryAnimationState = null;

            if (UseWorld)
            {
                HandObject.WorldUI.Container = null;
                HandObject.WorldUI.ContainerSlot = 0;

                if (HandObject.WorldUI is ObjectComponent)
                {
                    var objC = (ObjectComponent)HandObject.WorldUI;
                    objC.ForceDynamic = false;
                }
            }

            HandObject = null;
        }

        // End Container SLOTs interface

        public override void SetRoom(ushort room)
        {
            base.SetRoom(room);
            if (VM.UseWorld) WorldUI.Room = (ushort)GetValue(VMStackObjectVariable.Room);
        }

        public override Texture2D GetIcon(GraphicsDevice gd, int store)
        {
            if (Avatar.Head == null && Avatar.Body == null) return null;
            var content = FSO.Content.Content.Get();
            if (content.TS1) return null;
            Outfit ThumbOutfit = (Avatar.Head == null) ? Avatar.Body : Avatar.Head;
            var AppearanceID = ThumbOutfit.GetAppearance(Avatar.Appearance);
            var Appearance = content.AvatarAppearances.Get(AppearanceID);

            if (Appearance == null) return null;
            var ico = FSO.Content.Content.Get().AvatarThumbnails.Get(Appearance.ThumbnailTypeID, Appearance.ThumbnailFileID)?.Get(gd);

            //todo: better dispose handling for these icons
            return (store > 0 && ico != null)?TextureUtils.Decimate(ico, gd, 1<<(2-store), false):ico;
        }

        #region VM Marshalling Functions
        public VMAvatarMarshal Save()
        {
            var anims = new VMAnimationStateMarshal[Animations.Count];
            int i = 0;
            foreach (var anim in Animations) anims[i++] = anim.Save();
            var gameObj = new VMAvatarMarshal
            {
                Animations = anims,
                CarryAnimationState = (CarryAnimationState == null) ? null : CarryAnimationState.Save(), //NULLable

                Message = Message,

                MessageTimeout = MessageTimeout,

                MotiveChanges = MotiveChanges,
                MotiveDecay = MotiveDecay,
                PersonData = PersonData,
                MotiveData = MotiveData,
                HandObject = (HandObject == null) ? (short)0 : HandObject.ObjectID,
                RadianDirection = RadianDirection,
                KillTimeout = KillTimeout,
                DefaultSuits = DefaultSuits,
                DynamicSuits = DynamicSuits,
                Decoration = Decoration,

                BoundAppearances = BoundAppearances.ToArray(),
                BodyOutfit = BodyOutfit,
                HeadOutfit = HeadOutfit,
                SkinTone = SkinTone
            };
            SaveEnt(gameObj);
            return gameObj;
        }

        public virtual void Load(VMAvatarMarshal input)
        {
            base.Load(input);

            Animations = new List<VMAnimationState>();
            foreach (var anim in input.Animations) Animations.Add(new VMAnimationState(anim));
            CarryAnimationState = (input.CarryAnimationState == null) ? null : new VMAnimationState(input.CarryAnimationState);

            Message = input.Message;

            MessageTimeout = input.MessageTimeout;

            MotiveChanges = input.MotiveChanges;
            MotiveDecay = input.MotiveDecay;
            PersonData = input.PersonData;
            MotiveData = input.MotiveData;
            RadianDirection = input.RadianDirection;
            KillTimeout = input.KillTimeout;
            DefaultSuits = input.DefaultSuits;
            DynamicSuits = input.DynamicSuits;
            Decoration = input.Decoration;

            BoundAppearances = new HashSet<string>(input.BoundAppearances);

            if (VM.UseWorld)
            {
                foreach (var aprN in BoundAppearances)
                {
                    var apr = FSO.Content.Content.Get().AvatarAppearances.Get(aprN);
                    if (apr != null) Avatar.AddAccessory(apr);
                }

                var oftProvider = Content.Content.Get().AvatarOutfits;
                if (oftProvider != null) { 
                    if (Decoration.Back != 0) Avatar.DecorationBack = oftProvider.Get(Decoration.Back);
                    if (Decoration.Head != 0) Avatar.DecorationHead = oftProvider.Get(Decoration.Head);
                    if (Decoration.Tail != 0) Avatar.DecorationTail = oftProvider.Get(Decoration.Tail);
                    if (Decoration.Shoes != 0) Avatar.DecorationShoes = oftProvider.Get(Decoration.Shoes);
                }
            }

            SkinTone = input.SkinTone;

            if (UseWorld)
            {
                WorldUI.ObjectID = ObjectID;
            }
        }

        public virtual void LoadCrossRef(VMAvatarMarshal input, VMContext context)
        {
            base.LoadCrossRef(input, context);
            HandObject = context.VM.GetObjectById(input.HandObject);
            if (HandObject != null && HandObject is VMGameObject) ((ObjectComponent)HandObject.WorldUI).ForceDynamic = true;
            //we need to fix the gender, since InitBodyData resets it.
            var gender = GetPersonData(VMPersonDataVariable.Gender);
            InitBodyData(context);
            SetPersonData(VMPersonDataVariable.Gender, gender);
            SetPersonData(VMPersonDataVariable.RenderDisplayFlags, GetPersonData(VMPersonDataVariable.RenderDisplayFlags));
            SetPersonData(VMPersonDataVariable.IsGhost, GetPersonData(VMPersonDataVariable.IsGhost));
            BodyOutfit = input.BodyOutfit;
            HeadOutfit = input.HeadOutfit;
            if (UseWorld) ((AvatarComponent)WorldUI).blueprint = context.Blueprint;
        }
        #endregion
    }

    public enum VMAvatarType : byte
    {
        Adult,
        Child,
        Cat,
        Dog
    }

    public class VMAvatarDefaultSuits : VMSerializable
    {
        public VMOutfitReference Daywear;
        public VMOutfitReference Swimwear;
        public VMOutfitReference Sleepwear;

        public VMAvatarDefaultSuits(bool female)
        {
            Daywear = new VMOutfitReference(0x24C0000000D);
            Swimwear = new VMOutfitReference((ulong)((female) ? 0x620000000D : 0x5470000000D));
            Sleepwear = new VMOutfitReference((ulong)((female) ? 0x5150000000D : 0x5440000000D));
        }

        public VMAvatarDefaultSuits(BinaryReader reader)
        {
            Deserialize(reader);
        }

        public void SerializeInto(BinaryWriter writer)
        {
            Daywear.SerializeInto(writer);
            Swimwear.SerializeInto(writer);
            Sleepwear.SerializeInto(writer);
        }

        public void Deserialize(BinaryReader reader)
        {
            Daywear = new VMOutfitReference(reader);
            Swimwear = new VMOutfitReference(reader);
            Sleepwear = new VMOutfitReference(reader);
        }
    }

    public class VMAvatarDynamicSuits : VMSerializable
    {
        public ulong Daywear;
        public ulong Swimwear;
        public ulong Sleepwear;
        public ulong Costume;

        public VMAvatarDynamicSuits(bool female)
        {
            Daywear = 0x24C0000000D;
            Swimwear = (ulong)((female) ? 0x620000000D : 0x5470000000D);
            Sleepwear = (ulong)((female) ? 0x5150000000D : 0x5440000000D);
            Costume = Daywear;
        }

        public VMAvatarDynamicSuits(BinaryReader reader)
        {
            Deserialize(reader);
        }

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(Daywear);
            writer.Write(Swimwear);
            writer.Write(Sleepwear);
            writer.Write(Costume);
        }

        public void Deserialize(BinaryReader reader)
        {
            Daywear = reader.ReadUInt64();
            Swimwear = reader.ReadUInt64();
            Sleepwear = reader.ReadUInt64();
            Costume = reader.ReadUInt64();
        }
    }

    public class VMAvatarDecoration : VMSerializable
    {
        public ulong Head;
        public ulong Back;
        public ulong Shoes;
        public ulong Tail;

        public VMAvatarDecoration()
        {
        }

        public VMAvatarDecoration(BinaryReader reader)
        {
            Deserialize(reader);
        }

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(Head);
            writer.Write(Back);
            writer.Write(Shoes);
            writer.Write(Tail);
        }

        public void Deserialize(BinaryReader reader)
        {
            Head = reader.ReadUInt64();
            Back = reader.ReadUInt64();
            Shoes = reader.ReadUInt64();
            Tail = reader.ReadUInt64();
        }
    }
}
