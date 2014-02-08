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
using ProtocolAbstractionLibraryD.VM;

namespace TSOClient.VM
{
    /// <summary>
    /// Represents a Sim/Character in the game.
    /// </summary>
    public class Sim : SimBase
    {
        public ulong HeadOutfitID { get; set; }
        public ulong BodyOutfitID { get; set; }
        public AppearanceType AppearanceType { get; set; }
        public Matrix Offset = Matrix.Identity;

        private int m_CharacterID;

        private Skeleton m_Skeleton;

        public float HeadXPos = 0.0f, HeadYPos = 0.0f;

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

        /// <summary>
        /// Received a server-generated GUID.
        /// </summary>
        /// <param name="GUID">The GUID to assign to this sim.</param>
        public void AssignGUID(string GUID)
        {
            m_GUID = new Guid(GUID);
        }

        public Sim(string GUID) :
            base(new Guid(GUID))
        {
        }

        public List<SimModelBinding> HeadBindings = new List<SimModelBinding>();
        public List<SimModelBinding> BodyBindings = new List<SimModelBinding>();

        public HandBindings LeftHandBindings = new HandBindings();
        public HandBindings RightHandBindings = new HandBindings();

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

            //Only do idle hands for now...
            foreach (var binding in LeftHandBindings.IdleBindings)
            {
                binding.Mesh.TransformVertices(skel.RootBone);
            }
            foreach (var binding in RightHandBindings.IdleBindings)
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

    public class HandBindings
    {
        public List<SimModelBinding> FistBindings;
        public List<SimModelBinding> IdleBindings;
        public List<SimModelBinding> PointingBindings;

        public HandBindings()
        {
            FistBindings = new List<SimModelBinding>();
            IdleBindings = new List<SimModelBinding>();
            PointingBindings = new List<SimModelBinding>();
        }
    }
}
