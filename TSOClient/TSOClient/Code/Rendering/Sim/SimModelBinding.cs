using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimsLib.ThreeD;
using Microsoft.Xna.Framework.Graphics;
using TSOClient.Code.Data;

namespace TSOClient.Code.Rendering.Sim
{
    /// <summary>
    /// Sim's are made of body parts, each body part is a binding.
    /// A binding is made up of a mesh & a texture.
    /// </summary>
    public class SimModelBinding
    {
        public SimModelBinding(ulong bindingID)
        {
            BindingID = bindingID;
            
            var binding = SimCatalog.GetBinding(bindingID);
            Mesh = SimCatalog.GetOutfitMesh(binding.MeshAssetID);
            Texture = SimCatalog.GetOutfitTexture(binding.TextureAssetID);
        }

        public ulong BindingID;
        public Mesh Mesh;
        public Texture2D Texture;
    }
}
