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
        public VMAnimationState CurrentAnimationState {
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
        public Vector3 Velocity; //used for 60 fps walking animation

        private VMMotiveChange[] MotiveChanges = new VMMotiveChange[16];
        private VMAvatarMotiveDecay MotiveDecay;
        private short[] PersonData = new short[100];
        private short[] MotiveData = new short[16];
        private VMEntity HandObject;
        private float _RadianDirection;

        private int KillTimeout = -1;
        private static readonly int FORCE_DELETE_TIMEOUT = 60 * 30;
        private readonly ushort LEAVE_LOT_TREE = 8373;

        /*
            APPEARANCE DATA
        */
        public VMAvatarDefaultSuits DefaultSuits = new VMAvatarDefaultSuits(false);
        public HashSet<string> BoundAppearances = new HashSet<string>();

        private ulong _BodyOutfit;
        public ulong BodyOutfit
        {
            set
            {
                _BodyOutfit = value;
                Avatar.Body = FSO.Content.Content.Get().AvatarOutfits.Get(value);
                if (AvatarType == VMAvatarType.Adult || AvatarType == VMAvatarType.Child) Avatar.Handgroup = Avatar.Body;
            }
            get
            {
                return _BodyOutfit;
            }
        }

        private ulong _HeadOutfit;
        public ulong HeadOutfit
        {
            set
            {
                _HeadOutfit = value;
                Avatar.Head = (_HeadOutfit == 0)?null:FSO.Content.Content.Get().AvatarOutfits.Get(value);
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
                Avatar.Appearance = value;
            }
            get { return _SkinTone; }
        }

        public override Vector3 VisualPosition
        {
            get { return (UseWorld) ? WorldUI.Position : new Vector3(); }
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

        public void SubmitHITVars(HIT.HITThread thread)
        {
            if (thread.ObjectVar == null) return;
            thread.ObjectVar[12] = GetPersonData(VMPersonDataVariable.Gender);
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
            }


            MotiveDecay = new VMAvatarMotiveDecay();
            for (int i = 0; i < 16; i++)
            {
                MotiveChanges[i] = new VMMotiveChange();
                MotiveChanges[i].Motive = (VMMotive)i;
            }
        }

        public void SetAvatarType(STR data) {
            if (data == null)
            {
                AvatarType = VMAvatarType.Adult;
            }
            else
            {
                var type = data.GetString(0);
                if (type == "adult") AvatarType = VMAvatarType.Adult;
                else if (type == "child") AvatarType = VMAvatarType.Child;
                else if (type == "cat") AvatarType = VMAvatarType.Cat;
                else if (type == "dog") AvatarType = VMAvatarType.Dog;
            }

            switch (AvatarType)
            {
                case VMAvatarType.Adult:
                    Avatar = new SimAvatar(FSO.Content.Content.Get().AvatarSkeletons.Get("adult.skel"));
                    Avatar.Head = FSO.Content.Content.Get().AvatarOutfits.Get(0x000003a00000000D); //default to bob newbie, why not
                    Avatar.Body = FSO.Content.Content.Get().AvatarOutfits.Get("mab002_slob.oft");
                    Avatar.Handgroup = Avatar.Body;
                    break;
                case VMAvatarType.Cat:
                    var skel = FSO.Content.Content.Get().AvatarSkeletons.Get("cat.skel");
                    Avatar = new SimAvatar(skel);
                    Avatar.Body = FSO.Content.Content.Get().AvatarOutfits.Get("uaa002cat_calico.oft");
                    break;
                case VMAvatarType.Dog:
                    Avatar = new SimAvatar(FSO.Content.Content.Get().AvatarSkeletons.Get("dog.skel"));
                    Avatar.Body = FSO.Content.Content.Get().AvatarOutfits.Get("uaa012dog_scottish.oft"); //;)
                    break;
            }
        }

        public void SetAvatarBodyStrings(STR data, VMContext context) {
            if (data == null) return;

            try
            {
                var body = data.GetString(1);
                var randBody = data.GetString(10);

                if (randBody != "")
                {
                    var bodySpl = randBody.Split(';');
                    DefaultSuits.Daywear = Convert.ToUInt64(bodySpl[context.NextRandom((ulong)bodySpl.Length-1)], 16);
                }
                else if (body != "")
                {
                    DefaultSuits.Daywear = Convert.ToUInt64(body, 16);
                }

                BodyOutfit = DefaultSuits.Daywear;

                var head = data.GetString(2);
                var randHead = data.GetString(9);

                if (randHead != "")
                {
                    var headSpl = randHead.Split(';');
                    HeadOutfit = Convert.ToUInt64(headSpl[context.NextRandom((ulong)headSpl.Length-1)], 16);
                }
                else if (head != "")
                {
                    HeadOutfit = Convert.ToUInt64(head, 16);
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
                var nameSpl = names.Split(';');
                Name = nameSpl[context.NextRandom((ulong)nameSpl.Length)];
            }

            PersonData[(int)VMPersonDataVariable.PersonsAge] = Convert.ToInt16(data.GetString(13));

            var skinTone = data.GetString(14);
            if (skinTone.Equals("lgt", StringComparison.InvariantCultureIgnoreCase)) SkinTone = AppearanceType.Light;
            else if (skinTone.Equals("med", StringComparison.InvariantCultureIgnoreCase)) SkinTone = AppearanceType.Medium;
            else if (skinTone.Equals("drk", StringComparison.InvariantCultureIgnoreCase)) SkinTone = AppearanceType.Dark;
        }

        public void InitBodyData(VMContext context)
        {
            //init walking strings
            var GlobWalk = context.Globals.Resource.Get<STR>(150);
            for (int i = 0; i < GlobWalk.Length; i++)
            {
                WalkAnimations[i] = GlobWalk.GetString(i);
            }

            var GlobSwim = context.Globals.Resource.Get<STR>(158);
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
            if (UseWorld) WorldUI.ObjectID = ObjectID;
            base.Init(context);

            Animations = new List<VMAnimationState>();

            SetAvatarBodyStrings(Object.Resource.Get<STR>(Object.OBJ.BodyStringID), context);
            InitBodyData(context);

            for (int i=0; i<MotiveData.Length; i++)
            {
                MotiveData[i] = 75;
            }

            SetMotiveData(VMMotive.SleepState, 0); //max all motives except sleep state

            SetPersonData(VMPersonDataVariable.NeatPersonality, 1000); //for testing wash hands after toilet
            SetPersonData(VMPersonDataVariable.OnlineJobID, 1); //for testing wash hands after toilet

            SetPersonData(VMPersonDataVariable.CreativitySkill, 1000);
            SetPersonData(VMPersonDataVariable.CookingSkill, 1000);
            SetPersonData(VMPersonDataVariable.CharismaSkill, 1000);
            SetPersonData(VMPersonDataVariable.LogicSkill, 1000);
            SetPersonData(VMPersonDataVariable.BodySkill, 1000);

            SetPersonData(VMPersonDataVariable.NumOutgoingFriends, 100);
            SetPersonData(VMPersonDataVariable.IncomingFriends, 100);
        }

        public override void Reset(VMContext context)
        {
            base.Reset(context);
            if (Animations != null) Animations.Clear();
            if (Headline != null)
            {
                HeadlineRenderer.Dispose();
                Headline = null;
                HeadlineRenderer = null;
            }
            foreach (var aprName in BoundAppearances)
            {
                //remove all appearances, so we don't have stuff stuck to us.
                var apr = FSO.Content.Content.Get().AvatarAppearances.Get(aprName);
                Avatar.RemoveAccessory(apr);
            }
            BoundAppearances.Clear();
        }

        private void HandleTimePropsEvent(TimePropertyListItem tp)
        {
            VMAvatar avatar = this;
            var evt = tp.Properties["xevt"];
            if (evt != null)
            {
                var eventValue = short.Parse(evt);
                avatar.CurrentAnimationState.EventQueue.Add(eventValue);
                avatar.CurrentAnimationState.EventsRun++;
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
            if (UseWorld && soundevt != null)
            {
                var thread = FSO.HIT.HITVM.Get().PlaySoundEvent(soundevt);
                if (thread != null)
                {
                    var owner = this;
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

            if (PersonData[(int)VMPersonDataVariable.OnlineJobStatusFlags] == 0) PersonData[(int)VMPersonDataVariable.OnlineJobStatusFlags] = 1;
            if (Thread != null)
            {
                MotiveDecay.Tick(this, Thread.Context);
                SetPersonData(VMPersonDataVariable.OnlineJobGrade, Math.Max((short)0, Thread.Context.VM.GetGlobalValue(11))); //force job grade to what we expect
                if (Position == LotTilePos.OUT_OF_WORLD)
                {
                    //uh oh!
                    var mailbox = Thread.Context.VM.Entities.FirstOrDefault(x => (x.Object.OBJ.GUID == 0xEF121974 || x.Object.OBJ.GUID == 0x1D95C9B0));
                    if (mailbox != null) VMFindLocationFor.FindLocationFor(this, mailbox, Thread.Context);
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
                    var currentTime = currentFrame * 33.33f;
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

                    var status = Animator.RenderFrame(avatar.Avatar, state.Anim, (int)state.CurrentFrame, state.CurrentFrame%1f, state.Weight/totalWeight);
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
            }

            if (avatar.CarryAnimationState != null)
            {
                var status = Animator.RenderFrame(avatar.Avatar, avatar.CarryAnimationState.Anim, (int)avatar.CarryAnimationState.CurrentFrame, 0.0f, 1f); //currently don't advance frames... I don't think any of them are animated anyways.
            }

            for (int i = 0; i < 16; i++)
            {
                MotiveChanges[i].Tick(this); //tick over motive changes
            }

            avatar.Avatar.ReloadSkeleton();

            PersonData[(int)VMPersonDataVariable.TickCounter]++;
            if (KillTimeout > -1)
            {
                if (++KillTimeout > FORCE_DELETE_TIMEOUT) Delete(true, Thread.Context);
                else
                {
                    SetPersonData(VMPersonDataVariable.RenderDisplayFlags, 1);
                    SetValue(VMStackObjectVariable.Hidden, (short)((KillTimeout % 30) / 15));
                    if (Thread.BlockingState != null) Thread.BlockingState.WaitTime = Math.Max(Thread.BlockingState.WaitTime, 1000000); //make most things time out
                    UserLeaveLot(); //keep forcing
                }
            }
        }

        public void UserLeaveLot()
        {
            if (Thread.Queue.Exists(x => x.ActionRoutine.ID == LEAVE_LOT_TREE && Thread.Queue.IndexOf(x) < 2)) return; //we're already leaving
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
                    Flags = TTABFlags.Leapfrog | TTABFlags.MustRun
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
                    if (state.PlayingBackwards) visualFrame -= state.Speed/2;
                    else visualFrame += state.Speed/2;

                    Animator.RenderFrame(avatar.Avatar, state.Anim, (int)visualFrame, visualFrame%1, state.Weight/totalWeight);
                }
            }
            if (avatar.CarryAnimationState != null) Animator.RenderFrame(avatar.Avatar, avatar.CarryAnimationState.Anim, (int)avatar.CarryAnimationState.CurrentFrame, 0.0f, 1f);

            //TODO: if this gets changed to run at variable framerate need to "remember" visual position
            avatar.Avatar.ReloadSkeleton();
            VisualPosition += fraction * Velocity;
        }

        public virtual short GetPersonData(VMPersonDataVariable variable)
        {
            if ((ushort)variable > 100) throw new Exception("Person Data out of bounds!");
            switch (variable)
            {
                case VMPersonDataVariable.Priority:
                    return (Thread.Queue.Count == 0) ? (short)0 : Thread.Queue[0].Priority;
                case VMPersonDataVariable.IsHousemate:
                    var level = ((VMTSOAvatarState)TSOState).Permissions;
                    return (short)((level >= VMTSOAvatarPermissions.BuildBuyRoommate)?2:((level >= VMTSOAvatarPermissions.Roommate)?1:0));
            }
            return PersonData[(ushort)variable];
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

        public virtual void ClearMotiveChanges()
        {
            for (int i = 0; i < 16; i++)
            {
                MotiveChanges[i].Clear();
            }
        }

        public virtual bool SetPersonData(VMPersonDataVariable variable, short value)
            {
            if ((ushort)variable > 100) throw new Exception("Person Data out of bounds!");
            switch (variable)
            {
                case VMPersonDataVariable.Priority:
                    if (Thread.Queue.Count != 0 && Thread.Stack.LastOrDefault().ActionTree) Thread.Queue[0].Priority = value;
                    return true;
                case VMPersonDataVariable.RenderDisplayFlags:
                    if (WorldUI != null) ((AvatarComponent)WorldUI).DisplayFlags = (AvatarDisplayFlags)value;
                    return true;
            }
            PersonData[(ushort)variable] = value;
            return true;
        }

        public virtual short GetMotiveData(VMMotive variable) //needs special conditions for ones like Mood.
        {
            if ((ushort)variable > 15) throw new Exception("Motive Data out of bounds!");
            return MotiveData[(ushort)variable];
        }

        public virtual bool SetMotiveData(VMMotive variable, short value)
        {
            if ((ushort)variable > 15) throw new Exception("Motive Data out of bounds!");
            MotiveData[(ushort)variable] = (short)Math.Max(Math.Min((int)value, 100), -100);
            return true;
        }

        public override string ToString()
        {
            return Name;
        }

        public override VMObstacle GetObstacle(LotTilePos pos, Direction dir)
        {
            return new VMObstacle(
                (pos.x - 3),
                (pos.y - 3),
                (pos.x + 3),
                (pos.y + 3));
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
            if (GetSlot(slot) != null) return false;
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
            Outfit ThumbOutfit = (Avatar.Head == null) ? Avatar.Body : Avatar.Head;
            var AppearanceID = ThumbOutfit.GetAppearance(Avatar.Appearance);
            var Appearance = FSO.Content.Content.Get().AvatarAppearances.Get(AppearanceID);

            return FSO.Content.Content.Get().AvatarThumbnails.Get(Appearance.ThumbnailTypeID, Appearance.ThumbnailFileID);
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

            BoundAppearances = new HashSet<string>(input.BoundAppearances);

            foreach (var aprN in BoundAppearances)
            {
                var apr = FSO.Content.Content.Get().AvatarAppearances.Get(aprN);
                Avatar.AddAccessory(apr);
            }

            SkinTone = input.SkinTone;

            if (UseWorld) WorldUI.ObjectID = ObjectID;
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
            BodyOutfit = input.BodyOutfit;
            HeadOutfit = input.HeadOutfit;
        }
        #endregion
    }

    public enum VMAvatarType : byte {
        Adult,
        Child,
        Cat,
        Dog
    }

    public class VMAvatarDefaultSuits : VMSerializable
    {
        public ulong Daywear;
        public ulong Swimwear;
        public ulong Sleepwear;

        public VMAvatarDefaultSuits(bool female)
        {
            Daywear = 0x24C0000000D;
            Swimwear = (ulong)((female) ? 0x620000000D : 0x5470000000D);
            Sleepwear = (ulong)((female) ? 0x5150000000D : 0x5440000000D);
        }

        public VMAvatarDefaultSuits(BinaryReader reader)
        {
            Deserialize(reader);
        }

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(Daywear);
            writer.Write(Swimwear);
            writer.Write(Sleepwear);
        }

        public void Deserialize(BinaryReader reader)
        {
            Daywear = reader.ReadUInt64();
            Swimwear = reader.ReadUInt64();
            Sleepwear = reader.ReadUInt64();
        }
    }
}
