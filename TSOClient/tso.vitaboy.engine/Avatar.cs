using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.common.rendering.framework;
using Microsoft.Xna.Framework.Graphics;
using tso.content;
using Microsoft.Xna.Framework;

namespace tso.vitaboy
{
    public abstract class Avatar : _3DComponent {

        public List<AvatarBindingInstance> Bindings = new List<AvatarBindingInstance>();
        protected BasicEffect Effect;
        public Skeleton Skeleton { get; internal set; }

        public Avatar(Skeleton skel){
            this.Skeleton = skel.Clone();
        }



        private Dictionary<Appearance, AvatarAppearanceInstance> Accessories = new Dictionary<Appearance, AvatarAppearanceInstance>();
        public void AddAccessory(Appearance apr){
            if (Accessories.ContainsKey(apr))
            {
                return;
            }
            var add = AddAppearance(apr);
            Accessories.Add(apr, add);
        }

        public void RemoveAccessory(Appearance apr){
            if (Accessories.ContainsKey(apr))
            {
                RemoveAppearance(Accessories[apr], true);
                Accessories.Remove(apr);
            }
        }


        private bool GPUMode;
        private GraphicsDevice GPUDevice;
        public void StoreOnGPU(GraphicsDevice device){
            GPUMode = true;
            GPUDevice = device;
            foreach (var binding in Bindings){
                binding.Mesh.StoreOnGPU(device);
            }
        }

        protected AvatarAppearanceInstance AddAppearance(Appearance appearance)
        {
            var result = new AvatarAppearanceInstance();
            result.Bindings = new List<AvatarBindingInstance>();

            foreach (var bindingReference in appearance.Bindings)
            {
                var binding = Content.Get().AvatarBindings.Get(bindingReference.TypeID, bindingReference.FileID);
                if (binding == null) { continue; }
                result.Bindings.Add(AddBinding(binding));
            }

            return result;
        }

        public void RemoveAppearance(AvatarAppearanceInstance appearance, bool dispose)
        {
            foreach (var binding in appearance.Bindings)
            {
                RemoveBinding(binding, dispose);
            }
        }

        protected AvatarBindingInstance AddBinding(Binding binding){
            var content = Content.Get();
            var instance = new AvatarBindingInstance();
            instance.Mesh = content.AvatarMeshes.Get(binding.MeshTypeID, binding.MeshFileID);
            if (instance.Mesh != null)
            {
                //We make a copy so we can modify it, most of the variables
                //are kept as pointers because we only change a few locals
                //per sim, the rest are global
                instance.Mesh = instance.Mesh.Clone();
            }
            if (binding.TextureFileID > 0){
                instance.Texture = content.AvatarTextures.Get(binding.TextureTypeID, binding.TextureFileID);
            }
            MeshTransformer.Transform(instance.Mesh, Skeleton.RootBone);
            if (GPUMode)
            {
                instance.Mesh.StoreOnGPU(GPUDevice);
            }
            Bindings.Add(instance);
            return instance;
        }

        protected void RemoveBinding(AvatarBindingInstance instance, bool dispose){
            Bindings.Remove(instance);
        }

        public override void Initialize(){
            base.Initialize();

            Effect = new BasicEffect(Device, null);
            Effect.TextureEnabled = true;
        }

        /// <summary>
        /// When the skeleton changes (for example due to an animation) this
        /// method will recompute the meshes to adhere to the new skeleton positions
        /// </summary>
        public void InvalidateSkeleton(){
            Skeleton.ComputeBonePositions(Skeleton.RootBone, Matrix.Identity);
            foreach (var binding in Bindings){
                MeshTransformer.Transform(binding.Mesh, Skeleton.RootBone);
            }
        }

        public override void Update(tso.common.rendering.framework.model.UpdateState state){
        }

        public override void Draw(Microsoft.Xna.Framework.Graphics.GraphicsDevice device){
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
    }


    public class AvatarAppearanceInstance
    {
        public List<AvatarBindingInstance> Bindings;
    }

    public class AvatarBindingInstance {
        public Mesh Mesh;
        public Texture2D Texture;
    }
}
