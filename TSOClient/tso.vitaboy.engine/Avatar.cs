/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Common.rendering.framework;
using Microsoft.Xna.Framework.Graphics;
using TSO.Content;
using Microsoft.Xna.Framework;

namespace TSO.Vitaboy
{
    /// <summary>
    /// The base class for all avatars in the game.
    /// </summary>
    public abstract class Avatar : _3DComponent 
    {
        public List<AvatarBindingInstance> Bindings = new List<AvatarBindingInstance>();
        public static Effect Effect;
        public Skeleton Skeleton { get; internal set; }
        public Skeleton BaseSkeleton { get; internal set; }
        private Matrix[] SkelBones;

        public static void setVitaboyEffect(Effect e) {
            Effect = e;
        }

        /// <summary>
        /// Creates a new Avatar instance.
        /// </summary>
        /// <param name="skel">A Skeleton instance.</param>
        public Avatar(Skeleton skel)
        {
            this.Skeleton = skel.Clone();
            this.BaseSkeleton = skel.Clone(); //keep a copy we can revert back to
        }

        public Avatar(Avatar old)
        {
            this.BaseSkeleton = old.BaseSkeleton.Clone();
            this.Skeleton = old.BaseSkeleton.Clone();
            for (int i = 0; i < old.Bindings.Count(); i++)
            {
                AvatarBindingInstance oldb = old.Bindings[i];
                Bindings.Add(new AvatarBindingInstance()
                {
                    Mesh = oldb.Mesh,
                    Texture = oldb.Texture
                });
            }
            for (int i = 0; i < old.Accessories.Count(); i++)
            {
                this.Accessories.Add(old.Accessories.Keys.ElementAt(i), old.Accessories.Values.ElementAt(i));
            } //just shallow copy the binding and accessory list, as the data inside isn't going to change any time soon...
        }

        private Dictionary<Appearance, AvatarAppearanceInstance> Accessories = new Dictionary<Appearance, AvatarAppearanceInstance>();
        
        /// <summary>
        /// Adds an accessory to this avatar.
        /// </summary>
        /// <param name="apr">The Appearance instance of the accessory.</param>
        public void AddAccessory(Appearance apr)
        {
            if (Accessories.ContainsKey(apr))
            {
                return;
            }

            var add = AddAppearance(apr);
            Accessories.Add(apr, add);
        }

        /// <summary>
        /// Remove an accessory from this avatar.
        /// </summary>
        /// <param name="apr">The Appearance of the accessory to remove.</param>
        public void RemoveAccessory(Appearance apr)
        {
            if (Accessories.ContainsKey(apr))
            {
                RemoveAppearance(Accessories[apr], true);
                Accessories.Remove(apr);
            }
        }

        private bool GPUMode;
        private GraphicsDevice GPUDevice;
        public void StoreOnGPU(GraphicsDevice device)
        {
            GPUMode = true;
            GPUDevice = device;
            foreach (var binding in Bindings)
            {
                binding.Mesh.StoreOnGPU(device);
            }
        }

        /// <summary>
        /// Adds an Appearance instance to this avatar.
        /// </summary>
        /// <param name="appearance">The Appearance instance to add.</param>
        /// <returns>An AvatarAppearanceInstance instance.</returns>
        protected AvatarAppearanceInstance AddAppearance(Appearance appearance)
        {
            var result = new AvatarAppearanceInstance();
            result.Bindings = new List<AvatarBindingInstance>();

            foreach (var bindingReference in appearance.Bindings)
            {
                var binding = TSO.Content.Content.Get().AvatarBindings.Get(bindingReference.TypeID, bindingReference.FileID);
                if (binding == null) { continue; }
                result.Bindings.Add(AddBinding(binding));
            }

            return result;
        }

        /// <summary>
        /// Removes an Appearance instance from this avatar.
        /// </summary>
        /// <param name="appearance">The Appearance instance to remove.</param>
        /// <param name="dispose">Should the appearance be disposed?</param>
        public void RemoveAppearance(AvatarAppearanceInstance appearance, bool dispose)
        {
            foreach (var binding in appearance.Bindings)
            {
                RemoveBinding(binding, dispose);
            }
        }

