using SharpGLTF.Scenes;
using SharpGLTF.Schema2;
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
        private Quaternion RotateQ = Quaternion.Inverse(Quaternion.CreateFromAxisAngle(new Vector3(1, 0, 0), (float)Math.PI / -2f));
        private Matrix4x4 RotateM;
        private Microsoft.Xna.Framework.Matrix RotateMX = Microsoft.Xna.Framework.Matrix.Invert(Microsoft.Xna.Framework.Matrix.CreateFromAxisAngle(new Microsoft.Xna.Framework.Vector3(0, 0, 1), (float)Math.PI / 2f));

        public GLTFImporter()
        {
            Matrix4x4.Invert(Matrix4x4.CreateFromAxisAngle(new Vector3(1, 0, 0), (float)Math.PI / -2f), out RotateM);
        }


        //the reverse of the functions in GLTFExporter
        private Microsoft.Xna.Framework.Quaternion QuatConvert(Quaternion quat)
        {
            return new Microsoft.Xna.Framework.Quaternion(quat.Y, -quat.Z, -quat.X, quat.W);
            //return new Quaternion(-quat.Z, quat.X, -quat.Y, quat.W);

        }

        private Microsoft.Xna.Framework.Vector3 Vec3Convert(Vector3 vec)
        {
            return new Microsoft.Xna.Framework.Vector3(-vec.Y, vec.Z, vec.X);
            //return new Vector3(vec.Z, -vec.X, vec.Y);
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

        public List<FSO.Vitaboy.Animation> Animations;
        public List<FSO.Vitaboy.Mesh> Meshes;
        public FSO.Vitaboy.Skeleton Skeleton;
        //todo: textures

        public void Process(string filename)
        {
            var root = ModelRoot.Load(filename);
            Animations = new List<Vitaboy.Animation>();
            Meshes = new List<Vitaboy.Mesh>();

            int fps = 36;
            float invFPS = 1f / fps;

            var nodes = root.LogicalNodes;

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
                        motion.TimeProperties = new Vitaboy.TimePropertyList[0]; //todo
                        
                        if (motion.HasRotation)
                        {
                            motion.FirstRotationIndex = (uint)vitaRot.Count;

                            for (int i=0; i<frameDuration; i++)
                            {
                                var baseQuat = rotSampler.GetPoint(i * invFPS);
                                if (isroot) baseQuat = RotateQ * baseQuat;
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
                                if (isroot) baseTrans = Vector3.Transform(baseTrans, RotateM);
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
}
