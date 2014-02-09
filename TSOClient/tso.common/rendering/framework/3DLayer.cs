using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace tso.common.rendering.framework
{
    public class _3DLayer : IGraphicsLayer
    {
        public GraphicsDevice Device;
        public List<_3DAbstract> Scenes = new List<_3DAbstract>();


        #region IGraphicsLayer Members

        public void Update(tso.common.rendering.framework.model.UpdateState state)
        {
            foreach (var scene in Scenes)
            {
                scene.Update(state);
            }
        }

        public void PreDraw(Microsoft.Xna.Framework.Graphics.GraphicsDevice device)
        {
            foreach (var scene in Scenes)
            {
                scene.PreDraw(device);
            }
        }

        public void Draw(Microsoft.Xna.Framework.Graphics.GraphicsDevice device)
        {
            foreach (var scene in Scenes)
            {
                scene.Draw(device);
            }
        }

        public void Initialize(Microsoft.Xna.Framework.Graphics.GraphicsDevice device)
        {
            this.Device = device;
            foreach (var scene in Scenes)
            {
                scene.Initialize(this);
            }
        }

        public void Add(_3DAbstract scene)
        {
            Scenes.Add(scene);
            if (this.Device != null)
            {
                scene.Initialize(this);
            }
        }

        #endregion
    }
}
