using SharpGLTF.Scenes;
using SharpGLTF.Schema2;
using SharpGLTF.Transforms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace FSO.IDE.Utils
{
    public class GLTFImporter
    {
        private Quaternion RotateQ = Quaternion.Inverse(GLTFExporter.RotateQ);
        private Matrix4x4 RotateM;
        private Microsoft.Xna.Framework.Matrix RotateMX = Microsoft.Xna.Framework.Matrix.Invert(GLTFExporter.RotateMX);

        public List<FSO.Vitaboy.Animation> Animations;
        public List<ImportMeshGroup> Meshes;
        public FSO.Vitaboy.Skeleton Skeleton;
        //todo: textures

        public GLTFImporter()
        {
            Matrix4x4.Invert(GLTFExporter.RotateM, out RotateM);
        }


        //the reverse of the functions in GLTFExporter
        private Microsoft.Xna.Framework.Quaternion QuatConvert(Quaternion quat)
        {
            return new Microsoft.Xna.Framework.Quaternion(-quat.Y, quat.Z, -quat.X, quat.W);
            //return new Microsoft.Xna.Framework.Quaternion(quat.Y, -quat.Z, -quat.X, quat.W);
        }

        private Microsoft.Xna.Framework.Vector3 Vec3Convert(Vector3 vec)
        {
            return new Microsoft.Xna.Framework.Vector3(-vec.Y, vec.Z, -vec.X) / GLTFExporter.MeshScale;
            //return new Microsoft.Xna.Framework.Vector3(-vec.Y, vec.Z, vec.X);
        }

        private float RecursiveScale(Node elem)
        {
            if (elem == null) return 1f;
            var components = elem.LocalTransform;
            return components.Scale.X * RecursiveScale(elem.VisualParent);
        }

        private string SanitizeName(string name)
        {
            var ind = name.IndexOf('_');
            if (ind != -1) return name.Substring(0, ind);
            else return name;
        }

        private List<ParsedTimeProp> GetTimeProps(Dictionary<string, object> props, string animName)
        {
            return props.Select(x => ParsedTimeProp.Parse(x.Key, Convert.ToSingle(x.Value)))
                .Where(x => x != null && (x.AnimName == animName || x.AnimName == "timeprop")).ToList();
        }

        public Vitaboy.Bone NodeToBone(Node bone, Matrix4x4 worldMat)
        {
            AffineTransform worldTransform = AffineTransform.WorldToLocal(Matrix4x4.Identity, worldMat);
            var vbone = new Vitaboy.Bone();
            vbone.Name = bone.Name;
            var isroot = bone.Name == "ROOT";
            if (bone.VisualParent == null || isroot)
            {
                vbone.ParentName = "NULL";
            }
            else
            {
                vbone.ParentName = bone.VisualParent.Name;
            }
            vbone.HasProps = false;
            vbone.Properties = new List<Vitaboy.PropertyListItem>();
            vbone.CanBlend = 1;
            vbone.CanRotate = 1;
            vbone.CanTranslate = 1;
            var recursiveScale = RecursiveScale(bone.VisualParent);

            var transform = bone.LocalTransform;
            var baseQuat = transform.Rotation;
            if (isroot) baseQuat = RotateQ * worldTransform.Rotation * baseQuat; //worldTransform.Rotation * baseQuat;
            vbone.Rotation = QuatConvert(baseQuat);

            var baseTrans = transform.Translation;
            if (isroot)
            {
                baseTrans = Vector3.Transform(baseTrans, worldMat * RotateM); //worldMat * RotateM);
            }
            baseTrans *= recursiveScale;
            vbone.Translation = Vec3Convert(baseTrans);
            return vbone;
        }

        private bool IsChildOf(Node child, Node parent)
        {
            return child != null && (child.VisualParent == parent || IsChildOf(child.VisualParent, parent));
        }

        public void Process(string filename)
        {
            var root = ModelRoot.Load(filename);
            Animations = new List<Vitaboy.Animation>();
            Meshes = new List<ImportMeshGroup>();

            int fps = 36;
            float invFPS = 1f / fps;

            //var rootNode = root.LogicalNodes.FirstOrDefault(node => node.Name == "ROOT");
            var skin = root.LogicalSkins.FirstOrDefault();
            if (skin != null)
            {
                Skeleton = new Vitaboy.Skeleton();
                Skeleton.Name = "custom";
                var invBind = skin.GetInverseBindMatricesAccessor().AsMatrix4x4Array();

                var orderedNodes = Enumerable.Range(0, skin.JointsCount).Select(i => {
                    var node = skin.GetJoint(i).Item1;
                    Matrix4x4 mat;
                    if (Matrix4x4.Invert(invBind[i], out mat))
                    {
                        node.WorldMatrix = mat;
                    }
                    return node;
                    }).ToList();
                var rootNode = orderedNodes.FirstOrDefault(x => x.Name == "ROOT");

                var worldMat = Matrix4x4.Identity;
                //find the first node above the root bone
                var animNode = rootNode.VisualParent;
                if (animNode != null)
                {
                    worldMat = animNode.WorldMatrix;
                }

                if (rootNode != null)
                {
                    //var orderedNodes = root.LogicalNodes.Where(x => x == rootNode || IsChildOf(x, rootNode));
                    var bones = new List<Vitaboy.Bone>();
                    var boneI = 0;
                    foreach (var node in orderedNodes)
                    {
                        var bone = NodeToBone(node, worldMat);
                        bone.Index = boneI++;
                        bones.Add(bone);
                    }
                    Skeleton.RootBone = bones.FirstOrDefault(x => x.ParentName == "NULL");
                    foreach (var bone in bones)
                    {
                        bone.Children = bones.Where(x => x.ParentName == bone.Name).ToArray();
                    }
                    Skeleton.Bones = bones.ToArray();
                    Skeleton.BuildBoneDictionary();
                    Skeleton.ComputeBonePositions(Skeleton.RootBone, Microsoft.Xna.Framework.Matrix.Identity);
                }
            }

            var models = root.LogicalMeshes;
            foreach (var model in models)
            {
                foreach (var prim in model.Primitives)
                {
                    var mat = prim.Material;
                    if (mat == null) continue; // must have a material

                    var mesh = new Vitaboy.Mesh();
                    mesh.SkinName = mat.Name;

                    var tris = prim.GetTriangleIndices();
                    var indices = new List<int>();
                    foreach (var tri in tris)
                    {
                        indices.Add(tri.Item3);
                        indices.Add(tri.Item2);
                        indices.Add(tri.Item1);
                    }
                    var indBuffer = indices.ToArray();

                    var positions = prim.VertexAccessors["POSITION"].AsVector3Array();
                    var uvs = prim.VertexAccessors["TEXCOORD_0"].AsVector2Array();
                    var normals = prim.VertexAccessors["NORMAL"].AsVector3Array();
                    var joints = prim.VertexAccessors["JOINTS_0"].AsVector4Array();
                    var weights = prim.VertexAccessors["WEIGHTS_0"].AsVector4Array();

                    //we actually need to remap vertices.

                    var verts = new List<MeshVert>();

                    for (int i = 0; i < positions.Count; i++)
                    {
                        var pos = positions[i];
                        var uv = uvs[i];
                        var normal = normals[i];
                        var joint = joints[i];
                        var weight = weights[i];

                        verts.Add(new MeshVert(i, pos, uv, normal, joint, weight));
                    }

                    // order by bone bindings, as bones and blend verts are bound using regions
                    // sorts by blend bone first, then real bone. Result looks like:
                    // (bone 0, no blend), (bone 1, blend 2), (bone 1, blend 3), (bone 2, no blend), ... 
                    verts = verts.OrderBy(x => x.Joints.X).ToList(); //(x => (x.Weights.Y == 0) ? -1 : x.Joints.Y).ThenBy

                    // remap indices to point where the vertices are after sorting
                    for (int i = 0; i < indBuffer.Length; i++)
                    {
                        indBuffer[i] = verts.FindIndex(vert => vert.SourceIndex == indBuffer[i]);
                    }

                    mesh.IndexBuffer = indBuffer;
                    mesh.NumPrimitives = indBuffer.Length / 3;
                    mesh.VertexBuffer = verts.Select(vert => {
                        var pos = Vec3Convert(vert.Position);
                        var normal = Vec3Convert(vert.Normal);
                        // position is already transformed. we need to invert the bind bone transform to get back to origin
                        // (do not scale)

                        //find main bone
                        var mainInd = (int)vert.Joints.X;
                        var main = (mainInd < 0 || mainInd >= Skeleton.Bones.Length) ? null : Skeleton.Bones[mainInd];

                        //find blend bone
                        var blendInd = (int)vert.Joints.Y;
                        var blend = (vert.Weights.Y == 0 || blendInd < 0 || blendInd >= Skeleton.Bones.Length) ? null : Skeleton.Bones[blendInd];

                        var bpos = pos;
                        var bnorm = normal;

                        if (main != null)
                        {
                            var bonemat = RotateMX * Microsoft.Xna.Framework.Matrix.Invert(main.AbsoluteMatrix);
                            pos = Microsoft.Xna.Framework.Vector3.Transform(pos, bonemat);
                            normal = Microsoft.Xna.Framework.Vector3.TransformNormal(normal, bonemat);
                        }

                        if (blend != null)
                        {
                            var bonemat = RotateMX * Microsoft.Xna.Framework.Matrix.Invert(blend.AbsoluteMatrix);
                            bpos = Microsoft.Xna.Framework.Vector3.Transform(bpos, bonemat);
                            bnorm = Microsoft.Xna.Framework.Vector3.TransformNormal(bnorm, bonemat);
                        } else
                        {
                            bpos = pos;
                            bnorm = normal;
                        }

                        // only two weights are supported (one "real" and one "blend", so we will have to remove the smallest
                        // assume XYZW is sorted highest to lowest weight
                        var weight = vert.Weights;
                        var multiplier = (weight.X + weight.Y + weight.Z + weight.W) / (weight.X + weight.Y);

                        return new Vitaboy.Model.VitaboyVertex(
                            pos,
                            new Microsoft.Xna.Framework.Vector2(vert.UV.X, vert.UV.Y),
                            bpos,
                            new Microsoft.Xna.Framework.Vector3(vert.Joints.X, vert.Joints.Y, weight.Y * multiplier), //bone ind, blend ind, intensity
                            normal,
                            bnorm
                            );
                    }).ToArray();

                    //create bindings
                    var bindings = new List<Vitaboy.BoneBinding>();
                    var blendData = new List<Vitaboy.BlendData>();
                    var blendVerts = new List<Microsoft.Xna.Framework.Vector3>();
                    var blendNormals = new List<Microsoft.Xna.Framework.Vector3>();
                    Vitaboy.BoneBinding current = null;
                    var vertI = 0;
                    var vcount = 0;

                    //real verts first
                    foreach (var vert in mesh.VertexBuffer)
                    {
                        if (current == null || current.BoneIndex != (int)vert.Parameters.X)
                        {
                            if (current != null)
                            {
                                current.RealVertexCount = vcount;
                                current.BlendVertexCount = 0;
                            }
                            current = new Vitaboy.BoneBinding();
                            bindings.Add(current);
                            current.FirstRealVertex = vertI;
                            current.FirstBlendVertex = -1;
                            current.BoneIndex = (int)vert.Parameters.X;
                            current.BoneName = (current.BoneIndex < 0 || current.BoneIndex >= Skeleton.Bones.Length) ? "NULL" : Skeleton.Bones[current.BoneIndex].Name;
                            vcount = 0;
                        }
                        vcount++;
                        vertI++;
                    }
                    if (current != null)
                    {
                        current.RealVertexCount = vcount;
                        current.BlendVertexCount = 0;
                    }

                    // blend verts
                    // for each bone, find the verts blend affects
                    // then add them to the blend verts list, and update the binding
                    // O(n*m) but this is an import so whatever!
                    foreach (var binding in bindings)
                    {
                        var id = binding.BoneIndex;
                        var startBlend = blendVerts.Count;
                        var blends = mesh.VertexBuffer.Where(x => (int)x.Parameters.Y == id && x.Parameters.Z > 0).ToList();

                        vertI = 0;
                        foreach (var vert in mesh.VertexBuffer)
                        {
                            if ((int)vert.Parameters.Y == id && vert.Parameters.Z > 0)
                            {
                                blendData.Add(new Vitaboy.BlendData() { OtherVertex = vertI, Weight = vert.Parameters.Z });
                                blendVerts.Add(vert.BvPosition);
                                blendNormals.Add(vert.BvNormal);
                            }
                            vertI++;
                        }
                        if (blendVerts.Count != startBlend)
                        {
                            binding.FirstBlendVertex = startBlend;
                            binding.BlendVertexCount = blendVerts.Count - startBlend;
                        }
                    }

                    mesh.BlendData = blendData.ToArray();
                    mesh.BlendVerts = blendVerts.ToArray();
                    mesh.BlendNormals = blendNormals.ToArray();
                    mesh.BoneBindings = bindings.ToArray();
                    mesh.BlendVertBoneIndices = new int[mesh.BlendData.Length];

                    var tex = mat.FindChannel("BaseColor");
                    var texData = tex?.Texture?.PrimaryImage?.GetImageContent();

                    Meshes.Add(new ImportMeshGroup() { Name = mat.Name, Mesh = mesh, TextureData = texData });
                }
            }

            var nodes = root.LogicalNodes;
            var sceneExtras = root.LogicalScenes.FirstOrDefault().TryUseExtrasAsDictionary(false);

            foreach (var animation in root.LogicalAnimations)
            {
                var vitaAnim = new Vitaboy.Animation();
                Animations.Add(vitaAnim);
                var vitaRot = new List<Microsoft.Xna.Framework.Quaternion>();
                var vitaTrans = new List<Microsoft.Xna.Framework.Vector3>();

                vitaAnim.Name = SanitizeName(animation.Name);
                vitaAnim.Duration = animation.Duration;
                vitaAnim.XSkillName = vitaAnim.Name;

                uint frameDuration = (uint)Math.Max(1, Math.Ceiling(animation.Duration * fps));
                vitaAnim.NumFrames = (int)frameDuration;
                vitaAnim.UpdateFPS();
                var motions = new List<Vitaboy.AnimationMotion>();

                //find the first node above the root bone
                var transform = AffineTransform.Identity;
                var worldMat = Matrix4x4.Identity;
                var animNode = nodes.FirstOrDefault(x => animation.FindRotationSampler(x) != null || animation.FindTranslationSampler(x) != null);
                while (animNode != null && animNode.Name != "ROOT")
                {
                    animNode = animNode.VisualParent;
                }
                if (animNode != null) animNode = animNode.VisualParent;
                if (animNode != null)
                {
                    worldMat = animNode.WorldMatrix;
                    transform = AffineTransform.WorldToLocal(Matrix4x4.Identity, worldMat);
                }

                //check all nodes in the skeleton for matching samplers (rotation, translation).
                //if a sampler exists, add it to the animation
                foreach (var node in nodes) {
                    var recursiveScale = RecursiveScale(node.VisualParent);
                    var rotS = animation.FindRotationSampler(node);
                    var transS = animation.FindTranslationSampler(node);
                    var rotSampler = rotS?.CreateCurveSampler();
                    var transSampler = transS?.CreateCurveSampler();
                    var isroot = node.Name == "ROOT";

                    if (rotSampler != null || transSampler != null)
                    {
                        //add this motion to the animation
                        var motion = new Vitaboy.AnimationMotion();
                        motions.Add(motion);
                        motion.BoneName = node.Name;
                        motion.HasRotation = rotSampler != null;
                        motion.HasTranslation = transSampler != null;
                        motion.FrameCount = frameDuration;
                        motion.Duration = animation.Duration;
                        motion.Properties = new Vitaboy.PropertyList[0];
                       
                        if (sceneExtras != null) //node.Extras != null)
                        {
                            //timeprops for this node
                            var timeprops = GetTimeProps(sceneExtras, vitaAnim.Name).OrderBy(x => x.ID);
                            var list = new Vitaboy.TimePropertyList();

                            var propDict = new Dictionary<int, Vitaboy.TimePropertyListItem>();
                            foreach (var tp in timeprops)
                            {
                                Vitaboy.TimePropertyListItem item;
                                if (!propDict.TryGetValue(tp.ID, out item))
                                {
                                    item = new Vitaboy.TimePropertyListItem();
                                    item.ID = tp.ID;
                                    item.Properties = new Vitaboy.PropertyList();
                                    item.Properties.Items = new Vitaboy.PropertyListItem[0];
                                    propDict[tp.ID] = item;
                                }

                                Array.Resize(ref item.Properties.Items, item.Properties.Items.Length + 1);

                                var entry = new Vitaboy.PropertyListItem();
                                item.Properties.Items[item.Properties.Items.Length - 1] = entry;
                                entry.KeyPairs.Add(new KeyValuePair<string, string>(tp.Event, tp.Value));
                            }
                            list.Items = propDict.Values.ToArray();
                            motion.TimeProperties = new Vitaboy.TimePropertyList[] { list };
                        }
                        else
                        {
                            motion.TimeProperties = new Vitaboy.TimePropertyList[0];
                        }
                        
                        if (motion.HasRotation)
                        {
                            motion.FirstRotationIndex = vitaRot.Count;
                            float rotDuration;
                            if (rotS.InterpolationMode == AnimationInterpolationMode.CUBICSPLINE)
                                rotDuration = rotS.GetCubicKeys().LastOrDefault().Item1;
                            else
                                rotDuration = rotS.GetLinearKeys().LastOrDefault().Item1;

                            for (int i=0; i<frameDuration; i++)
                            {
                                var baseQuat = rotSampler.GetPoint(Math.Min(rotDuration, i * invFPS));
                                if (isroot) baseQuat = RotateQ * transform.Rotation * baseQuat;
                                vitaRot.Add(QuatConvert(baseQuat));
                            }
                        }
                        else
                        {
                            motion.FirstRotationIndex = -1;
                        }

                        if (motion.HasTranslation)
                        {
                            motion.FirstTranslationIndex = vitaTrans.Count;
                            float transDuration;
                            if (transS.InterpolationMode == AnimationInterpolationMode.CUBICSPLINE)
                                transDuration = transS.GetCubicKeys().LastOrDefault().Item1;
                            else
                                transDuration = transS.GetLinearKeys().LastOrDefault().Item1;
                            for (int i = 0; i < frameDuration; i++)
                            {
                                var baseTrans = transSampler.GetPoint(Math.Min(transDuration, i * invFPS));
                                if (isroot)
                                {
                                    baseTrans = Vector3.Transform(baseTrans, worldMat * RotateM);
                                }
                                baseTrans *= recursiveScale;
                                vitaTrans.Add(Vec3Convert(baseTrans));
                            }
                        }
                        else
                        {
                            motion.FirstTranslationIndex = -1;
                        }
                    }
                }

                vitaAnim.Motions = motions.ToArray();
                vitaAnim.Rotations = vitaRot.ToArray();
                vitaAnim.Translations = vitaTrans.ToArray();
                vitaAnim.RotationCount = (uint)vitaAnim.Rotations.Length;
                vitaAnim.TranslationCount = (uint)vitaAnim.Translations.Length;

                vitaAnim.IsMoving = (byte)((motions.Count > 0) ? 1 : 0);
            }
        }
    }

    public class MeshVert
    {
        public int SourceIndex;
        public Vector3 Position;
        public Vector2 UV;
        public Vector3 Normal;
        public Vector4 Joints;
        public Vector4 Weights;

        public MeshVert(int ind, Vector3 pos, Vector2 uv, Vector3 normal, Vector4 joints, Vector4 weights)
        {
            SourceIndex = ind;
            Position = pos;
            UV = uv;
            Normal = normal;
            Joints = joints;
            Weights = weights;
        }
    }

    public class ParsedTimeProp
    {
        public string AnimName;
        public int ID;
        public string Event;
        public string Value;

        public static ParsedTimeProp Parse(string key, float value)
        {
            var split = key.Split('/');
            if (split.Length != 3) return null;
            var result = new ParsedTimeProp();
            result.AnimName = split[0];
            if (!int.TryParse(split[1], out result.ID)) return null;

            var keySplit = split[2].Split('=');
            if (keySplit.Length == 1)
            {
                //just a key, use number value
                result.Event = split[2];
                result.Value = ((short)value).ToString();
            }
            else if (keySplit.Length == 2)
            {
                //key value, ignore number
                result.Event = keySplit[0];
                result.Value = keySplit[1];
            }
            else
            {
                return null; //invalid
            }
            return result;
        }
    }
}
