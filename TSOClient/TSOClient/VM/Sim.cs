/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

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
using tso.vitaboy;

namespace TSOClient.VM
{
    /// <summary>
    /// Represents a Sim/Character in the game.
    /// </summary>
    public class Sim
    {
        public ulong HeadOutfitID { get; set; }
        public ulong BodyOutfitID { get; set; }
        public AppearanceType AppearanceType { get; set; }
        public Matrix Offset = Matrix.Identity;

        private int m_CharacterID;

        protected Guid m_GUID;
        protected string m_Timestamp;
        protected string m_Name;
        protected string m_Sex;
        protected string m_Description;
        protected ulong m_HeadOutfitID;
        protected ulong m_BodyOutfitID;

        protected CityInfo m_City;

        protected bool m_CreatedThisSession = false;

        private Skeleton m_Skeleton;

        public float HeadXPos = 0.0f, HeadYPos = 0.0f;

        public Skeleton SimSkeleton
        {
            get
            {
                if (m_Skeleton == null)
                {
                    m_Skeleton = new Skeleton();
                    m_Skeleton.Read(new MemoryStream(ContentManager.GetResourceFromLongID(0x100000005)));
                    return m_Skeleton;
                }

                return m_Skeleton;
            }

            set
            {
                m_Skeleton = value;
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
        }

        public Sim(Guid GUID)
        {
            this.m_GUID = GUID;
        }


        /// <summary>
        /// The account which is the owner of this Sim.
        /// </summary>
        /*public Account Account
        {
            get { return m_Account; }
        }*/

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

    //public class HandBindings
    //{
    //    public List<SimModelBinding> FistBindings;
    //    public List<SimModelBinding> IdleBindings;
    //    public List<SimModelBinding> PointingBindings;

    //    public HandBindings()
    //    {
    //        FistBindings = new List<SimModelBinding>();
    //        IdleBindings = new List<SimModelBinding>();
    //        PointingBindings = new List<SimModelBinding>();
    //    }
    //}
}