        /// <summary>
        /// Adds a Binding instance to this avatar.
        /// </summary>
        /// <param name="binding">The Binding instance to add.</param>
        /// <returns>An AvatarBindingInstance instance.</returns>
        protected AvatarBindingInstance AddBinding(Binding binding)
        {
            var content = TSO.Content.Content.Get();
            var instance = new AvatarBindingInstance();
            instance.Mesh = content.AvatarMeshes.Get(binding.MeshTypeID, binding.MeshFileID);

            /*if (instance.Mesh != null)
            {
                //We make a copy so we can modify it, most of the variables
                //are kept as pointers because we only change a few locals
                //per sim, the rest are global
                instance.Mesh = instance.Mesh.Clone();
            }*/

            if (binding.TextureFileID > 0)
            {
                instance.Texture = content.AvatarTextures.Get(binding.TextureTypeID, binding.TextureFileID);
            }

            instance.Mesh.Prepare(Skeleton.RootBone);

            if (GPUMode)
            {
                instance.Mesh.StoreOnGPU(GPUDevice);
            }
            Bindings.Add(instance);
            return instance;
        }

        /// <summary>
        /// Removes a Binding instance from this avatar.
        /// </summary>
        /// <param name="instance">The Binding instance to remove.</param>
        /// <param name="dispose">Should the binding be disposed?</param>
        protected void RemoveBinding(AvatarBindingInstance instance, bool dispose)
        {
            Bindings.Remove(instance);
        }

        /// <summary>
        /// Initializes this Avatar instance.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
        }

        /// <summary>
        /// When the skeleton changes (for example due to an animation) this
        /// method will recompute the meshes to adhere to the new skeleton positions.
        /// </summary>
        public void ReloadSkeleton()
        {
            Skeleton.ComputeBonePositions(Skeleton.RootBone, Matrix.Identity);
            SkelBones = new Matrix[Skeleton.Bones.Length];
            for (int i = 0; i < Skeleton.Bones.Length; i++)
            {
                SkelBones[i] = Skeleton.Bones[i].AbsoluteMatrix;
            }
        }

        /// <summary>
        /// Updates this Avatar instance.
        /// </summary>
        /// <param name="state">An UpdateState instance.</param>
        public override void Update(TSO.Common.rendering.framework.model.UpdateState state)
        {
        }

        /// <summary>
        /// Draws the meshes making up this Avatar instance.
        /// </summary>
        /// <param name="device">A GraphicsDevice instance.</param>
        public override void Draw(Microsoft.Xna.Framework.Graphics.GraphicsDevice device)
        {
            Effect.CurrentTechnique = Effect.Techniques[0];
            Effect.Parameters["View"].SetValue(View);
            Effect.Parameters["Projection"].SetValue(Projection);
            Effect.Parameters["World"].SetValue(World);

            DrawGeometry(device, Effect);
        }

        public void DrawGeometry(Microsoft.Xna.Framework.Graphics.GraphicsDevice device, Effect effect)
        {
            //Effect.CurrentTechnique = Effect.Techniques[0];
            if (SkelBones == null) ReloadSkeleton();
            effect.Parameters["SkelBindings"].SetValue(SkelBones);

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                foreach (var binding in Bindings)
                {
                    effect.Parameters["MeshTex"].SetValue(binding.Texture);
                    pass.Apply();
                    binding.Mesh.Draw(device);
                }
            }
        }

        public override void DeviceReset(GraphicsDevice Device)
        {
        }
    }

    /// <summary>
    /// Holds bindings for an avatar.
    /// </summary>
    public class AvatarAppearanceInstance
    {
        public List<AvatarBindingInstance> Bindings;
    }

    /// <summary>
    /// Holds a mesh and texture for an avatar.
    /// </summary>
    public class AvatarBindingInstance 
    {
        public Mesh Mesh;
        public Texture2D Texture;
    }
}
