//using Assimp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.IDE.Common
{
    public class MeshExporter
    {
        /*
        private static void RecursiveBoneAdd(Scene scn, Node parentNode, Vitaboy.Bone bone)
        {
            var node = new Node(bone.Name);
            if (parentNode == null)
            {
                scn.RootNode = node;
            }
        }

        public static Scene SceneGroup(
            List<Vitaboy.Mesh> meshes, 
            List<Vitaboy.Animation> animations, 
            List<Microsoft.Xna.Framework.Graphics.Texture2D> textures, 
            Vitaboy.Skeleton skel
            )
        {
            skel.ComputeBonePositions(skel.RootBone, Microsoft.Xna.Framework.Matrix.Identity);
            var scn = new Scene();
            var framerate = 1 / 50f;

            //set up the skeleton
            var nodeByBone = new Dictionary<Vitaboy.Bone, Node>();

            Func<Vitaboy.Bone, Node> addOrGetNode = null;
            addOrGetNode = (Vitaboy.Bone bone) =>
            {
                Node result;
                if (!nodeByBone.TryGetValue(bone, out result))
                {
                    result = new Node(bone.Name);

                    var myWorld = BoneMat(bone.Translation, bone.Rotation);
                    myWorld = Microsoft.Xna.Framework.Matrix.Identity;

                    result.Transform = M4(myWorld);
                    if (bone.Name == "ROOT") scn.RootNode = result;
                    else
                    {
                        var pBone = skel.GetBone(bone.ParentName);
                        result.Transform = M4(myWorld);
                        var parent = addOrGetNode(pBone);
                        parent.Children.Add(result);
                    }
                    for (int i = 0; i < meshes.Count; i++) { }
                    //    result.MeshIndices.Add(i);
                    nodeByBone[bone] = result;
                }
                return result;
            };

            foreach (var b in skel.Bones)
            {
                addOrGetNode(b);
            }

            var superRoot = new Node("<superRoot>");

            var meshNode = new Node("mesh");
            superRoot.Children.Add(meshNode);
            superRoot.Children.Add(scn.RootNode);
            scn.RootNode = superRoot;

            for (int i = 0; i < meshes.Count; i++)
                meshNode.MeshIndices.Add(i);

            var texI = 0;
            var meshI = 0;
            foreach (var mesh in meshes)
            {
                var cMesh = new Mesh(mesh.SkinName ?? "tsomesh"+(meshI++), PrimitiveType.Triangle);
                
                foreach (var vert in mesh.VertexBuffer)
                {
                    cMesh.Vertices.Add(VC(VPos(vert.Position)));
                    cMesh.Normals.Add(VC(VPos(vert.Normal)));
                    cMesh.TextureCoordinateChannels[0].Add(VC(new Microsoft.Xna.Framework.Vector3(vert.TextureCoordinate, 0)));
                }

                cMesh.SetIndices(mesh.IndexBuffer.Select(x => (int)x).ToArray(), 3);

                /*
                int[] blendVToBone = new int[mesh.BlendData.Length];
                int boneI = 0;
                foreach (var bone in mesh.BoneBindings)
                {
                    for (int i = 0; i < bone.BlendVertexCount; i++)
                        blendVToBone[i] = boneI;
                    boneI++;
                }

                int[] realVToBlendV = new int[mesh.VertexBuffer.Length];
                


                foreach (var bone in mesh.BoneBindings)
                {
                    var cBone = new Bone();
                    cBone.Name = bone.BoneName;

                    for (int i=0; i<bone.RealVertexCount; i++)
                    {
                        cBone.VertexWeights.Add(new VertexWeight(bone.FirstRealVertex + i, 1));
                    }
                    var rBone = skel.GetBone(bone.BoneName);

                    cMesh.Bones.Add(cBone);
                }

                var mat = new Material();
                mat.Name = "tsomaterial" + (meshI - 1);
                mat.ShadingMode = ShadingMode.Blinn;

                var tex = textures[meshI - 1];
                if (false) {
                    byte[] data;
                    using (var mem = new MemoryStream())
                    {
                        tex.SaveAsPng(mem, tex.Width, tex.Height);
                        data = mem.ToArray();
                    }

                    var diffuSlot = new TextureSlot("tex"+texI, TextureType.Diffuse, texI, TextureMapping.FromUV, 0, 1, TextureOperation.Add, TextureWrapMode.Wrap, TextureWrapMode.Wrap, 0);
                    scn.Textures.Add(new EmbeddedTexture("png", data));
                    mat.TextureDiffuse = diffuSlot;
                    texI++;
                }
                cMesh.MaterialIndex = scn.MaterialCount;
                scn.Materials.Add(mat);
                scn.Meshes.Add(cMesh);
            }

            foreach (var anim in animations)
            {
                var cAnim = new Animation();

                foreach (var motion in anim.Motions)
                {
                    var channel = new NodeAnimationChannel();
                    channel.NodeName = motion.BoneName;
                    var bone = skel.GetBone(motion.BoneName);
                    if (bone == null) continue;

                    var baseInv = Microsoft.Xna.Framework.Matrix.Invert(BoneMat(bone.Translation, bone.Rotation));
                    for (int i = 0; i < motion.FrameCount; i++)
                    {
                        var trans = (motion.HasTranslation) ? anim.Translations[i + motion.FirstTranslationIndex] : bone.Translation;
                        var quat = (motion.HasRotation) ? anim.Rotations[i + motion.FirstRotationIndex] : bone.Rotation;
                        var scale = Microsoft.Xna.Framework.Vector3.One;

                        var diffMat = BoneMat(trans, quat) * baseInv;


                        //diffMat.Decompose(out scale, out quat, out trans);
                        channel.PositionKeys.Add(new VectorKey(i * framerate, VC(VHand(trans))));
                        channel.RotationKeys.Add(new QuaternionKey(i * framerate, VQ(QHand(quat))));
                        channel.ScalingKeys.Add(new VectorKey(i * framerate, new Vector3D(1, 1, 1)));
                    }

                    cAnim.NodeAnimationChannels.Add(channel);
                }
                cAnim.Name = anim.Name;
                cAnim.TicksPerSecond = 50f;
                cAnim.DurationInTicks = anim.Motions.Max(x => x.FrameCount);

                scn.Animations.Add(cAnim);
            }

            return scn;
        }

        private static Microsoft.Xna.Framework.Matrix BoneMat(
            Microsoft.Xna.Framework.Vector3 trans,
            Microsoft.Xna.Framework.Quaternion quat)
        {
            var translateMatrix = Microsoft.Xna.Framework.Matrix.CreateTranslation(trans);
            var rotationMatrix = Microsoft.Xna.Framework.Matrix.CreateFromQuaternion(quat);

            return (rotationMatrix * translateMatrix);
        }

        private static Matrix4x4 M4(Microsoft.Xna.Framework.Matrix mat)
        {
            var result = new Matrix4x4(
            mat.M11, mat.M21, mat.M31, mat.M41,
            mat.M12, mat.M22, mat.M32, mat.M42,
            mat.M13, mat.M23, mat.M33, mat.M43,
            mat.M14, mat.M24, mat.M34, mat.M44
            );

            //result.Transpose();
            return result;
        }

        private static Quaternion VQ(Microsoft.Xna.Framework.Quaternion quat)
        {
            return new Quaternion(quat.W, quat.X, quat.Y, quat.Z);
        }

        private static Vector3D VC(Microsoft.Xna.Framework.Vector3 vec)
        {
            return new Vector3D(vec.X, vec.Y, vec.Z);
        }

        private static Microsoft.Xna.Framework.Vector3 VPos(Microsoft.Xna.Framework.Vector3 vec)
        {
            return new Microsoft.Xna.Framework.Vector3(vec.Z, -vec.X, vec.Y);
            //return new Microsoft.Xna.Framework.Vector3(vec.X, vec.Z, vec.Y);
        }

        private static Microsoft.Xna.Framework.Vector3 VHand(Microsoft.Xna.Framework.Vector3 vec)
        {
            return VPos(vec);
            //return new Microsoft.Xna.Framework.Vector3(vec.X, vec.Z, vec.Y);
        }

        private static Microsoft.Xna.Framework.Quaternion QHand(Microsoft.Xna.Framework.Quaternion quat)
        {
            return new Microsoft.Xna.Framework.Quaternion(-quat.Z, quat.X, -quat.Y, quat.W);
            //return new Microsoft.Xna.Framework.Quaternion(-quat.X, -quat.Z, -quat.Y, quat.W);
        }

        public static void ExportToFBX(Scene scn, string path)
        {
            var context = new AssimpContext();
            var sceneKid = context.ImportFile(Path.Combine(Path.GetDirectoryName(path), "boblampclean.md5mesh"));
            
            context.ExportFile(scn, path, "collada");
        }
    */
    }
}
