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
            switch (variable){
                case VMPersonDataVariable.UnusedAndDoNotUse:
                    return PersonData[(short)VMPersonDataVariable.UnusedAndDoNotUse];
            }
            throw new Exception("Unknown get person data!");
        }

        public virtual bool SetPersonData(VMPersonDataVariable variable, short value){
            switch (variable){
                case VMPersonDataVariable.UnusedAndDoNotUse:
                    PersonData[(short)VMPersonDataVariable.UnusedAndDoNotUse] = value;
                    return true;
            }
            throw new Exception("Unknown set person data!");
        }

        public Vector3 Position { get { return WorldUI.Position; } set { WorldUI.Position = value; } }

        public override string ToString()
        {
            return "Sim";
        }
    }
}
