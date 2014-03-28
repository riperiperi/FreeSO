using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.world.components;
using TSO.Vitaboy;
using TSO.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TSO.Simantics.model;
using tso.world.model;

namespace TSO.Simantics
{
    public class VMAvatar : VMEntity
    {
        public const uint TEMPLATE_PERSON = 0x7FD96B54;
        
        public AdultVitaboyModel Avatar;

        /** Animation vars **/
        public Animation CurrentAnimation;
        public VMAnimationState CurrentAnimationState;
        public Animation CarryAnimation;
        public VMAnimationState CarryAnimationState;

        private VMMotiveChange[] MotiveChanges = new VMMotiveChange[16];    
        private short[] PersonData = new short[100];
        private short[] MotiveData = new short[16];
        private VMEntity HandObject;

        public VMAvatar()
            : base(TSO.Content.Content.Get().WorldObjects.Get(TEMPLATE_PERSON))
        {
            WorldUI = new AvatarComponent();

            Avatar = new AdultVitaboyModel();
            Avatar.Head = TSO.Content.Content.Get().AvatarOutfits.Get("mah010_baldbeard01.oft");
            Avatar.Body = TSO.Content.Content.Get().AvatarOutfits.Get("mab002_slob.oft");
            Avatar.Handgroup = Avatar.Body;

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

            SetMotiveData(VMMotive.Comfort, -100);
            SetPersonData(VMPersonDataVariable.NeatPersonality, 1000); //for testing wash hands after toilet

            //also run the main function of all people because i'm a massochist
            ExecuteEntryPoint(1, context);
        }

        private void HandleTimePropsEvent(TimePropertyListItem tp)
        {
            VMAvatar avatar = this;
            var evt = tp.Properties["xevt"];
            if (evt != null)
            {
                var eventValue = short.Parse(evt);
                avatar.CurrentAnimationState.EventCode = eventValue;
                avatar.CurrentAnimationState.EventFired = true;
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
        }

        public override void Tick()
        {
            base.Tick();
            //animation update for avatars
            VMAvatar avatar = this;
            if (avatar.CurrentAnimation != null && !avatar.CurrentAnimationState.EndReached)
            {
                if (avatar.CurrentAnimationState.PlayingBackwards) avatar.CurrentAnimationState.CurrentFrame--;
                else avatar.CurrentAnimationState.CurrentFrame++;
                var currentFrame = avatar.CurrentAnimationState.CurrentFrame;
                var currentTime = currentFrame * 33.33f;
                var timeProps = avatar.CurrentAnimationState.TimePropertyLists;
                if (!avatar.CurrentAnimationState.PlayingBackwards)
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
                    for (var i = timeProps.Count-1; i >= 0; i--)
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

                var status = Animator.RenderFrame(avatar.Avatar, avatar.CurrentAnimation, avatar.CurrentAnimationState.CurrentFrame);
                if (status != AnimationStatus.IN_PROGRESS)
                {
                    avatar.CurrentAnimationState.EndReached = true;
                }
            }

            if (avatar.CarryAnimation != null)
            {
                var status = Animator.RenderFrame(avatar.Avatar, avatar.CarryAnimation, avatar.CarryAnimationState.CurrentFrame); //currently don't advance frames... I don't think any of them are animated anyways.
            }

            for (int i = 0; i < 16; i++)
            {
                MotiveChanges[i].Tick(this); //tick over motive changes
            }

            PersonData[(int)VMPersonDataVariable.TickCounter]++;
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

        public override Vector3 Position
        {
            get { return WorldUI.Position; }
            set { WorldUI.Position = value; }
        }

        public override Direction Direction
        {
            get { return tso.world.model.Direction.WEST; }
            set {  }
        }

        // Begin Container SLOTs interface

        public override void PlaceInSlot(VMEntity obj, int slot)
        {
            HandObject = obj;
            obj.SetValue(VMStackObjectVariable.ContainerId, this.ObjectID);
            obj.SetValue(VMStackObjectVariable.SlotNumber, (short)slot);
            obj.WorldUI.Container = this.WorldUI;
            obj.WorldUI.ContainerSlot = slot;
            ((ObjectComponent)obj.WorldUI).renderInfo.Layer = tso.world.WorldObjectRenderLayer.DYNAMIC;
        }

        public override VMEntity GetSlot(int slot)
        {
            return HandObject;
        }

        public override void ClearSlot(int slot)
        {
            HandObject.SetValue(VMStackObjectVariable.ContainerId, 0);
            HandObject.SetValue(VMStackObjectVariable.SlotNumber, 0);
            HandObject.WorldUI.Container = null;
            HandObject.WorldUI.ContainerSlot = 0;
            HandObject = null;
        }

        // End Container SLOTs interface

        public override Texture2D GetIcon(GraphicsDevice gd)
        {
            return null; //todo, get based on sim head
        }
    }
}
