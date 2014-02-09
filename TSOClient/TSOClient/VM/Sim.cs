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
using SimsLib.ThreeD;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using TSOClient.Code.Rendering.Sim;

namespace TSOClient.VM
{
    /// <summary>
    /// Represents a Sim/Character in the game.
    /// </summary>
    public class Sim : SimulationObject
    {
        public ulong HeadOutfitID { get; set; }
        public ulong BodyOutfitID { get; set; }
        public AppearanceType AppearanceType { get; set; }
        public Matrix Offset = Matrix.Identity;



        private int m_CharacterID;
        private int m_CityID;
        private string m_Timestamp;
        private string m_Name;
        private string m_Sex;
        private string m_Description;

        private Skeleton m_Skeleton;

        ////Head
        //private Mesh m_HeadMesh;
        //private Texture2D m_HeadTexture;

        ////Body
        //private Mesh m_BodyMesh;
        //private Texture2D m_BodyTexture;

        public float HeadXPos = 0.0f, HeadYPos = 0.0f;

        /// <summary>
        /// The player's description of this Sim.
        /// </summary>
        public string Description
        {
            get { return m_Description; }
            set { m_Description = value; }
        }

        /// <summary>
        /// This sim's city's ID.
        /// </summary>
        public int CityID
        {
            get { return m_CityID; }
            set { m_CityID = value; }
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

        ///// <summary>
        ///// The headmesh for this Sim.
        ///// </summary>
        //public Mesh HeadMesh
        //{
        //    get { return m_HeadMesh; }
        //    set { m_HeadMesh = value; }
        //}

        //public Mesh BodyMesh
        //{
        //    get { return m_BodyMesh; }
        //    set { m_BodyMesh = value; }
        //}

        public Skeleton SimSkeleton
        {
            get
            {
                if (m_Skeleton == null)
                {
                    m_Skeleton = new Skeleton();
                    m_Skeleton.Read(ContentManager.GetResourceFromLongID(0x100000005));
                    return m_Skeleton;
                }

                return m_Skeleton;
            }

            set
            {
                m_Skeleton = value;
            }
        }

        ///// <summary>
        ///// The headtexture for this Sim.
        ///// </summary>
        //public Texture2D HeadTexture
        //{
        //    get { return m_HeadTexture; }
        //    set { m_HeadTexture = value; }
        //}

        //public Texture2D BodyTexture
        //{
        //    get { return m_BodyTexture; }
        //    set { m_BodyTexture = value; }
        //}

        public Sim(string GUID) : 
            base(GUID)
        {
        }




        public List<SimModelBinding> HeadBindings = new List<SimModelBinding>();
        public List<SimModelBinding> BodyBindings = new List<SimModelBinding>();







        #region Rendering


        /// <summary>
        /// Modifies the meshes to have the correct positions
        /// based on the skel
        /// </summary>
        public void RepositionMesh()
        {
            var skel = SimSkeleton;

            foreach (var binding in HeadBindings)
            {
                binding.Mesh.TransformVertices(skel.RootBone);
            }
            foreach (var binding in BodyBindings)
            {
                binding.Mesh.TransformVertices(skel.RootBone);
            }
        }

        /// <summary>
        /// If a bone has moved, this method will recalculate
        /// all the 3d position data
        /// </summary>
        public void Reposition()
        {
            var skel = SimSkeleton;

            skel.ComputeBonePositions(skel.RootBone, Offset);
            RepositionMesh();
        }



        #endregion






    }
}
