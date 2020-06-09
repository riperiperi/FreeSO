using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SharpGLTF;
using SharpGLTF.Animations;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Scenes;
using SharpGLTF.Schema2;

namespace FSO.IDE.Utils
{
    public class GLTFExporter
    {
        public static Quaternion RotateQ = Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), (float)Math.PI / -2f) 
                                          * Quaternion.CreateFromAxisAngle(new Vector3(1, 0, 0), (float)Math.PI / -2f);

        public static Matrix4x4 RotateM = Matrix4x4.CreateFromAxisAngle(new Vector3(1, 0, 0), (float)Math.PI / -2f) 
                                         * Matrix4x4.CreateFromAxisAngle(new Vector3(0, 1, 0), (float)Math.PI / -2f);

        public static Microsoft.Xna.Framework.Matrix RotateMX =
            Microsoft.Xna.Framework.Matrix.CreateFromAxisAngle(new Microsoft.Xna.Framework.Vector3(0, 1, 0), (float) Math.PI / -2f) *
            Microsoft.Xna.Framework.Matrix.CreateFromAxisAngle(new Microsoft.Xna.Framework.Vector3(0, 0, 1), (float) Math.PI / 2f);

        public static float MeshScale = 1 / 3f;
        
        //negative y becomes positive z
        //positive z becomes positive y
        private Quaternion QuatConvert(Microsoft.Xna.Framework.Quaternion quat)
        {
            return new Quaternion(-quat.Z, -quat.X, quat.Y, quat.W); // x mirror
            //return new Quaternion(-quat.Z, quat.X, -quat.Y, quat.W); //good

        }

        private Vector3 Vec3Convert(Microsoft.Xna.Framework.Vector3 vec)
        {
            return new Vector3(-vec.Z, -vec.X, vec.Y) * MeshScale; //x mirror
            //return new Vector3(vec.Z, -vec.X, vec.Y);
        }

        private float RecursiveScale(NodeBuilder elem)
        {
            if (elem == null) return 1f;
            return (elem.Parent == null) ? (elem.Scale?.Value.X ?? 1f) : (elem.Scale?.Value.X ?? 1f) * RecursiveScale(elem.Parent);
        }

        private void ConvertBone(Dictionary<Vitaboy.Bone, NodeBuilder> all, Vitaboy.Bone bone, NodeBuilder target)
        {
            all[bone] = target;
            var len = (bone.Children.Length == 0) ? 0.1f : bone.Children.Where(x => x.Translation.Length() > 0).Average(x => x.Translation.Length() * MeshScale);
            if (len == 0) len = 0.1f;
            var recursiveScale = RecursiveScale(target);
            if (target.Parent != null) len = len / recursiveScale;
            //var len = Vec3Convert(bone.Translation).Length();
            var quat = QuatConvert(bone.Rotation);
            var trans = Vec3Convert(bone.Translation) / recursiveScale;
            if (bone.Name == "ROOT")
            {
                quat = RotateQ * quat;
                trans = Vector3.Transform(trans, RotateM);
            }
            target.WithLocalRotation(quat)
                  .WithLocalTranslation(trans)
                  .WithLocalScale(Vector3.One * len);

            foreach (var child in bone.Children)
            {
                ConvertBone(all, child, target.CreateNode(child.Name));
            }
        }

        private byte[] TextureToPng(Microsoft.Xna.Framework.Graphics.Texture2D tex)
        {
            var bmp = new Bitmap(tex.Width, tex.Height, PixelFormat.Format32bppArgb);

            var bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, bmp.PixelFormat);
            IntPtr ptr = bmpData.Scan0;

            var data = new byte[tex.Width * tex.Height * 4];
            tex.GetData(data);

            for (int i = 0; i < data.Length; i += 4)
            {
                var swap = data[i];
                data[i] = data[i + 2];
                data[i + 2] = swap;
            }

            Marshal.Copy(data, 0, ptr, bmpData.Stride * bmpData.Height);
            bmp.UnlockBits(bmpData);
            
            using (var mem = new MemoryStream())
            {
                bmp.Save(mem, ImageFormat.Png);
                return mem.ToArray();
            }
        }

        public ModelRoot SceneGroup(
            List<Vitaboy.Mesh> meshes,
            List<Vitaboy.Animation> animations,
            List<Microsoft.Xna.Framework.Graphics.Texture2D> textures,
            Vitaboy.Skeleton skel
            )
            {
            var builder = new SharpGLTF.Scenes.SceneBuilder();

            var framerate = 1 / 36f;

            var nodes = new Dictionary<Vitaboy.Bone, NodeBuilder>();
            var transforms = new List<Matrix4x4>();
            skel.ComputeBonePositions(skel.RootBone, Microsoft.Xna.Framework.Matrix.Identity);

            ConvertBone(nodes, skel.RootBone, new NodeBuilder(skel.RootBone.Name));

            //animations must be uploaded as part of the bone. add them as animation tracks to the existing nodes

            var timeprops = new Dictionary<string, float>();

            var useAnimID = animations.Count > 1;

            foreach (var anim in animations)
            {
                var name = anim.XSkillName ?? anim.Name;
                foreach (var motion in anim.Motions)
                {
                    //find the bone we're creating a curve for
                    var bone = nodes.Values.FirstOrDefault(x => x.Name == motion.BoneName);
                    if (bone == null) continue; //cannot add this curve to a bone
                    var root = bone.Name == "ROOT";
                    var scale = RecursiveScale(bone.Parent);

                    if (motion.TimeProperties != null)
                    {
                        foreach (var tps in motion.TimeProperties)
                        {
                            foreach (var tp in tps.Items)
                            {
                                foreach (var prop in tp.Properties.Items)
                                {
                                    foreach (var keyPair in prop.KeyPairs)
                                    {
                                        var tpname = name + "/" + tp.ID.ToString() + "/" + keyPair.Key;
                                        float value = 1;
                                        if (!float.TryParse(keyPair.Value, out value))
                                        {
                                            value = 1;
                                            tpname += "=" + keyPair.Value;
                                        }
                                        timeprops[tpname] = value;
                                    }
                                }
                            }
                        }
                    }

                    //create curves for rotation and translation
                    
                    CurveBuilder<Vector3> transCurve = null;
                    CurveBuilder<Quaternion> rotCurve = null;
                    if (motion.HasTranslation) transCurve = bone.Translation.UseTrackBuilder(name);
                    if (motion.HasRotation) rotCurve = bone.Rotation.UseTrackBuilder(name);

                    for (int i = 0; i < motion.FrameCount; i++)
                    {
                        var quat = new Quaternion();
                        Vector3 trans = new Vector3();
                        if (rotCurve != null) quat = QuatConvert(anim.Rotations[i + motion.FirstRotationIndex]);
                        if (transCurve != null) trans = Vec3Convert(anim.Translations[i + motion.FirstTranslationIndex]) / scale;
                        if (root)
                        {
                            quat = RotateQ * quat;
                            trans = Vector3.Transform(trans, RotateM);
                        }
                        rotCurve?.SetPoint(i * framerate, quat, true);
                        transCurve?.SetPoint(i * framerate, trans, true);
                    }
                    if (bone.Name == "R_LEG") { }
                }
            }

            var resultMeshes = new List<MeshBuilder<VertexPositionNormal, VertexTexture1, VertexJoints8x4>>();

            var orderedBones = skel.Bones.Select(x => nodes[x]).ToArray();
            var world = Matrix4x4.Identity; //Matrix4x4.CreateFromAxisAngle(new Vector3(0, 1, 0), (float)Math.PI / 2f);
            var outMesh = new MeshBuilder<VertexPositionNormal, VertexTexture1, VertexJoints8x4>("Avatar");

            var meshi = 0;
            foreach (var mesh in meshes) {
                var tex = textures[meshi++];
                var data = TextureToPng(tex);

                var material = new MaterialBuilder(mesh.SkinName ?? ("mesh_" + meshi));
                var mtex = material.UseChannel(KnownChannels.BaseColor).UseTexture().WithPrimaryImage(new ArraySegment<byte>(data));
                var prim = outMesh.UsePrimitive(material, 3);

                //todo: blend verts
                var previous = new List<VertexBuilder<VertexPositionNormal, VertexTexture1, VertexJoints8x4>>();
                for (int i=0; i<mesh.IndexBuffer.Length; i++)
                {
                    var point = mesh.IndexBuffer[(i / 3) * 3 + (2 - (i % 3))]; //flip triangles
                    var vert = mesh.VertexBuffer[point];
                    var texc = new Vector2(vert.TextureCoordinate.X, vert.TextureCoordinate.Y);
                    var boneInd = (int)vert.Parameters.X;
                    var blendInd = (int)vert.Parameters.Y;
                    var mat = skel.Bones[boneInd].AbsoluteMatrix * RotateMX;
                    var bmat = skel.Bones[blendInd].AbsoluteMatrix * RotateMX;
                    VertexBuilder<VertexPositionNormal, VertexTexture1, VertexJoints8x4> nvert =
                        (
                         new VertexPositionNormal(
                            Vector3.Lerp(Vec3Convert(Microsoft.Xna.Framework.Vector3.Transform(vert.Position, mat)),
                                Vec3Convert(Microsoft.Xna.Framework.Vector3.Transform(vert.BvPosition, bmat)),
                                vert.Parameters.Z),
                            Vector3.Lerp(Vec3Convert(Microsoft.Xna.Framework.Vector3.TransformNormal(vert.Normal, mat)),
                                Vec3Convert(Microsoft.Xna.Framework.Vector3.TransformNormal(vert.BvNormal, bmat)),
                                vert.Parameters.Z)
                            ),
                         new VertexTexture1(texc),
                         new VertexJoints8x4(new SharpGLTF.Transforms.SparseWeight8(new Vector4(boneInd, blendInd, 0, 0), new Vector4(1-vert.Parameters.Z, vert.Parameters.Z, 0, 0)))
                         );
                    if (previous.Count == 2)
                    {
                        prim.AddTriangle(previous[0], previous[1], nvert);
                        previous.Clear();
                    }
                    else
                    {
                        previous.Add(nvert);
                    }
                }
                
            }
            var skin = builder.AddSkinnedMesh(outMesh, world, orderedBones);
            skin.Name = "Skeleton";
            var schema = builder.ToSchema2();

            /*
            var children = schema.LogicalScenes.FirstOrDefault().VisualChildren;
            foreach (var child in children)
            {
                if (child.Skin == null) { //armature
                    var extras = child.TryUseExtrasAsDictionary(true);
                    foreach (var tp in timeprops)
                    {
                        extras.Add(tp.Key, tp.Value);
                    }
                }
            }
            */

            //var extras = schema.LogicalNodes.FirstOrDefault(x => x.Name == "ROOT").TryUseExtrasAsDictionary(true);
            var extras = schema.LogicalScenes.FirstOrDefault().TryUseExtrasAsDictionary(true);
            foreach (var tp in timeprops)
            {
                extras.Add(tp.Key, tp.Value);
            }

            return schema;
        }
    }
}
