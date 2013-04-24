using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using TSOClient.VM;
using SimsLib.ThreeD;
using Microsoft.Xna.Framework.Graphics;

namespace TSOClient.Code.Data
{
    /// <summary>
    /// Place to get information and assets related to sims, e.g. skins, thumbnails etc
    /// </summary>
    public class SimCatalog
    {
        public static void GetCollection(ulong fileID)
        {
            var collectionData = ContentManager.GetResourceFromLongID(fileID);
            var reader = new BinaryReader(new MemoryStream(collectionData));
        }

        public SimCatalog()
        {
        }

        private static Dictionary<ulong, Outfit> Outfits = new Dictionary<ulong, Outfit>();
        public static Outfit GetOutfit(ulong id)
        {
            if (Outfits.ContainsKey(id))
            {
                return Outfits[id];
            }

            var bytes = ContentManager.GetResourceFromLongID(id);
            var outfit = new Outfit(bytes);
            Outfits.Add(id, outfit);
            return outfit;
        }

        private static Dictionary<ulong, Texture2D> OutfitTextures = new Dictionary<ulong, Texture2D>();
        public static Texture2D GetOutfitTexture(ulong id)
        {
            if (OutfitTextures.ContainsKey(id))
            {
                return OutfitTextures[id];
            }

            var bytes = ContentManager.GetResourceFromLongID(id);
            using (var stream = new MemoryStream(bytes))
            {
                var texture = Texture2D.FromFile(GameFacade.GraphicsDevice, stream);
                OutfitTextures.Add(id, texture);
                return texture;
            }
        }

        private static Dictionary<ulong, Mesh> OutfitMeshes = new Dictionary<ulong, Mesh>();
        public static Mesh GetOutfitMesh(Skeleton Skel, ulong id)
        {
            if (OutfitMeshes.ContainsKey(id))
            {
                return OutfitMeshes[id];
            }
            
            var mesh = new Mesh();
            mesh.Read(ContentManager.GetResourceFromLongID(id));
            mesh.ProcessMesh(Skel);
            OutfitMeshes.Add(id, mesh);
            return mesh;
        }

        public static void LoadSim3D(Sim sim, Outfit OutfHead, AppearanceType skin)
        {
            var Apr = new Appearance(ContentManager.GetResourceFromLongID(OutfHead.GetAppearance(skin)));
            var Bnd = new Binding(ContentManager.GetResourceFromLongID(Apr.BindingIDs[0]));

            sim.HeadTexture = GetOutfitTexture(Bnd.TextureAssetID);
            sim.HeadMesh = GetOutfitMesh(sim.SimSkeleton, Bnd.MeshAssetID);
        }
    }
}
