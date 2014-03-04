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
        protected BasicEffect Effect;
        public Skeleton Skeleton { get; internal set; }

        /// <summary>
        /// Creates a new Avatar instance.
        /// </summary>
        /// <param name="skel">A Skeleton instance.</param>
        public Avatar(Skeleton skel)
        {
            this.Skeleton = skel.Clone();
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

            if (instance.Mesh != null)
            {
                //We make a copy so we can modify it, most of the variables
                //are kept as pointers because we only change a few locals
                //per sim, the rest are global
                instance.Mesh = instance.Mesh.Clone();
            }

            if (binding.TextureFileID > 0)
            {
                instance.Texture = content.AvatarTextures.Get(binding.TextureTypeID, binding.TextureFileID);
            }

            instance.Mesh.Transform(Skeleton.RootBone);

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

            Effect = new BasicEffect(Device, null);
            Effect.TextureEnabled = true;
            Effect.EnableDefaultLighting();
            Effect.CommitChanges();
        }

        /// <summary>
        /// When the skeleton changes (for example due to an animation) this
        /// method will recompute the meshes to adhere to the new skeleton positions.
        /// </summary>
        public void ReloadSkeleton()
        {
            Skeleton.ComputeBonePositions(Skeleton.RootBone, Matrix.Identity);
            foreach (var binding in Bindings)
                binding.Mesh.Transform(Skeleton.RootBone);
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
            Effect.View = View;
            Effect.Projection = Projection;
            Effect.World = World;

            Effect.Begin();
            foreach (var pass in Effect.CurrentTechnique.Passes)
            {
                pass.Begin();
                foreach (var binding in Bindings)
                {
                    Effect.Texture = binding.Texture;
                    Effect.CommitChanges();
                    binding.Mesh.Draw(device);
                }
                pass.End();
            }
            Effect.End();
        }

        public override void DeviceReset(GraphicsDevice Device)
        {
        }
    }

    public class AvatarAppearanceInstance
    {
        public List<AvatarBindingInstance> Bindings;
    }

    public class AvatarBindingInstance 
    {
        public Mesh Mesh;
        public Texture2D Texture;
    }
}
