/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using FSO.Client.UI.Framework.Parser;
using Microsoft.Xna.Framework;
using FSO.Client.Utils;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Utils;

namespace FSO.Client.UI.Framework
{
    public class UIContainer : UIElement
    {
        /// <summary>
        /// List of UIElements inside this UIContainer
        /// </summary>
        protected List<UIElement> Children { get; set; }

        public UIContainer()
        {
            Children = new List<UIElement>();
        }

        public List<UIElement> ChildrenWithinIdRange(int min, int max)
        {
            var result = new List<UIElement>();
            foreach (var child in Children)
            {
                if (child.NumericId != null)
                {
                    if (child.NumericId >= min && child.NumericId <= max)
                    {
                        result.Add(child);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Adds a UIElement at the top most position in the container
        /// </summary>
        /// <param name="child"></param>
        public void AddBefore(UIElement child, UIElement before)
        {
            var index = Children.IndexOf(before);
            if (index != -1)
            {
                AddAt(index, child);
            }
            else
            {
                Add(child);
            }
        }

        /// <summary>
        /// Adds a UIElement at the top most position in the container
        /// </summary>
        /// <param name="child"></param>
        public virtual void Add(UIElement child)
        {
            if (child == null) { return; }

            lock (Children)
            {
                if (Children.Contains(child))
                {
                    Children.Remove(child);
                }
                Children.Add(child);
                AssignAsInvalidationParent(this);
                child.Parent = this;
            }
        }

        public void AssignAsInvalidationParent(UIContainer cont)
        {
            if (InvalidationParent == null) return;
            foreach (var child in cont.Children)
            {
                if (child.InvalidationParent == null) {
                    child.InvalidationParent = InvalidationParent;
                    if (child is UIContainer) AssignAsInvalidationParent((UIContainer)child);
                }
            }
        }

        /// <summary>
        /// Adds a UIElement at a specific depth in the container
        /// </summary>
        /// <param name="index"></param>
        /// <param name="child"></param>
        public virtual void AddAt(int index, UIElement child)
        {
            lock (Children)
            {
                if (Children.Contains(child))
                {
                    Children.Remove(child);
                }

                Children.Insert(index, child);
                AssignAsInvalidationParent(this);
                child.Parent = this;
            }
        }

        public void Remove(UIElement child)
        {
            lock (Children)
            {
                Children.Remove(child);
                child?.Removed();
                //if (child?.Parent == this) child.Parent = null;
            }
        }

        public override void Removed()
        {
            base.Removed();
            var childCopy = new List<UIElement>(Children);
            foreach (var child in childCopy)
            {
                child.Removed();
            }
        }

        /// <summary>
        /// Get a list of the children, this is for debug only,
        /// you should not modify this array
        /// </summary>
        /// <returns></returns>
        public List<UIElement> GetChildren()
        {
            return Children;
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void CalculateMatrix()
        {
            base.CalculateMatrix();

            /**
             * If our matrix has changed, then our children's matrixes will have to as well
             */
            lock (Children)
            {
                foreach (var child in Children)
                {
                    child.InvalidateMatrix();
                }
            }
        }

        protected override void CalculateOpacity()
        {
            base.CalculateOpacity();

            /**
             * If our matrix has changed, then our children's matrixes will have to as well
             */
            lock (Children)
            {
                foreach (var child in Children)
                {
                    child.InvalidateOpacity();
                }
            }
        }

        /// <summary>
        /// Generates & plumbs in UI from UI script
        /// </summary>
        /// <param name="uiScript"></param>
        public UIScript RenderScript(string uiScript)
        {
            var path = Path.Combine(GlobalSettings.Default.StartupPath, @"gamedata/uiscripts/" + uiScript);
            var script = new UIScript(GameFacade.GraphicsDevice, this);
            script.Parse(path);
            return script;
        }

        private Texture2D AlphaBlendedScene;

        public override void PreDraw(UISpriteBatch batch)
        {
            base.PreDraw(batch);
            if (!Visible)
            {
                return;
            }
            lock (Children)
            {
                foreach (var child in Children)
                {
                    child.PreDraw(batch);
                }
            }

            /** If we have opacity, draw ourself to a texture so we can blend it later **/
            /*
            if (_HasOpacity)
            {

                Promise<Texture2D> bufferTexture = null;
                using (batch.WithBuffer(ref bufferTexture))
                {
                    lock (Children)
                    {
                        foreach (var child in Children)
                        {
                            child.Draw(batch);
                        }
                    }
                }

                AlphaBlendedScene = bufferTexture.Get();
            }
            */
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="batch"></param>
        /// <param name="mtx"></param>
        public override void Draw(UISpriteBatch batch)
        {
            if (!Visible)
            {
                return;
            }

            /**
             * If opacity is not 100% we need to draw to a texture
             * and then paint that with our opacity value
             */
            {
                lock (Children)
                {
                    foreach (var child in Children)
                    {
                        child.Draw(batch);
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="time"></param>
        public override void Update(UpdateState state)
        {
            base.Update(state);
            lock (Children)
            {
                var chCopy = new List<UIElement>(Children);
                //todo: why are all these locks here, and what kind of problems might that cause
                //also find a cleaner way to allow modification of an element's children by its own children.
                foreach (var child in chCopy)
                    child.Update(state);
            }
        }

        protected void BaseUpdate(UpdateState state)
        {
            base.Update(state);
        }

        public override void GameResized()
        {
            base.GameResized();
            lock (Children)
            {
                var chCopy = new List<UIElement>(Children);
                //
                foreach (var child in chCopy)
                    child.GameResized();
            }
        }

        public void SendToBack(params UIElement[] elements)
        {
            lock (Children)
            {
                for (int i = elements.Length - 1; i >= 0; i--)
                {
                    Children.Remove(elements[i]);
                    Children.Insert(0, elements[i]);
                }
            }
        }

        public void SendToFront(params UIElement[] elements)
        {
            lock (Children)
            {
                for (int i = elements.Length - 1; i >= 0; i--)
                {
                    Children.Remove(elements[i]);
                    Children.Add(elements[i]);
                }
            }
        }
    }
}
