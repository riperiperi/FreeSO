/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Text;
using FSO.Content;
using FSO.Common.Rendering.Framework;
using FSO.Vitaboy;

namespace FSO.Vitaboy
{
    /// <summary>
    /// Represents all sims in the game.
    /// </summary>
    public class SimAvatar : Avatar
    {
        /// <summary>
        /// Creates a new instance of SimAvatar.
        /// </summary>
        /// <param name="skel">A Skeleton instance.</param>
        public SimAvatar(Skeleton skel) : base(skel)
        {
        }

        public SimAvatar(SimAvatar old)
            : base(old.BaseSkeleton)
        {
            m_Handgroup = old.m_Handgroup;
            m_Body = old.m_Body;
            m_Head = old.m_Head;
            m_Appearance = old.m_Appearance;

            ReloadSkeleton();
            ReloadHead();
            ReloadBody();
            ReloadHandgroup();
        }

        /// <summary>
        /// Helper method to remove all body parts from an avatar save for the head.
        /// Used by pie menus.
        /// </summary>
        public void StripAllButHead()
        {
            if (m_Handgroup != null) {
                RemoveAppearance(m_LeftHandInstance, true);
                RemoveAppearance(m_RightHandInstance, true);
            }
            RemoveAppearance(m_BodyInstance, true);
        }

        private AvatarAppearanceInstance m_LeftHandInstance;
        private AvatarAppearanceInstance m_RightHandInstance;
        private Outfit m_Handgroup;
        private SimHandGesture m_LeftHandGesture = SimHandGesture.Idle;
        private SimHandGesture m_RightHandGesture = SimHandGesture.Idle;

        /// <summary>
        /// Gets or sets the handgroup of this SimAvatar.
        /// Handgroups use the same outfit as bodies!
        /// </summary>
        public Outfit Handgroup
        {
            get { return m_Handgroup; }
            set
            {
                m_Handgroup = value;
                ReloadHandgroup();
            }
        }

        /// <summary>
        /// Gets or sets the left hand gesture of this SimAvatar.
        /// Idle, fist or pointing! Usually should be set by animations, defaults to idle.
        /// </summary>
        public SimHandGesture LeftHandGesture
        {
            get { return m_LeftHandGesture; }
            set
            {
                m_LeftHandGesture = value;
                ReloadHandgroup();
            }
        }

        /// <summary>
        /// Gets or sets the right hand gesture of this SimAvatar.
        /// Idle, fist or pointing! Usually should be set by animations, defaults to idle.
        /// </summary>
        public SimHandGesture RightHandGesture
        {
            get { return m_RightHandGesture; }
            set
            {
                m_RightHandGesture = value;
                ReloadHandgroup();
            }
        }

        private AvatarAppearanceInstance m_HeadInstance;
        private Outfit m_Head;

        /// <summary>
        /// Gets or sets the head of this SimAvatar.
        /// </summary>
        public Outfit Head
        {
            get { return m_Head; }
            set {
                m_Head = value;
                ReloadHead();
            }
        }

        /// <summary>
        /// Reloads the hand meshes.
        /// </summary>
        private void ReloadHandgroup()
        {
            if (m_LeftHandInstance != null)
                base.RemoveAppearance(m_LeftHandInstance, true);
            if(m_RightHandInstance != null)
                base.RemoveAppearance(m_RightHandInstance, true);

            if (m_Handgroup != null)
            {
                var HandgroupID = m_Handgroup.GetHandgroup();
                if (HandgroupID.FileID == 0) HandgroupID.FileID = (int)(158913789970>>32);
                var Handgroup = FSO.Content.Content.Get().AvatarHandgroups.Get(HandgroupID.TypeID, HandgroupID.FileID);

                FSO.Common.Content.ContentID LeftID = null;
                FSO.Common.Content.ContentID RightID = null;

                FSO.Vitaboy.HandSet HSet = null;

                switch (m_Appearance)
                {
                    case AppearanceType.Light:
                        HSet = Handgroup.LightSkin;
                        break;
                    case AppearanceType.Medium:
                        HSet = Handgroup.MediumSkin;
                        break;
                    case AppearanceType.Dark:
                        HSet = Handgroup.DarkSkin;
                        break;
                }

                switch (m_LeftHandGesture)
                {
                    case SimHandGesture.Idle:
                        LeftID = HSet.LeftHand.Idle.ID;
                        break;
                    case SimHandGesture.Fist:
                        LeftID = HSet.LeftHand.Fist.ID;
                        break;
                    case SimHandGesture.Pointing:
                        LeftID = HSet.LeftHand.Pointing.ID;
                        break;
                }

                switch (m_RightHandGesture)
                {
                    case SimHandGesture.Idle:
                        RightID = HSet.RightHand.Idle.ID;
                        break;
                    case SimHandGesture.Fist:
                        RightID = HSet.RightHand.Fist.ID;
                        break;
                    case SimHandGesture.Pointing:
                        RightID = HSet.RightHand.Pointing.ID;
                        break;
                }

                Appearance LeftApr = FSO.Content.Content.Get().AvatarAppearances.Get(LeftID);
                Appearance RightApr = FSO.Content.Content.Get().AvatarAppearances.Get(RightID);

                if (LeftApr != null)
                    m_LeftHandInstance = base.AddAppearance(LeftApr);
                if(RightApr != null)
                    m_RightHandInstance = base.AddAppearance(RightApr);
            }
        }

