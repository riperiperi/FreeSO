/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using ProtocolAbstractionLibraryD;
using TSO.Vitaboy;
using TSO.Content;

namespace TSOClient.VM
{
    /// <summary>
    /// Represents a Sim/Character in the game.
    /// </summary>
    public class Sim
    {
        public AppearanceType AppearanceType { get; set; }
        public Matrix Offset = Matrix.Identity;

        private AdultVitaboyModel m_Vitaboymodel = new AdultVitaboyModel();

        public Outfit Head
        {
            get 
            {
                if (m_Vitaboymodel.Body == null)
                    return Content.Get().AvatarOutfits.Get(m_HeadOutfitID);

                return m_Vitaboymodel.Head;
            }
            set { m_Vitaboymodel.Head = value; }
        }

        public Outfit Body
        {
            get 
            {
                if (m_Vitaboymodel.Body == null)
                    return Content.Get().AvatarOutfits.Get(m_BodyOutfitID);

                return m_Vitaboymodel.Body; 
            }

            set { m_Vitaboymodel.Body = value; }
        }

        public Outfit Handgroup
        {
            get { return m_Vitaboymodel.Handgroup; }
            set { m_Vitaboymodel.Handgroup = value; }
        }

        private int m_CharacterID;

        protected Guid m_GUID;
        protected string m_Timestamp;
        protected string m_Name;
        protected string m_Sex;
        protected string m_Description;
        protected ulong m_HeadOutfitID;
        protected ulong m_BodyOutfitID;

        /// <summary>
        /// The ID of the head's outfit. Used by the network protocol.
        /// </summary>
        public ulong HeadOutfitID
        {
            get { return m_HeadOutfitID; }
            set { m_HeadOutfitID = value; }
        }

        /// <summary>
        /// The ID of the body's Outfit. Used by the network protocol.
        /// </summary>
        public ulong BodyOutfitID
        {
            get { return m_BodyOutfitID; }
            set { m_BodyOutfitID = value; }
        }

        protected CityInfo m_City;

        protected bool m_CreatedThisSession = false;

        public float HeadXPos = 0.0f, HeadYPos = 0.0f;

        public Skeleton SimSkeleton
        {
            get
            {
                return m_Vitaboymodel.Skeleton;
            }
        }

        /// <summary>
        /// Received a server-generated GUID.
        /// </summary>
        /// <param name="GUID">The GUID to assign to this sim.</param>
        public void AssignGUID(string GUID)
        {
            m_GUID = new Guid(GUID);
        }

        public Sim(string GUID)
        {
            this.AssignGUID(GUID);
            m_Vitaboymodel = new AdultVitaboyModel();
        }

        public Sim(Guid GUID)
        {
            this.m_GUID = GUID;
        }

        /// <summary>
        /// A Sim's GUID, created by the client and stored in the DB.
        /// </summary>
        public Guid GUID
        {
            get { return m_GUID; }
        }

        /// <summary>
        /// The character's ID, as it exists in the DB.
        /// </summary>
        public int CharacterID
        {
            get { return m_CharacterID; }
            set { m_CharacterID = value; }
        }

        /// <summary>
        /// When was this character last cached by the client?
        /// </summary>
        public string Timestamp
        {
            get { return m_Timestamp; }
            set { m_Timestamp = value; }
        }

        /// <summary>
        /// The character's name, as it exists in the DB.
        /// </summary>
        public string Name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        public string Sex
        {
            get { return m_Sex; }
            set { m_Sex = value; }
        }

        public string Description
        {
            get { return m_Description; }
            set { m_Description = value; }
        }

        public CityInfo ResidingCity
        {
            get { return m_City; }
            set { m_City = value; }
        }

        /// <summary>
        /// Set to true when a CharacterCreate packet was
        /// received. If this is false, the character in
        /// the DB will NOT be updated with the city that
        /// the character resides in when receiving a 
        /// KeyRequest packet from a CityServer, saving 
        /// an expensive DB call.
        /// </summary>
        public bool CreatedThisSession
        {
            get { return m_CreatedThisSession; }
            set { m_CreatedThisSession = value; }
        }
    }
}