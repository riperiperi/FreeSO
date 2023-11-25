using FSO.Content.Framework;
using FSO.Content.Model;
using FSO.Files.Utils;
using FSO.Vitaboy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FSO.IDE.Utils
{
    public class AppearanceGenerator
    {
        public Dictionary<ArraySegment<byte>, ulong> TextureToID = new Dictionary<ArraySegment<byte>, ulong>();
        public Dictionary<Mesh, ulong> MeshToID = new Dictionary<Mesh, ulong>();
        
        public ulong GenerateAppearanceTSO(List<ImportMeshGroup> meshes, string name, bool runtime)
        {
            var bindings = meshes.Select(meshG =>
            {
                var mesh = meshG.Mesh;
                var textureData = meshG.TextureData;

                ulong texID = 0;
                if (textureData != null && !TextureToID.TryGetValue(textureData.Value, out texID)) {
                    var texDataArray = textureData.Value.ToArray();
                    var texRef = new InMemoryTextureRef(texDataArray);
                    texID = (Content.Content.Get().AvatarTextures as TSOAvatarContentProvider<ITextureRef>).CreateFile(name + ".png", texRef, texDataArray, runtime);
                    TextureToID[textureData.Value] = texID;
                }

                ulong meshID;
                if (!MeshToID.TryGetValue(mesh, out meshID)) {
                    byte[] meshData;
                    using (var mem = new MemoryStream())
                    {
                        using (var io = IoWriter.FromStream(mem, ByteOrder.BIG_ENDIAN))
                        {
                            mesh.Write((BCFWriteProxy)io, false);
                            meshData = mem.ToArray();
                        }
                    }
                    meshID = (Content.Content.Get().AvatarMeshes as TSOAvatarContentProvider<Mesh>).CreateFile(name + ".mesh", mesh, meshData, runtime);
                    MeshToID[mesh] = meshID;
                }

                var binding = new Binding()
                {
                    Bone = mesh.BoneBindings.FirstOrDefault()?.BoneName ?? "ROOT",
                    MeshFileID = (uint)(meshID >> 32),
                    MeshTypeID = (uint)meshID,
                    MeshGroupID = 0,
                    //MeshName = name + ".mesh",
                    TextureFileID = (uint)(texID >> 32),
                    TextureTypeID = (uint)texID,
                    TextureGroupID = 0,
                    //TextureName = name + ".png"
                };

                byte[] bindingData;
                using (var mem = new MemoryStream())
                {
                    binding.Write(mem);
                    bindingData = mem.ToArray();
                }
                var bindingID = (Content.Content.Get().AvatarBindings as TSOAvatarContentProvider<Binding>).CreateFile(name + ".bnd", binding, bindingData, runtime);

                return new AppearanceBinding()
                {
                    FileID = (uint)(bindingID >> 32),
                    TypeID = (uint)bindingID
                };
            });

            var apr = new Appearance()
            {
                Bindings = bindings.ToArray(),
                Name = name,
                Type = 0 //bcf value? not currently used
            };

            byte[] appearanceData;
            using (var mem = new MemoryStream())
            {
                apr.Write(mem);
                appearanceData = mem.ToArray();
            }
            var aprID = (Content.Content.Get().AvatarAppearances as TSOAvatarContentProvider<Appearance>).CreateFile(name + ".apr", apr, appearanceData, runtime);

            return aprID;
        }
    }

    public delegate ulong SaveFunction<T>(TSOAvatarContentProvider<T> content, string filename, T obj, byte[] data);
}
