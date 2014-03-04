using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.world.components;
using TSO.Vitaboy;
using TSO.Content;
using Microsoft.Xna.Framework;
using TSO.Simantics.model;

namespace TSO.Simantics
{
    public class VMAvatar : VMEntity
    {
        public const uint TEMPLATE_PERSON = 0x7FD96B54;
        
        public AdultSimAvatar Avatar;

        /** Animation vars **/
        public Animation CurrentAnimation;
        public VMAnimationState CurrentAnimationState;

        private VMMotiveChange[] MotiveChanges = new VMMotiveChange[16];
        private short[] PersonData = new short[100];
        private short[] MotiveData = new short[16];

        public VMAvatar()
            : base(TSO.Content.Content.Get().WorldObjects.Get(TEMPLATE_PERSON))
        {
            WorldUI = new AvatarComponent();

            Avatar = new AdultSimAvatar();
            Avatar.Head = TSO.Content.Content.Get().AvatarOutfits.Get("mah108_apallo.oft");
            Avatar.Body = TSO.Content.Content.Get().AvatarOutfits.Get("mab011_lsexy.oft");

            var avatarc = (AvatarComponent)WorldUI;
            avatarc.Avatar = Avatar;

            for (int i = 0; i < 16; i++)
            {
                MotiveChanges[i] = new VMMotiveChange();
                MotiveChanges[i].Motive = (VMMotive)i;
            }
        }

        public override void Init(TSO.Simantics.VMContext context)
        {
            base.Init(context);
            var testa = 0;

            SetPersonData(VMPersonDataVariable.NeatPersonality, 1000); //for testing wash hands after toilet

            //also run the main function of all people because i'm a massochist
            //ExecuteEntryPoint(1, context);
        }

        public override void Tick()
        {
            base.Tick();
            //animation update for avatars
            VMAvatar avatar = this;
            if (avatar.CurrentAnimation != null && !avatar.CurrentAnimationState.EndReached)
            {
                avatar.CurrentAnimationState.CurrentFrame++;
                var currentFrame = avatar.CurrentAnimationState.CurrentFrame;
                var currentTime = currentFrame * 33.33f;
                var timeProps = avatar.CurrentAnimationState.TimePropertyLists;

                for (var i = 0; i < timeProps.Count; i++)
                {
                    var tp = timeProps[i];
                    if (tp.ID > currentTime)
                    {
                        break;
                    }

                    timeProps.RemoveAt(0);
                    i--;

                    var evt = tp.Properties["xevt"];
                    if (evt != null)
                    {
                        var eventValue = short.Parse(evt);
                        avatar.CurrentAnimationState.EventCode = eventValue;
                        avatar.CurrentAnimationState.EventFired = true;
                    }
                }

                var status = Animator.RenderFrame(avatar.Avatar, avatar.CurrentAnimation, avatar.CurrentAnimationState.CurrentFrame);
                if (status != AnimationStatus.IN_PROGRESS)
                {
                    avatar.CurrentAnimationState.EndReached = true;
                }
            }

            for (int i = 0; i < 16; i++)
            {
                MotiveChanges[i].Tick(this); //tick over motive changes
            }
        }

        public virtual short GetPersonData(VMPersonDataVariable variable)
        {
            /*switch (variable){
                case VMPersonDataVariable.UnusedAndDoNotUse:
                    return PersonData[(short)VMPersonDataVariable.UnusedAndDoNotUse];
            }
             - Will be reanabled later to deal with special cases where the value needs to be calculated on access.
             */
            if ((ushort)variable > 100) throw new Exception("Person Data out of bounds!");
            return PersonData[(ushort)variable];
            
        }

        public virtual void SetMotiveChange(VMMotive motive, short PerHourChange, short MaxValue)
        {
            var temp = MotiveChanges[(int)motive];
            temp.PerHourChange = PerHourChange;
            temp.MaxValue = MaxValue;
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
            /*switch (variable){
                case VMPersonDataVariable.UnusedAndDoNotUse:
                    PersonData[(short)VMPersonDataVariable.UnusedAndDoNotUse] = value;
                    return true;
            }*/
            if ((ushort)variable > 100) throw new Exception("Person Data out of bounds!");
            PersonData[(ushort)variable] = value;
            return true;
        }

        public virtual short GetMotiveData(VMMotive variable) //needs special conditions for ones like Mood.
        {
            switch (variable){
                case VMMotive.Mood:
                    return 50; //always happy!! really!! it's not a front :(
            }
            if ((ushort)variable > 15) throw new Exception("Motive Data out of bounds!");
            return MotiveData[(ushort)variable];
        }

        public virtual bool SetMotiveData(VMMotive variable, short value)
        {
            if ((ushort)variable > 15) throw new Exception("Motive Data out of bounds!");
            MotiveData[(ushort)variable] = value;
            return true;
        }

        public override string ToString()
        {
            return "Sim";
        }

        public Vector3 Position
        {
            get { return WorldUI.Position; }
            set { WorldUI.Position = value; }
        }
    }
}