        /// <summary>
        /// Reloads the head mesh.
        /// </summary>
        private void ReloadHead()
        {
            if (m_HeadInstance != null){
                base.RemoveAppearance(m_HeadInstance, true);
            }
            if (m_Head != null)
            {
                var AppearanceID = m_Head.GetAppearance(m_Appearance);
                var Appearance = FSO.Content.Content.Get().AvatarAppearances.Get(AppearanceID);
                if (Appearance != null)
                {
                    m_HeadInstance = base.AddAppearance(Appearance);
                }
            }
        }

        private AvatarAppearanceInstance m_BodyInstance;
        private Outfit m_Body;

        /// <summary>
        /// Gets of sets the body of this SimAvatar.
        /// </summary>
        public Outfit Body
        {
            get
            {
                return m_Body;
            }
            set
            {
                m_Body = value;
                ReloadBody();
            }
        }
        
        /// <summary>
        /// Reloads the body mesh.
        /// </summary>
        private void ReloadBody()
        {
            if (m_BodyInstance != null)
            {
                base.RemoveAppearance(m_BodyInstance, true);
            }
            if (m_Body != null)
            {
                var AppearanceID = m_Body.GetAppearance(m_Appearance);
                var Appearance = FSO.Content.Content.Get().AvatarAppearances.Get(AppearanceID);
                if (Appearance != null)
                {
                    m_BodyInstance = base.AddAppearance(Appearance);
                }
            }
        }
        
        private AppearanceType m_Appearance = AppearanceType.Light;

        /// <summary>
        /// Gets or sets the ApperanceType of this SimAvatar.
        /// </summary>
        public AppearanceType Appearance
        {
            get
            {
                return m_Appearance;
            }
            set
            {
                m_Appearance = value;
                ReloadHead();
            }
        }

        private ulong _BodyOutfitId;
        public ulong BodyOutfitId
        {
            set
            {
                _BodyOutfitId = value;
                var outfit = Content.Content.Get().AvatarOutfits.Get(value);
                this.Body = outfit;
                this.Handgroup = outfit;
            }
            get
            {
                return _BodyOutfitId;
            }
        }

        private ulong _HeadOutfitId;
        public ulong HeadOutfitId
        {
            set
            {
                _HeadOutfitId = value;
                var outfit = Content.Content.Get().AvatarOutfits.Get(value);
                this.Head = outfit;
            }
            get
            {
                return _HeadOutfitId;
            }
        }

        public ulong ShortBodyOutfitId
        {
            set
            {
                BodyOutfitId = (value << 32) & 0x0000000D;
            }
        }

        public ulong ShortHeadOutfitId
        {
            set
            {
                HeadOutfitId = (value << 32) & 0x0000000D;
            }
        }

        /// <summary>
        /// Graphics device was reset.
        /// </summary>
        /// <param name="Device">GraphicsDevice instance.</param>
        public override void DeviceReset(Microsoft.Xna.Framework.Graphics.GraphicsDevice Device)
        {
            ReloadSkeleton();
            ReloadHead();
            ReloadBody();
            ReloadHandgroup();
        }
    }

    public enum SimHandGesture
    {
        Idle = 0,
        Fist = 2,
        Pointing = 1,
        None = 3 //some animations remove the hands completely, eg. for puppets
    }
}
