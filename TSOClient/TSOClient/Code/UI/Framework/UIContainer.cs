using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using TSOClient.Code.UI.Model;

namespace TSOClient.Code.UI.Framework
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

        /// <summary>
        /// Adds a UIElement at the top most position in the container
        /// </summary>
        /// <param name="child"></param>
        public void Add(UIElement child)
        {
            Children.Add(child);
            child.Parent = this;
        }

        /// <summary>
        /// Adds a UIElement at a specific depth in the container
        /// </summary>
        /// <param name="index"></param>
        /// <param name="child"></param>
        public void AddAt(int index, UIElement child)
        {
            Children.Insert(index, child);
            child.Parent = this;
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
            foreach (var child in Children)
            {
                child.InvalidateMatrix();
            }
        }

        protected override void CalculateOpacity()
        {
            base.CalculateOpacity();

            /**
             * If our matrix has changed, then our children's matrixes will have to as well
             */
            foreach (var child in Children)
            {
                child.InvalidateOpacity();
            }
        }


        /// <summary>
        /// Generates & plumbs in UI from UI script
        /// </summary>
        /// <param name="uiScript"></param>
        /*public void RenderScript(GraphicsDevice gd, string uiScript)
        {
            var path = Path.Combine(GlobalSettingsDummy.StartupPath, @"gamedata\uiscripts\" + uiScript);
            var script = new UIScript(gd, this);
            script.Parse(path);
        }*/

        /// <summary>
        /// 
        /// </summary>
        /// <param name="batch"></param>
        /// <param name="mtx"></param>
        public override void Draw(SpriteBatch batch)
        {
            if (!Visible)
            {
                return;
            }
            foreach (var child in Children)
            {
                child.Draw(batch);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="time"></param>
        public override void Update(UpdateState state)
        {
            if (!Visible)
            {
                return;
            }

            base.Update(state);
            foreach (var child in Children)
            {
                child.Update(state);
            }
        }
    }
}
