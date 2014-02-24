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
        public AvatarComponent WorldUI;
        public AdultSimAvatar Avatar;

        /** Animation vars **/
        public Animation CurrentAnimation;
        public VMAnimationState CurrentAnimationState;

        private short[] PersonData = new short[100];

        public VMAvatar() : base(Content.Get().WorldObjects.Get(TEMPLATE_PERSON)) {
            WorldUI = new AvatarComponent();

            Avatar = new AdultSimAvatar();
            Avatar.Head = Content.Get().AvatarOutfits.Get("mah108_apallo.oft");
            Avatar.Body = Content.Get().AvatarOutfits.Get("mab011_lsexy.oft");

            WorldUI.Avatar = Avatar;
        }

        public virtual short GetPersonData(VMPersonDataVariable variable){
            /*switch (variable){
                case VMPersonDataVariable.UnusedAndDoNotUse:
                    return PersonData[(short)VMPersonDataVariable.UnusedAndDoNotUse];
            }
             - Will be reanabled later to deal with special cases where the value needs to be calculated on access.
             */
            if ((short)variable > 100) throw new Exception("Person Data out of bounds!");
            return PersonData[(short)variable];
            
        }

        public virtual bool SetPersonData(VMPersonDataVariable variable, short value){
            /*switch (variable){
                case VMPersonDataVariable.UnusedAndDoNotUse:
                    PersonData[(short)VMPersonDataVariable.UnusedAndDoNotUse] = value;
                    return true;
            }*/
            if ((short)variable > 100) throw new Exception("Person Data out of bounds!");
            PersonData[(short)variable] = value;
            return true;
        }

        public Vector3 Position { get { return WorldUI.Position; } set { WorldUI.Position = value; } }

        public override string ToString()
        {
            return "Sim";
        }
    }
}
