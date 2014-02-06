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
using System.Threading;
using Microsoft.Xna.Framework;
using TSOClient.Code;
using Microsoft.Xna.Framework.Graphics;
using TSOClient.Code.Rendering;
using TSOClient.Code.Utils;

namespace TSOClient.ThreeD
{
    /// <summary>
    /// A renderable three dimensional scene that is rendered
    /// separately from UIScenes. 3D objects cannot be rendered
    /// between a call to SpriteBatch.Begin() and SpriteBatch.End(),
    /// as that will cause the objects to be transparent.
    /// </summary>
    public abstract class ThreeDAbstract
    {
        public Camera Camera;
        public string ID;
        public abstract List<ThreeDElement> GetElements();
        public abstract void Add(ThreeDElement item);
        public abstract void Update(GameTime Time);
        public abstract void Draw(GraphicsDevice device);

        public abstract void DeviceReset(GraphicsDevice Device);

        public static bool IsInvalidated;
    }

    public class ThreeDScene : ThreeDAbstract
    {
        private SceneManager m_SceneMgr;
        private List<ThreeDElement> m_Elements = new List<ThreeDElement>();

        public SceneManager SceneMgr
        {
            get { return m_SceneMgr; }
        }

        public ThreeDScene()
        {
            m_SceneMgr = GameFacade.Scenes;
            Camera = new Camera(Vector3.Backward * 17, Vector3.Zero, Vector3.Right);
        }

        public override List<ThreeDElement> GetElements()
        {
            return m_Elements;
        }

        public override void Update(GameTime Time)
        {
            for (int i = 0; i < m_Elements.Count; i++)
                m_Elements[i].Update(Time);
        }

        public override void DeviceReset(GraphicsDevice Device)
        {
            IsInvalidated = true;

            Camera = new Camera(Vector3.Backward * 17, Vector3.Zero, Vector3.Right);

            //Can't reload resources directly, so supplant the reset...
            for (int i = 0; i < m_Elements.Count; i++)
                m_Elements[i].DeviceReset(Device);

            IsInvalidated = false;
        }

        public ThreeDScene(SceneManager SceneMgr)
        {
            m_SceneMgr = SceneMgr;
        }

        public override void Add(ThreeDElement item)
        {
            m_Elements.Add(item);
            item.Scene = this;
        }

        public override void Draw(GraphicsDevice device)
        {
            for (int i = 0; i < m_Elements.Count; i++)
            {
                if(!IsInvalidated)
                    m_Elements[i].Draw(device, this);
            }

            if (Camera.DrawCamera)
            {
                if(!IsInvalidated)
                    Camera.Draw(device);
            }
        }

        public override string ToString()
        {
            if (ID != null)
            {
                return ID;
            }

            return base.ToString();
        }
    }
}
