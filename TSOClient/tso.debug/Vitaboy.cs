using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TSO.Common.rendering.framework;
using TSO.Common.rendering.framework.camera;
using Microsoft.Xna.Framework;
using TSO.Common.rendering.framework.shapes;
using TSO.Content;
using tso.debug.content.preview;
using TSO.Content.framework;
using TSO.Common.content;
using TSO.Vitaboy;

namespace tso.debug
{
    public partial class Vitaboy : Form
    {
        private _3DLayer _3D;
        private _3DScene Scene;
        private BasicCamera Camera;

        private Animator Animator;

        public Vitaboy()
        {
            InitializeComponent();
        }

        private void menu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            
            
        }

        private void Vitaboy_Load(object sender, EventArgs e)
        {
            try
            {
                Content.Init("F:\\Games\\Maxis\\The Sims Online\\TSOClient\\", canvas.GraphicsDevice);
            }
            catch (Exception err)
            {
                return;
            }

            Content content;
            content = Content.Get();

            foreach (var binding in content.AvatarBindings.List()){
                bindingsList.Items.Add(binding);
            }

            foreach (var outfit in content.AvatarOutfits.List()){
                outfitList.Items.Add(outfit);
            }

            foreach (var animation in content.AvatarAnimations.List()){
                animationsList.Items.Add(animation);
            }


            _3D = new _3DLayer();
            Camera = new BasicCamera(canvas.GraphicsDevice, new Vector3(10.0f, 10.0f, 10.0f), new Vector3(5.0f, 5.0f, 5.0f), Vector3.Up);
            Scene = new _3DScene(canvas.GraphicsDevice, Camera);
            _3D.Add(Scene);
            canvas.Screen.Add(_3D);

            Animator = new Animator();
            Scene.Add(Animator);
        }

        private void bindingsLoad_Click(object sender, EventArgs e){
            var binding = ((IContentReference<TSO.Vitaboy.Binding>)bindingsList.SelectedItem).Get();
            if (binding == null) { return; }

            var content = Content.Get();
            var preview = new MeshPreviewComponent();
            preview.Mesh = content.AvatarMeshes.Get(binding.MeshTypeID, binding.MeshFileID);
            if (binding.TextureFileID > 0)
            {
                preview.Texture = content.AvatarTextures.Get(binding.TextureTypeID, binding.TextureFileID);
            }
            SetPreview(preview);
        }

        private AdultVitaboyModel Avatar;

        private void outfitLoadBtn_Click(object sender, EventArgs e)
        {
            var outfit = ((IContentReference<Outfit>)outfitList.SelectedItem).Get();
            if (outfit == null) { return; }

            if (Avatar == null)
            {
                Avatar = new AdultVitaboyModel();
            }
            var content = Content.Get();
            if (outfit.Region == 0)
            {
                /** Head **/
                Avatar.Head = outfit;
            }
            else
            {
                Avatar.Body = outfit;
            }
            SetPreview(Avatar);
        }

        private _3DComponent Preview;
        private void SetPreview(_3DComponent component){
            if (this.Preview != null){
                Scene.Remove(Preview);
            }
            this.Preview = component;
            Scene.Add(component);
        }

        private void animationLoadBtn_Click(object sender, EventArgs e)
        {
            Animation animation = ((IContentReference<Animation>)animationsList.SelectedItem).Get();
            if (animation == null) { return; }
            if (Avatar == null) { return; }
            this.Animator.RunAnimation(Avatar, animation);

        }
    }
}
