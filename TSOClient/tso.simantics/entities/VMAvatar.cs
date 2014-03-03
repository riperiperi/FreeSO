using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.world.components;
using tso.vitaboy;
using tso.content;
using Microsoft.Xna.Framework;
using tso.simantics.model;

namespace tso.simantics
{
    public class VMAvatar : VMEntity
    {
        public const uint TEMPLATE_PERSON = 0x7FD96B54;
        
        public AdultSimAvatar Avatar;

        /** Animation vars **/
        public Animation CurrentAnimation;
        public VMAnimationState CurrentAnimationState;

        private short[] PersonData = new short[100];
        private short[] MotiveData = new short[16];

        public VMAvatar() : base(Content.Get().WorldObjects.Get(TEMPLATE_PERSON)) {
            WorldUI = new AvatarComponent();

            Avatar = new AdultSimAvatar();
            Avatar.Head = Content.Get().AvatarOutfits.Get("mah108_apallo.oft");
            Avatar.Body = Content.Get().AvatarOutfits.Get("mab011_lsexy.oft");

            var avatarc = (AvatarComponent)WorldUI;
            avatarc.Avatar = Avatar;
        }

        public override void Init(tso.simantics.VMContext context)
        {
            base.Init(context);
            var testa = 0;
            //also run the main function of all people because i'm a massochist
            //ExecuteEntryPoint(1, context);
        }

        public virtual short GetPersonData(VMPersonDataVariable variable){
            /*switch (variable){
                case VMPersonDataVariable.UnusedAndDoNotUse:
                    return PersonData[(short)VMPersonDataVariable.UnusedAndDoNotUse];
            }
             - Will be reanabled later to deal with special cases where the value needs to be calculated on access.
             */
            if ((ushort)variable > 100) throw new Exception("Person Data out of bounds!");
            return PersonData[(ushort)variable];
            
        }

        public virtual bool SetPersonData(VMPersonDataVariable variable, short value){
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
