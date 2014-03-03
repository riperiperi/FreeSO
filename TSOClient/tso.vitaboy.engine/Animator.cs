using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using TSO.Common.rendering.framework;

namespace TSO.Vitaboy
{
    public class Animator : _3DComponent
    {
        protected List<AnimationHandle> Animations = new List<AnimationHandle>();

        public AnimationHandle RunAnimation(Avatar avatar, Animation animation){
            var instance = new AnimationHandle(this);
            instance.Animation = animation;
            instance.Avatar = avatar;
            
            Animations.Add(instance);
            return instance;
        }

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

        public override void Update(TSO.Common.rendering.framework.model.UpdateState state)
        {
            this.Update(state.Time);
        }

        public override void Draw(Microsoft.Xna.Framework.Graphics.GraphicsDevice device)
        {
        }

        public static AnimationStatus RenderFrame(Avatar avatar, Animation animation, uint frame)
        {
            var numDone = 0;

            foreach (var motion in animation.Motions)
            {
                var bone = avatar.Skeleton.GetBone(motion.BoneName);
                var motionFrame = frame;
                if (frame >= motion.FrameCount)
                {
                    numDone++;
                    motionFrame = (uint)motion.FrameCount - 1;
                }

                if (motion.HasTranslation)
                {
                    bone.Translation = animation.Translations[motion.FirstTranslationIndex + motionFrame];
                }
                if (motion.HasRotation)
                {
                    bone.Rotation = animation.Rotations[motion.FirstRotationIndex + motionFrame];
                }
            }

            avatar.ReloadSkeleton();
            if (numDone == animation.Motions.Length)
            {
                return AnimationStatus.COMPLETED;
            }
            else
            {
                return AnimationStatus.IN_PROGRESS;
            }
        }

        public override void DeviceReset(Microsoft.Xna.Framework.Graphics.GraphicsDevice Device)
        {
        }
    }

    public class AnimationHandle
    {
        public Animation Animation;
        public double Speed = 1.0f;
        public Avatar Avatar;
        public long StartTime;
        private Animator Owner;
        public AnimationStatus Status;

        public AnimationHandle(Animator animator)
        {
            this.Owner = animator;
        }

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
