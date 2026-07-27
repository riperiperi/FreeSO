using Microsoft.Xna.Framework;

namespace FSO.Common.Model
{
    [Flags]
    public enum SurroundPuppetDelta : int
    {
        None = 0,
        BodyInfo = 1 << 0, // persist id, skin tone, outfits, skeleton name
        Position = 1 << 1,
        Appearances = 1 << 2,
        AnimationNames = 1 << 3,
        AnimationState = 1 << 4,

        Animation = AnimationNames | AnimationState,
        All = BodyInfo | Position | Appearances | Animation,

        Required = BodyInfo | Position | Animation,

        // This isn't a delta flag - it's just a special flag that means that this puppet should disappear
        // if there's a puppet somewhere else without the flag, or a user on the source lot.
        // This prevents some duplicate overlapping puppets during lot transitions.
        Leaving = 1 << 31,
    }

    [Flags]
    public enum SurroundPuppetAnimationFlags
    {
        None = 0,
        EndReached = 1 << 0,
        PlayingBackwards = 1 << 1,
        Loop = 1 << 2,
    }

    public struct SurroundPuppetAnimation(string name, float currentFrame, float speed, float weight, SurroundPuppetAnimationFlags flags)
    {
        public readonly string Name = name;
        public readonly float CurrentFrame = currentFrame;
        public readonly float Speed = speed;
        public readonly float Weight = weight;
        public readonly SurroundPuppetAnimationFlags Flags = flags;

        public readonly bool EndReached => Flags.HasFlag(SurroundPuppetAnimationFlags.EndReached);
        public readonly bool PlayingBackwards => Flags.HasFlag(SurroundPuppetAnimationFlags.PlayingBackwards);
        public readonly bool Loop => Flags.HasFlag(SurroundPuppetAnimationFlags.Loop);

        public SurroundPuppetAnimation(string name, float currentFrame, float speed, float weight, bool endReached, bool playingBackwards, bool loop)
            : this(name, currentFrame, speed, weight, (endReached ? SurroundPuppetAnimationFlags.EndReached : 0) |
                (playingBackwards ? SurroundPuppetAnimationFlags.PlayingBackwards : 0) |
                (loop ? SurroundPuppetAnimationFlags.Loop : 0))
        {
        }
    }

    public struct SurroundPuppet
    {
        public SurroundPuppetDelta Delta;
        public uint PersistID;
        public uint SkinTone;
        public ulong HeadOutfit;
        public ulong BodyOutfit;
        public string SkeletonName;
        public Vector4 VisualPositionStart;
        public Vector4 Velocity;
        public SurroundPuppetAnimation[] Animations;
        public string[] Appearances;

        public void CalculateDelta(in SurroundPuppet previous)
        {
            SurroundPuppetDelta delta = Delta & (SurroundPuppetDelta.Leaving);

            if (PersistID != previous.PersistID || SkinTone != previous.SkinTone || HeadOutfit != previous.HeadOutfit || BodyOutfit != previous.BodyOutfit || SkeletonName != previous.SkeletonName)
            {
                delta |= SurroundPuppetDelta.BodyInfo;
            }

            if (VisualPositionStart != previous.VisualPositionStart || Velocity != previous.Velocity)
            {
                delta |= SurroundPuppetDelta.Position;
            }

            if (!Appearances.SequenceEqual(previous.Appearances))
            {
                delta |= SurroundPuppetDelta.Appearances;
            }

            if (Animations.Length != previous.Animations.Length)
            {
                delta |= SurroundPuppetDelta.Animation;
            }
            else
            {
                for (int i = 0; i < Animations.Length; i++)
                {
                    ref readonly var anim = ref Animations[i];
                    ref readonly var oldAnim = ref previous.Animations[i];

                    if (anim.Name != oldAnim.Name)
                    {
                        delta |= SurroundPuppetDelta.AnimationNames;
                    }

                    if (anim.CurrentFrame != oldAnim.CurrentFrame || anim.Weight != oldAnim.Weight || anim.Speed != oldAnim.Speed || anim.Flags != oldAnim.Flags)
                    {
                        delta |= SurroundPuppetDelta.AnimationState;
                    }
                }
            }

            Delta = delta;
        }

        public void ApplyDelta(SurroundPuppet puppet)
        {
            var delta = puppet.Delta;
            if (delta.HasFlag(SurroundPuppetDelta.BodyInfo))
            {
                PersistID = puppet.PersistID;
                SkinTone = puppet.SkinTone;
                HeadOutfit = puppet.HeadOutfit;
                BodyOutfit = puppet.BodyOutfit;
                SkeletonName = puppet.SkeletonName;
            }

            if (delta.HasFlag(SurroundPuppetDelta.Position))
            {
                VisualPositionStart = puppet.VisualPositionStart;
                Velocity = puppet.Velocity;
            }

            if ((delta & SurroundPuppetDelta.Animation) != 0)
            {
                if ((delta & SurroundPuppetDelta.Animation) == SurroundPuppetDelta.Animation || puppet.Animations.Length != (Animations?.Length ?? 0))
                {
                    Animations = puppet.Animations;
                }
                else
                {
                    for (int i = 0; i < Animations.Length; i++)
                    {
                        ref var anim = ref Animations[i];
                        ref var deltaAnim = ref puppet.Animations[i];

                        string animName = anim.Name;

                        if (delta.HasFlag(SurroundPuppetDelta.AnimationNames))
                        {
                            animName = deltaAnim.Name;
                        }

                        float animCurrentFrame = anim.CurrentFrame;
                        float animSpeed = anim.Speed;
                        float animWeight = anim.Weight;
                        SurroundPuppetAnimationFlags animFlags = anim.Flags;

                        if (delta.HasFlag(SurroundPuppetDelta.AnimationState))
                        {
                            animCurrentFrame = deltaAnim.CurrentFrame;
                            animSpeed = deltaAnim.Speed;
                            animWeight = deltaAnim.Weight;
                            animFlags = deltaAnim.Flags;
                        }

                        anim = new SurroundPuppetAnimation(animName, animCurrentFrame, animSpeed, animWeight, animFlags);
                    }
                }
            }

            if (delta.HasFlag(SurroundPuppetDelta.Appearances))
            {
                Appearances = puppet.Appearances;
            }

            Delta = delta;
        }
    }
}
