using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Client.UI.Controls;
using Microsoft.Xna.Framework;

namespace FSO.Client.UI.Framework
{
    /// <summary>
    /// A container that allows for a rectangle area (a mask) to be specificed as screen limit bounds for drawing.
    /// By checking the specificed area (mask) against the position, width, and height of each of its children, the class
    /// directs the children (UIElement.cs) to update their texture source nullable rectangle, as seen as "from?" in Draw calls.
    /// Note that this class automatically centers the child images and makes all calculations by the textures' center.
    /// </summary>
    public class UIMaskedContainer : UIContainer
    {
        private Rectangle m_Mask;

        public UIMaskedContainer()
        {
        }
        public UIMaskedContainer(Rectangle mask)
        {
            m_Mask = mask;
        }
        /// <summary>
        /// When the mask is reset, the children's texture source masks must be reset
        /// </summary>
        public Rectangle Mask
        {
            set
            {
                m_Mask = value;
                UpdateChildMasks();
            }
        }
        /// <summary>
        /// Check each child's position, width, and height in order to determine which part of their respective textures
        /// should be used to limit their draw boundaries.
        /// </summary>
        public void UpdateChildMasks()
        {
            var children = new List<UIElement>(Children);
            foreach (var child in children)
            {
                UpdateChildMask(child as UIImage);
            }
        }
        /// <summary>
        /// Check child's position, width, and height in order to determine which part of their respective textures
        /// should be used to limit their draw boundaries.
        /// </summary>
        public void UpdateChildMask(UIImage child)
        {
            float fromX;
            float fromY;
            float fromWidth = 0;
            float fromHeight = 0;
            if (m_Mask == null || child == null || child.Texture == null) 
                child.SourceRectangle = null;
            else
            {
                child.X = child.Y = 0;
                var scaledWidth = child.Texture.Width * child.ScaleX;
                var scaledMaskWidth = m_Mask.Width / child.ScaleX;
                var scaledHeight = child.Texture.Height * child.ScaleY;
                var scaledMaskHeight = m_Mask.Height / child.ScaleY;
                var scaledAbstractX = child.AbstractX / child.ScaleX;
                var scaledAbstractY = child.AbstractY / child.ScaleY;
                // force the child to be centered with the mask. the center of the mask should match the center of the child's texture
                var texCenter = new Vector2(child.Texture.Width / 2f, child.Texture.Height / 2f);

                if (scaledWidth > m_Mask.Width)
                    fromX = texCenter.X - m_Mask.Width / 2f / child.ScaleX - scaledAbstractX;
                else
                {
                    fromX = 0 - scaledAbstractX;
                    child.X = (m_Mask.Width - scaledWidth) / 2f;
                }
                if (scaledHeight > m_Mask.Height)
                    fromY = texCenter.Y - m_Mask.Height / 2f / child.ScaleY - scaledAbstractY;
                else
                {
                    fromY = 0 - scaledAbstractY;
                    child.Y = (m_Mask.Height - scaledHeight) / 2f;
                }

                // source rectangle width and height can never be larger than the texture's width and height
                // source rectangle width and height can never be larger than the mask's width and height
                if (fromX >= child.Texture.Width)
                {
                    fromX -= child.X / child.ScaleX;
                    child.X = 0;
                }
                if (fromY >= child.Texture.Height)
                {
                    fromY -= child.Y / child.ScaleY;
                    child.Y = 0;
                }
                if (fromX <= 0)
                {
                    child.X += Math.Abs(fromX) * child.ScaleX;
                    fromX = 0;
                }
                fromWidth = Math.Min(Math.Max((m_Mask.Width - child.X) / child.ScaleX, 0), Math.Min(scaledMaskWidth, child.Texture.Width - fromX));
                if (fromY <= 0)
                {
                    child.Y += Math.Abs(fromY) * child.ScaleY;
                    fromY = 0;
                }
                fromHeight = Math.Min(Math.Max((m_Mask.Height - child.Y) / child.ScaleY, 0), Math.Min(scaledMaskHeight, child.Texture.Height - fromY));

                // source rectangle X and Y cannot be less than zero because we never start drawing before the texture's origin
                if (fromWidth < 0)
                    fromWidth = 0;
                if (fromHeight < 0)
                    fromHeight = 0;
                child.SourceRectangle = new Rectangle((int)Math.Round(fromX, 2), (int)Math.Round(fromY, 2), (int)fromWidth, (int)fromHeight);
            }
            child.Invalidate();
        }
        /// <summary>
        /// Override all Add() functions to ensure the added child has its mask updated immediately
        /// </summary>
        public override void Add(UIElement child)
        {
            base.Add(child);
            if (m_Mask != null)
                UpdateChildMask(child as UIImage);
        }
        public override void AddAt(int index, UIElement child)
        {
            base.Add(child);
            if (m_Mask != null)
                UpdateChildMask(child as UIImage);
        }
        public override void AddBefore(UIElement child, UIElement before)
        {
            base.Add(child);
            if (m_Mask != null)
                UpdateChildMask(child as UIImage);
        }
    }
}
