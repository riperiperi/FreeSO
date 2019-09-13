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
        public List<FSO.Vitaboy.Mesh> Meshes;
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

        public void Process(string filename)
        {
            var root = ModelRoot.Load(filename);
            Animations = new List<Vitaboy.Animation>();
            Meshes = new List<Vitaboy.Mesh>();

            int fps = 36;
            float invFPS = 1f / fps;

            var nodes = root.LogicalNodes;
            var sceneExtras = root.TryUseExtrasAsDictionary(false);

            foreach (var animation in root.LogicalAnimations)
            {
                var vitaAnim = new Vitaboy.Animation();
                Animations.Add(vitaAnim);
                var vitaRot = new List<Microsoft.Xna.Framework.Quaternion>();
                var vitaTrans = new List<Microsoft.Xna.Framework.Vector3>();

                vitaAnim.Name = SanitizeName(animation.Name);
                vitaAnim.Duration = animation.Duration;
                vitaAnim.XSkillName = vitaAnim.Name;

                uint frameDuration = (uint)Math.Ceiling(animation.Duration * fps);
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
                    var rotSampler = animation.FindRotationSampler(node)?.CreateCurveSampler();
                    var transSampler = animation.FindTranslationSampler(node)?.CreateCurveSampler();
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
                            var timeprops = GetTimeProps(sceneExtras, vitaAnim.Name);
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
                            motion.FirstRotationIndex = (uint)vitaRot.Count;

                            for (int i=0; i<frameDuration; i++)
                            {
                                var baseQuat = rotSampler.GetPoint(i * invFPS);
                                if (isroot) baseQuat = RotateQ * transform.Rotation * baseQuat;
                                vitaRot.Add(QuatConvert(baseQuat));
                            }
                        }
                        else
                        {
                            motion.FirstRotationIndex = uint.MaxValue;
                        }

                        if (motion.HasTranslation)
                        {
                            motion.FirstTranslationIndex = (uint)vitaTrans.Count;
                            for (int i = 0; i < frameDuration; i++)
                            {
                                var baseTrans = transSampler.GetPoint(i * invFPS);
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
                            motion.FirstTranslationIndex = uint.MaxValue;
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
