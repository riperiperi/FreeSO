/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using FSO.Common.Rendering.Framework;

namespace FSO.Vitaboy
{
    /// <summary>
    /// An animator is used to animate an avatar.
    /// </summary>
    public class Animator : _3DComponent
    {
        protected List<AnimationHandle> Animations = new List<AnimationHandle>();

        /// <summary>
        /// Runs an animation.
        /// </summary>
        /// <param name="avatar">The avatar to run animation for.</param>
        /// <param name="animation">The animation to run.</param>
        /// <returns>Handle to the animation run.</returns>
        public AnimationHandle RunAnimation(Avatar avatar, Animation animation)
        {
            var instance = new AnimationHandle(this);
            instance.Animation = animation;
            instance.Avatar = avatar;
            
            Animations.Add(instance);
            return instance;
        }

        /// <summary>
        /// Disposes an animation.
        /// </summary>
        /// <param name="animation">The animation to dispose.</param>
        public void DisposeAnimation(AnimationHandle animation)
        {
            this.Animations.Remove(animation);
        }

        public void Update(GameTime time)
        {
            lock (Animations)
            {
                for (var i = 0; i < Animations.Count; i++)
                {
                    var item = Animations[i];
                    item.Update(time);
                    if (item.Status == AnimationStatus.COMPLETED)
                    {
                        Animations.RemoveAt(i);
                        i--;
                    }
                }

                //AnimationStatus
            }
        }

        public override void Update(FSO.Common.Rendering.Framework.Model.UpdateState state)
        {
            this.Update(state.Time);
        }

        public override void Draw(Microsoft.Xna.Framework.Graphics.GraphicsDevice device)
        {
        }

        public static AnimationStatus SilentFrameProgress(Avatar avatar, Animation animation, int frame)
        {
            bool completed = (frame < 0 || frame > animation.NumFrames);
            frame = Math.Max(Math.Min(frame, animation.NumFrames), 0);

            if (completed || frame + 1 > animation.NumFrames) return AnimationStatus.COMPLETED;
            return AnimationStatus.IN_PROGRESS;
        }

        /// <summary>
        /// Renders an animation's frame.
        /// </summary>
        /// <param name="avatar">The avatar which the animation is run for.</param>
        /// <param name="animation">The animation.</param>
        /// <param name="frame">Frame number in animation.</param>
        /// <param name="fraction"></param>
        /// <param name="weight">The amount to apply this animation to the skeleton.</param>
        /// <returns>Status of animation.</returns>
        public static AnimationStatus RenderFrame(Avatar avatar, Animation animation, int frame, float fraction, float weight)
        {
            bool completed = (frame < 0 || frame > animation.NumFrames);
            frame = Math.Max(Math.Min(frame, animation.NumFrames), 0);

            var numDone = 0;

            foreach (var motion in animation.Motions)
            {
                var bone = avatar.Skeleton.GetBone(motion.BoneName);
                if (bone == null) continue; //fixes bugs with missing bones.. need to find out what R_FINGERPOLY0 is though.

                var motionFrame = frame;
                if (frame >= motion.FrameCount)
                {
                    numDone++;
                    motionFrame = (int)motion.FrameCount - 1;
                }

                if (motion.HasTranslation)
                {
                    Vector3 trans;
                    if (fraction >= 0)
                    {
                        var trans1 = animation.Translations[motion.FirstTranslationIndex + motionFrame];
                        var trans2 = (frame + 1 >= motion.FrameCount) ? trans1 : animation.Translations[motion.FirstTranslationIndex + motionFrame+1];
                        trans = Vector3.Lerp(trans1, trans2, fraction);
                    }
                    else
                    {
                        trans = animation.Translations[motion.FirstTranslationIndex + motionFrame];
                    }
                    if (weight == 1) bone.Translation = trans;
                    else bone.Translation = Vector3.Lerp(bone.Translation, trans, weight);
                }
                if (motion.HasRotation)
                {
                    Quaternion quat;
                    if (fraction >= 0)
                    {
                        var quat1 = animation.Rotations[motion.FirstRotationIndex + motionFrame];
                        var quat2 = (frame + 1 >= motion.FrameCount) ? quat1 : animation.Rotations[motion.FirstRotationIndex + motionFrame + 1];
                        quat = Quaternion.Slerp(quat1, quat2, fraction);
                    }
                    else
                    {
                        quat = animation.Rotations[motion.FirstRotationIndex + motionFrame];
                    }
                    if (weight == 1) bone.Rotation = quat;
                    else bone.Rotation = Quaternion.Slerp(bone.Rotation, quat, weight);
                }
            }

            if (completed || frame+1 > animation.NumFrames) return AnimationStatus.COMPLETED;
            return AnimationStatus.IN_PROGRESS;
        }

