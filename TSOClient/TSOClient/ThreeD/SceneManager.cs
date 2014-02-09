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
using TSOClient.Code;
using TSOClient.Code.UI.Model;
using tso.common.rendering.framework.model;
using tso.common.rendering.framework;

namespace TSOClient.ThreeD
{
    /// <summary>
    /// Manages ThreeDScene instances.
    /// </summary>
    public class SceneManager
    {
        private List<_3DScene> m_Scenes = new List<ThreeDScene>();

        private Game m_Game;

        private Matrix m_WorldMatrix, m_ViewMatrix, m_ProjectionMatrix;

        public List<ThreeDScene> Scenes
        {
            get { return m_Scenes; }
        }

        /// <summary>
        /// The graphicsdevice that is part of the game instance.
        /// Used when calling XNA's graphic functions.
        /// </summary>
        public GraphicsDevice Device
        {
            get { return m_Game.GraphicsDevice; }
        }

        /// <summary>
        /// A worldmatrix, used to display 3D objects (sims).
        /// Initialized in the ScreenManager's constructor.
        /// </summary>
        public Matrix WorldMatrix
        {
            get { return m_WorldMatrix; }
            set { m_WorldMatrix = value; }
        }

        /// <summary>
        /// A viewmatrix, used to display 3D objects (sims).
        /// Initialized in the ScreenManager's constructor.
        /// </summary>
        public Matrix ViewMatrix
        {
            get { return m_ViewMatrix; }
            set { m_WorldMatrix = value; }
        }

        /// <summary>
        /// A projectionmatrix, used to display 3D objects (sims).
        /// Initialized in the ScreenManager's constructor.
        /// </summary>
        public Matrix ProjectionMatrix
        {
            get { return m_ProjectionMatrix; }
            set { m_ProjectionMatrix = value; }
        }

        public SceneManager(Game G)
        {
            m_Game = G;

            //Device.VertexDeclaration = new VertexDeclaration(Device, VertexPositionNormalTexture.VertexElements);
            Device.RenderState.CullMode = CullMode.None;

            m_WorldMatrix = Matrix.Identity;
            m_ViewMatrix = Matrix.CreateLookAt(Vector3.Backward * 5, Vector3.Zero, Vector3.Right);




            //m_ProjectionMatrix = Matrix.CreatePerspectiveOffCenter(0.0f, (float)Device.PresentationParameters.BackBufferWidth, (float)Device.PresentationParameters.BackBufferHeight, 0.0f, 1.0f, 100000.0f);
            m_ProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.Pi / 4.0f,
                    (float)Device.PresentationParameters.BackBufferWidth /
                    (float)Device.PresentationParameters.BackBufferHeight,
                    1.0f, 10000.0f);

        }

        public List<ThreeDScene> ExternalScenes = new List<ThreeDScene>();

        /// <summary>
        /// Adds a ThreeDScene instance to the scene manager but the scene manager will not render
        /// this scene. It is only added so it can be included in the debug panel
        /// </summary>
        /// <param name="Scene"></param>
        public void AddExternalScene(ThreeDScene Scene)
        {
            ExternalScenes.Add(Scene);
        }

        /// <summary>
        /// Adds a ThreeDScene instance to this SceneManager instance's list of scenes.
        /// </summary>
        /// <param name="Scene">The ThreeDScene instance to add.</param>
        public void AddScene(ThreeDScene Scene)
        {
            m_Scenes.Add(Scene);
        }

        /// <summary>
        /// Removes a scene from this SceneManager instances' list of scenes.
        /// </summary>
        /// <param name="Scene">The ThreeDScene instance to remove.</param>
        public void RemoveScene(ThreeDScene Scene)
        {
            m_Scenes.Remove(Scene);
        }

        public void Update(UpdateState GState)
        {
            for(int i = 0; i < m_Scenes.Count; i++)
            {
                m_Scenes[i].Update(GState);
            }
        }

        public void Draw()
        {
            var device = GameFacade.GraphicsDevice;

            for (int i = 0; i < m_Scenes.Count; i++)
            {
                m_Scenes[i].Draw(device);
            }
        }
    }
}
