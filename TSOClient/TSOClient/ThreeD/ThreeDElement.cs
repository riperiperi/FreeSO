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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TSOClient.Code.UI.Model;

namespace TSOClient.ThreeD
{
    /// <summary>
    /// Represents a renderable 3D object.
    /// </summary>
    public abstract class ThreeDElement
    {
        private string m_StringID;

        public ThreeDScene Scene;

        public ThreeDElement()
        {
        }



        private Vector3 m_Position = Vector3.Zero;
        private Vector3 m_Scale = Vector3.One;
        private float m_RotateX = 0.0f;
        private float m_RotateY = 0.0f;
        private float m_RotateZ = 0.0f;


        public float RotationX
        {
            get { return m_RotateX; }
            set
            {
                m_RotateX = value;
                m_WorldDirty = true;
            }
        }

        public float RotationY
        {
            get { return m_RotateY; }
            set
            {
                m_RotateY = value;
                m_WorldDirty = true;
            }
        }

        public float RotationZ
        {
            get { return m_RotateZ; }
            set
            {
                m_RotateZ = value;
                m_WorldDirty = true;
            }
        }

        public Vector3 Position
        {
            get { return m_Position; }
            set
            {
                m_Position = value;
                m_WorldDirty = true;
            }
        }

        public Vector3 Scale
        {
            get { return m_Scale; }
            set
            {
                m_Scale = value;
                m_WorldDirty = true;
            }
        }


        private Matrix m_World = Matrix.Identity;
        private bool m_WorldDirty = false;
        public Matrix World
        {
            get
            {
                if (m_WorldDirty)
                {
                    m_World = Matrix.CreateRotationX(m_RotateX) * Matrix.CreateRotationY(m_RotateY) * Matrix.CreateRotationZ(m_RotateZ) * Matrix.CreateScale(m_Scale) * Matrix.CreateTranslation(m_Position);
                    m_WorldDirty = false;
                }
                return m_World;
            }
        }




        public string ID
        {
            get { return m_StringID; }
            set { m_StringID = value; }
        }


        public abstract void Update(UpdateState state);
        public abstract void Draw(GraphicsDevice device, ThreeDScene scene);


        public override string ToString()
        {
            if (m_StringID != null)
            {
                return m_StringID;
            }
            return base.ToString();
        }
    }
}
