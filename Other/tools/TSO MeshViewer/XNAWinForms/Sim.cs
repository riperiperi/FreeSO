/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSO MeshViewer.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s):
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dressup
{
    /// <summary>
    /// Represents a renderable Sim.
    /// </summary>
    public class Sim
    {
        private Matrix m_WorldMat;
        public Skeleton SimSkeleton;
        private Mesh m_HeadMesh, m_BodyMesh, m_LeftHand, m_RightHand;
        private Texture2D m_HeadTexture, m_BodyTexture, m_LeftHandTex, m_RightHandTex;

        public Sim(Matrix WorldMatrix)
        {
            m_WorldMat = WorldMatrix;

            SimSkeleton = new Skeleton();
            SimSkeleton.Read(ContentManager.GetResourceFromLongID(0x100000005));
            SimSkeleton.ComputeBonePositions(SimSkeleton.RootBone, m_WorldMat);
        }

        /// <summary>
        /// Adds a head mesh for this Sim.
        /// </summary>
        /// <param name="ID">The ID of the head mesh to add.</param>
        public void AddHeadMesh(ulong ID)
        {
            m_HeadMesh = new Mesh();
            m_HeadMesh.Read(ContentManager.GetResourceFromLongID(ID));
            m_HeadMesh.ProcessMesh(SimSkeleton, true);
        }

        /// <summary>
        /// Returns a head mesh for this Sim.
        /// </summary>
        /// <returns>This Sims's head mesh.</returns>
        public Mesh GetHeadMesh()
        {
            return m_HeadMesh;
        }

        /// <summary>
        /// Adds a head texture for this Sim.
        /// </summary>
        /// <param name="ID">The ID of the head texture to add.</param>
        public void AddHeadTexture(GraphicsDevice Device, ulong ID)
        {
            m_HeadTexture = Texture2D.FromFile(Device, new MemoryStream(
                ContentManager.GetResourceFromLongID(ID)));
        }

        /// <summary>
        /// Returns a head texture for this Sim.
        /// </summary>
        /// <returns>This Sims's head texture.</returns>
        public Texture2D GetHeadTexture()
        {
            return m_HeadTexture;
        }

        /// <summary>
        /// Adds a body mesh for this Sim.
        /// </summary>
        /// <param name="ID">The ID of the body mesh to add.</param>
        public void AddBodyMesh(ulong ID)
        {
            m_BodyMesh = new Mesh();
            m_BodyMesh.Read(ContentManager.GetResourceFromLongID(ID));
            m_BodyMesh.TransformVertices(SimSkeleton.RootBone);
            //m_BodyMesh.BlendVertices2();
            m_BodyMesh.ProcessMesh(SimSkeleton, false);
        }

        /// <summary>
        /// Returns a body mesh for this Sim.
        /// </summary>
        /// <returns>This Sims's body mesh.</returns>
        public Mesh GetBodyMesh()
        {
            return m_BodyMesh;
        }

        /// <summary>
        /// Adds a body texture for this Sim.
        /// </summary>
        /// <param name="ID">The ID of the body texture to add.</param>
        public void AddBodyTexture(GraphicsDevice Device, ulong ID)
        {
            m_BodyTexture = Texture2D.FromFile(Device, new MemoryStream(
                ContentManager.GetResourceFromLongID(ID)));
        }

        /// <summary>
        /// Returns a body texture for this Sim.
        /// </summary>
        /// <returns>This Sims's body texture.</returns>
        public Texture2D GetBodyTexture()
        {
            return m_BodyTexture;
        }

        /// <summary>
        /// Adds a left hand mesh for this Sim.
        /// </summary>
        /// <param name="ID">The ID of the left hand mesh to add.</param>
        public void AddLHandMesh(ulong ID)
        {
            m_LeftHand = new Mesh();
            m_LeftHand.Read(ContentManager.GetResourceFromLongID(ID));
            m_LeftHand.ProcessMesh(SimSkeleton, false);
        }

        /// <summary>
        /// Returns a left hand mesh for this Sim.
        /// </summary>
        /// <returns>This Sims's left hand mesh.</returns>
        public Mesh GetLHandMesh()
        {
            return m_LeftHand;
        }

        /// <summary>
        /// Adds a left hand texture for this Sim.
        /// </summary>
        /// <param name="ID">The ID of the left hand texture to add.</param>
        public void AddLHandTexture(GraphicsDevice Device, ulong ID)
        {
            m_LeftHandTex = Texture2D.FromFile(Device, new MemoryStream(
                ContentManager.GetResourceFromLongID(ID)));
        }

        /// <summary>
        /// Returns a left hand texture for this Sim.
        /// </summary>
        /// <returns>This Sims's left hand texture.</returns>
        public Texture2D GetLHandTexture()
        {
            return m_LeftHandTex;
        }

        /// <summary>
        /// Adds a right hand mesh for this Sim.
        /// </summary>
        /// <param name="ID">The ID of the right hand mesh to add.</param>
        public void AddRHandMesh(ulong ID)
        {
            m_RightHand = new Mesh();
            m_RightHand.Read(ContentManager.GetResourceFromLongID(ID));
            m_RightHand.ProcessMesh(SimSkeleton, false);
        }

        /// <summary>
        /// Returns a right hand mesh for this Sim.
        /// </summary>
        /// <returns>This Sims's right hand mesh.</returns>
        public Mesh GetRHandMesh()
        {
            return m_LeftHand;
        }

        /// <summary>
        /// Adds a right hand texture for this Sim.
        /// </summary>
        /// <param name="ID">The ID of the right hand texture to add.</param>
        public void AddRHandTexture(GraphicsDevice Device, ulong ID)
        {
            m_RightHandTex = Texture2D.FromFile(Device, new MemoryStream(
                ContentManager.GetResourceFromLongID(ID)));
        }

        /// <summary>
        /// Returns a right hand texture for this Sim.
        /// </summary>
        /// <returns>This Sims's right hand texture.</returns>
        public Texture2D GetRHandTexture()
        {
            return m_RightHandTex;
        }
    }
}
