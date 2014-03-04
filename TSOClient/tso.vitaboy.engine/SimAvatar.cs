using System;
using System.Collections.Generic;
using System.Text;
using TSO.Content;
using TSO.Common.rendering.framework;

namespace TSO.Vitaboy
{
    /// <summary>
    /// Represents all sims in the game.
    /// </summary>
    public abstract class SimAvatar : Avatar
    {
        /// <summary>
        /// Creates a new instance of SimAvatar.
        /// </summary>
        /// <param name="skel">A Skeleton instance.</param>
        public SimAvatar(Skeleton skel) : base(skel)
        {
        }

        private AvatarAppearanceInstance m_LeftHandInstance;
        private AvatarAppearanceInstance m_RightHandInstance;
        private Outfit m_Handgroup;

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
                var Handgroup = TSO.Content.Content.Get().AvatarHandgroups.Get(HandgroupID.TypeID, HandgroupID.FileID);
                Appearance LeftApr = new Appearance();
                Appearance RightApr = new Appearance();

                switch (m_Appearance)
                {
                    case AppearanceType.Light:
                        LeftApr = TSO.Content.Content.Get().AvatarAppearances.Get(Handgroup.LightSkin.LeftHand.Idle.ID);
                        RightApr = TSO.Content.Content.Get().AvatarAppearances.Get(Handgroup.LightSkin.RightHand.Idle.ID);
                        break;
                    case AppearanceType.Medium:
                        LeftApr = TSO.Content.Content.Get().AvatarAppearances.Get(Handgroup.MediumSkin.LeftHand.Idle.ID);
                        RightApr = TSO.Content.Content.Get().AvatarAppearances.Get(Handgroup.MediumSkin.RightHand.Idle.ID);
                        break;
                    case AppearanceType.Dark:
                        LeftApr = TSO.Content.Content.Get().AvatarAppearances.Get(Handgroup.DarkSkin.LeftHand.Idle.ID);
                        RightApr = TSO.Content.Content.Get().AvatarAppearances.Get(Handgroup.MediumSkin.RightHand.Idle.ID);
                        break;
                }

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
                var Appearance = TSO.Content.Content.Get().AvatarAppearances.Get(AppearanceID);
                if (Appearance != null)
                {
                    m_HeadInstance = base.AddAppearance(Appearance);
                }
            }
        }

        private AvatarAppearanceInstance _BodyInstance;
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
            if (_BodyInstance != null)
            {
                base.RemoveAppearance(_BodyInstance, true);
            }
            if (m_Body != null)
            {
                var AppearanceID = m_Body.GetAppearance(m_Appearance);
                var Appearance = TSO.Content.Content.Get().AvatarAppearances.Get(AppearanceID);
                if (Appearance != null)
                {
                    _BodyInstance = base.AddAppearance(Appearance);
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
}