        public static Quaternion CalculateHeadSeek(Avatar avatar, Vector3 target, float radianDir)
        {
            var head = avatar.Skeleton.GetBone("HEAD");
            var neck = avatar.Skeleton.GetBone(head.ParentName);

            var absoluteNeck = neck.AbsoluteMatrix * Matrix.CreateRotationY((float)(Math.PI - radianDir));
            var inv = Matrix.Invert(absoluteNeck);

            var diff = Vector3.Transform(target, inv);

            var dirh = (float)Math.Atan2(-diff.Y, diff.Z);
            var dist = Math.Sqrt(diff.Z * diff.Z + diff.Y * diff.Y);
            var dirv = (float)Math.Atan(diff.X / dist);

            dirv = Math.Min((float)Math.PI / 4, Math.Max((float)Math.PI / -4, dirv));
            var hlimit = (float)Math.PI * (65f / 180f);
            dirh = Math.Min(hlimit, Math.Max(-hlimit, dirh));

            var mat = Matrix.CreateRotationY(dirv) * Matrix.CreateRotationX(dirh);
            var quat = Quaternion.CreateFromRotationMatrix(mat);
            return quat;
        }

        public static void ApplyHeadSeek(SimAvatar avatar, Quaternion quat, float weight)
        {
            var head = avatar.Skeleton.GetBone("HEAD");

            if (weight == 1) head.Rotation = quat;
            else head.Rotation = Quaternion.Slerp(head.Rotation, quat, weight);
        }

        public override void DeviceReset(Microsoft.Xna.Framework.Graphics.GraphicsDevice Device)
        {
        }
    }

    /// <summary>
    /// Handle to an animation.
    /// </summary>
    public class AnimationHandle
    {
        public Animation Animation;
        public double Speed = 1.0f;
        public Avatar Avatar;
        public long StartTime;
        private Animator Owner;
        public AnimationStatus Status;
        
        /// <summary>
        /// Constructs a new AnimationHandle instance.
        /// </summary>
        /// <param name="animator">The Animator instance to use.</param>
        public AnimationHandle(Animator animator)
        {
            this.Owner = animator;
        }

        /// <summary>
        /// Disposes this animation handle.
        /// </summary>
        public void Dispose()
        {
            this.Owner.DisposeAnimation(this);
        }

        public void Update(GameTime time)
        {
            var now = time.ElapsedGameTime.Milliseconds;
            if (this.Status == AnimationStatus.WAITING_TO_START){
                StartTime = now;
                this.Status = AnimationStatus.IN_PROGRESS;
            }

            var fps = 30.0f * Speed;
            var fpms = fps / 1000;

            var runTime = now - StartTime;
            uint frame = (uint)(runTime * fpms);
            var fraction = (runTime * fpms) - frame;

            int numDone = 0;

            /** Speed is 30fps by default **/
            foreach (var motion in Animation.Motions)
            {
                var bone = Avatar.Skeleton.GetBone(motion.BoneName);
                var motionFrame = frame;
                if (frame >= motion.FrameCount)
                {
                    numDone++;
                    motionFrame = motion.FrameCount - 1;
                }

                if (motion.HasTranslation)
                {
                    bone.Translation = Animation.Translations[motion.FirstTranslationIndex + motionFrame];
                }
                if (motion.HasRotation)
                {
                    bone.Rotation = Animation.Rotations[motion.FirstRotationIndex + motionFrame];
                }
            }

            if (numDone == Animation.Motions.Length)
            {
                /** Completed! **/
                this.Status = AnimationStatus.COMPLETED;
            }
            else
            {
                Avatar.ReloadSkeleton();
            }
        }
    }

    public enum AnimationStatus
    {
        WAITING_TO_START,
        IN_PROGRESS,
        COMPLETED,
        STOPPED
    }
}
