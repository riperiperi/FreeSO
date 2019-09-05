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
        private Quaternion RotateQ = Quaternion.CreateFromAxisAngle(new Vector3(1, 0, 0), (float)Math.PI / -2f);
        private Matrix4x4 RotateM = Matrix4x4.CreateFromAxisAngle(new Vector3(1, 0, 0), (float)Math.PI / -2f);
        private Microsoft.Xna.Framework.Matrix RotateMX = Microsoft.Xna.Framework.Matrix.CreateFromAxisAngle(new Microsoft.Xna.Framework.Vector3(0, 0, 1), (float)Math.PI / 2f);
        //negative y becomes positive z
        //positive z becomes positive y
        private Quaternion QuatConvert(Microsoft.Xna.Framework.Quaternion quat)
        {
            //return new Quaternion(-quat.Z, -quat.Y, -quat.X, quat.W); //up but bad
            return new Quaternion(-quat.Z, quat.X, -quat.Y, quat.W);

        }

        private Vector3 Vec3Convert(Microsoft.Xna.Framework.Vector3 vec)
        {
            //return new Vector3(vec.Z, vec.Y, vec.X); //up but bad
            return new Vector3(vec.Z, -vec.X, vec.Y);
        }

        private float RecursiveScale(NodeBuilder elem)
        {
            if (elem == null) return 1f;
            return (elem.Parent == null) ? (elem.Scale?.Value.X ?? 1f) : (elem.Scale?.Value.X ?? 1f) * RecursiveScale(elem.Parent);
        }

        private void ConvertBone(Dictionary<Vitaboy.Bone, NodeBuilder> all, Vitaboy.Bone bone, NodeBuilder target)
        {
            all[bone] = target;
            var len = (bone.Children.Length == 0) ? 0.1f : bone.Children.Where(x => x.Translation.Length() > 0).Average(x => x.Translation.Length());
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
            //TODO: support xevt somehow

            
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

                    //create curves for rotation and translation

                    var rotCurve = bone.Rotation.UseTrackBuilder(name);
                    CurveBuilder<Vector3> transCurve = null;
                    if (motion.FirstTranslationIndex != uint.MaxValue) transCurve = bone.Translation.UseTrackBuilder(name);

                    for (int i = 0; i < motion.FrameCount; i++)
                    {
                        var quat = QuatConvert(anim.Rotations[i + motion.FirstRotationIndex]);
                        Vector3 trans = new Vector3();
                        if (transCurve != null) trans = Vec3Convert(anim.Translations[i + motion.FirstTranslationIndex]) / scale;
                        if (root)
                        {
                            quat = RotateQ * quat;
                            trans = Vector3.Transform(trans, RotateM);
                        }
                        rotCurve.SetPoint(i * framerate, quat, true);
                        transCurve?.SetPoint(i * framerate, trans, true);
                    }
                }
            }

            var outMesh = new MeshBuilder<VertexPositionNormal, VertexTexture1, VertexJoints8x4>("sim");

            var meshi = 0;
            foreach (var mesh in meshes) {
                var tex = textures[meshi++];
                var data = TextureToPng(tex);

                var material = new MaterialBuilder(mesh.SkinName);
                material.UseChannel(KnownChannels.BaseColor).UseTexture().WithPrimaryImage(new ArraySegment<byte>(data));
                var prim = outMesh.UsePrimitive(material, 3);

                //todo: blend verts
                var previous = new List<VertexBuilder<VertexPositionNormal, VertexTexture1, VertexJoints8x4>>();
                foreach (var point in mesh.IndexBuffer)
                {
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
                            Vec3Convert(Microsoft.Xna.Framework.Vector3.TransformNormal(vert.Normal, mat))
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

            var orderedBones = skel.Bones.Select(x => nodes[x]);

            var world = Matrix4x4.Identity; //Matrix4x4.CreateFromAxisAngle(new Vector3(0, 1, 0), (float)Math.PI / 2f);
            var test = builder.AddSkinnedMesh(outMesh, world, orderedBones.ToArray());

            return builder.ToSchema2();
        }
    }
}
