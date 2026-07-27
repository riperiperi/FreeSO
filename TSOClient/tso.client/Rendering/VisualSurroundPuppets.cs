using FSO.Client.UI.Screens;
using FSO.Common.Domain.Realestate;
using FSO.Common.Model;
using FSO.LotView.Components;
using FSO.LotView.Model;
using FSO.Server.Protocol.Electron.Packets;
using FSO.Vitaboy;
using JWT.Builder;
using Microsoft.Xna.Framework;
using System.Diagnostics;

namespace FSO.Client.Rendering
{
    class VisualSurroundPuppet : IDisposable
    {
        private readonly VisualSurroundPuppets Parent;
        private readonly uint LotLocation;

        private SurroundPuppet Puppet;
        private long StartTimestamp;

        private SimAvatar TargetAvatar;
        private AvatarComponent TargetAvatarComponent;
        private Dictionary<string, Animation> AnimByName = [];

        private Blueprint LastBp;
        private bool ReloadPuppet = false;
        private bool UpdateAppearances = false;
        private string[] LastAppearances;
        private Vector3 LotOffset;

        private SurroundPuppetDelta MissingDelta = SurroundPuppetDelta.Required;

        public bool IsLeaving => Puppet.Delta.HasFlag(SurroundPuppetDelta.Leaving);

        public VisualSurroundPuppet(VisualSurroundPuppets parent, uint lotLocation)
        {
            Parent = parent;
            LotLocation = lotLocation;
        }

        private void RecalculateOffset(uint parentLocation)
        {
            var loc = MapCoordinates.Unpack(LotLocation).ToPoint();
            var parent = MapCoordinates.Unpack(parentLocation).ToPoint();

            var relative = LotTransitionInfo.RelativeChangeCityToLot(loc - parent);

            var width = LastBp.Width;
            var height = LastBp.Height;

            LotOffset = new Vector3(relative.X * (width - 2), relative.Y * (height - 2), 0);
        }

        public void SetPuppet(SurroundPuppet puppet, long startTimestamp)
        {
            Puppet.ApplyDelta(puppet);

            if (puppet.Delta.HasFlag(SurroundPuppetDelta.BodyInfo))
            {
                ReloadPuppet = true;
            }

            if (puppet.Delta.HasFlag(SurroundPuppetDelta.Appearances))
            {
                UpdateAppearances = true;
            }

            MissingDelta &= ~puppet.Delta;

            StartTimestamp = startTimestamp;
        }

        public void PreDraw(uint parentLocation, Blueprint bp, long renderTimestamp)
        {
            if (MissingDelta != 0)
            {
                // Need to see at least one delta for certain fields to draw at all.
                return;
            }

            if ((bp != LastBp || ReloadPuppet || TargetAvatar == null) && bp != null)
            {
                if (TargetAvatar == null)
                {
                    TargetAvatar = new SimAvatar(Content.Content.Get().AvatarSkeletons.Get(Puppet.SkeletonName+".skel"));
                }

                TargetAvatar.Appearance = (AppearanceType)Puppet.SkinTone;
                TargetAvatar.Head = FSO.Content.Content.Get().AvatarOutfits.Get(Puppet.HeadOutfit);
                TargetAvatar.Body = FSO.Content.Content.Get().AvatarOutfits.Get(Puppet.BodyOutfit);

                if (Puppet.SkeletonName == "adult")
                {
                    TargetAvatar.Handgroup = TargetAvatar.Body;
                }

                if (bp != LastBp || TargetAvatarComponent == null)
                {
                    LastBp?.RemoveAvatar(TargetAvatarComponent);

                    TargetAvatarComponent = new()
                    {
                        Avatar = TargetAvatar
                    };
                    TargetAvatarComponent.blueprint = bp;

                    bp.AddAvatar(TargetAvatarComponent);

                    LastBp = bp;

                    RecalculateOffset(parentLocation); // Can somehow end up wildly negative
                }

                LastBp = bp;
                ReloadPuppet = false;
            }

            if (TargetAvatar != null)
            {
                if (UpdateAppearances)
                {
                    var oldApr = LastAppearances ?? [];
                    var newApr = Puppet.Appearances ?? [];

                    var toAdd = newApr.Where(x => Array.IndexOf(oldApr, x) == -1);
                    var toRemove = oldApr.Where(x => Array.IndexOf(newApr, x) == -1);

                    var appearances = Content.Content.Get().AvatarAppearances;

                    foreach (var aprN in toAdd)
                    {
                        var apr = appearances.Get(aprN);
                        if (apr != null) TargetAvatar.AddAccessory(apr);
                    }

                    foreach (var aprN in toRemove)
                    {
                        var apr = appearances.Get(aprN);
                        if (apr != null) TargetAvatar.RemoveAccessory(apr);
                    }

                    LastAppearances = newApr;
                }

                // Update the avatar's position and animation based on the frame timing.
                float fraction = (renderTimestamp - StartTimestamp) / ((float)Stopwatch.Frequency / 30f);

                float totalWeight = 0f;
                foreach (var state in Puppet.Animations)
                {
                    totalWeight += state.Weight;
                    if (!state.EndReached && state.Name != null)
                    {
                        float visualFrame = state.CurrentFrame;
                        if (state.PlayingBackwards) visualFrame -= state.Speed * fraction;
                        else visualFrame += state.Speed * fraction;

                        if (!AnimByName.TryGetValue(state.Name, out var anim))
                        {
                            anim = Content.Content.Get().AvatarAnimations.Get(state.Name + ".anim");
                        }

                        if (anim != null)
                        {
                            Animator.RenderFrame(TargetAvatar, anim, (int)visualFrame, visualFrame % 1, state.Weight / totalWeight);
                        }
                    }
                }

                var pos = Puppet.VisualPositionStart;
                var vel = Puppet.Velocity;
                bool visible = Puppet.Delta.HasFlag(SurroundPuppetDelta.Leaving) ? !Parent.IsPresentElsewhere(Puppet.PersistID) : true;

                TargetAvatar.ReloadSkeleton();
                TargetAvatarComponent.Position = new Vector3(pos.X, pos.Y, pos.Z) + fraction * new Vector3(vel.X, vel.Y, vel.Z) + LotOffset;
                TargetAvatarComponent.RadianDirection = (double)(pos.W - Puppet.Velocity.W * fraction);
                TargetAvatarComponent.Visible = visible;
            }
        }

        public void Dispose()
        {
            LastBp?.RemoveAvatar(TargetAvatarComponent);
        }
    }

    public class VisualSurroundPuppets
    {
        private const int QUEUE_LENGTH_MAX = 3;

        private readonly CoreGameScreen Screen;
        private readonly Dictionary<uint, Dictionary<uint, VisualSurroundPuppet>> LotIdToPuppet = [];

        private readonly HashSet<uint> ExpectedAvatars = [];
        private readonly HashSet<uint> ExpectedLots = [];

        private readonly Queue<SurroundPuppetTick> TickQueue = [];
        private readonly long TickRate;

        private Blueprint LastBp;
        private bool Instant = false;

        private long LastTimestamp;

        public VisualSurroundPuppets(CoreGameScreen screen)
        {
            Screen = screen;
            TickRate = Stopwatch.Frequency / 30;
        }

        private Point CalculateOffset(uint parentLocation, uint lotLocation)
        {
            var loc = MapCoordinates.Unpack(lotLocation).ToPoint();
            var parent = MapCoordinates.Unpack(parentLocation).ToPoint();

            return loc - parent;
        }

        public void PreDraw()
        {
            RunTicks();

            // Try update animation and position for any surround puppets

            var renderTimestamp = Stopwatch.GetTimestamp();

            var vm = Screen.VisualVM;

            if (vm == null || !vm.Ready || vm.FSOVAsyncLoading)
            {
                return;
            }

            uint parentLocation = vm.TSOState.LotID;
            Blueprint bp = vm.Context.Blueprint;
            bool bpChanged = bp != LastBp;

            List<uint> lotIdsToDelete = null;

            foreach (var lot in LotIdToPuppet)
            {
                if (bpChanged)
                {
                    var delta = CalculateOffset(parentLocation, lot.Key);

                    if ((delta.X == 0 && delta.Y == 0) || Math.Abs(delta.X) > 1 || Math.Abs(delta.Y) > 1)
                    {
                        lotIdsToDelete ??= [];

                        lotIdsToDelete.Add(lot.Key);

                        continue;
                    }
                }

                foreach (var puppet in lot.Value)
                {
                    puppet.Value.PreDraw(parentLocation, bp, renderTimestamp);
                }
            }

            if (lotIdsToDelete != null)
            {
                DeleteLots(lotIdsToDelete);
            }

            LastBp = bp;
        }

        private void RunTicks()
        {
            uint myID = Screen.VisualVM?.MyUID ?? 0;

            var now = Stopwatch.GetTimestamp();

            int ticksToRun = Instant ? TickQueue.Count : (int)((now - LastTimestamp) / TickRate);

            if (ticksToRun >= TickQueue.Count)
            {
                ticksToRun = TickQueue.Count;
                Instant = true;
            }

            if (TickQueue.Count > QUEUE_LENGTH_MAX && ticksToRun < TickQueue.Count - 1)
            {
                // Try and catch up a little
                ticksToRun++;
                Instant = true;
            }

            for (int i = 0; i < ticksToRun; i++)
            {
                var tick = TickQueue.Dequeue();

                if (Instant)
                {
                    LastTimestamp = Stopwatch.GetTimestamp();
                }
                else
                {
                    LastTimestamp += TickRate;
                }

                ProcessTick(myID, tick);
            }

            Instant = false;
        }

        private void DeleteLots<T>(T lots) where T : IEnumerable<uint>
        {
            foreach (var id in lots)
            {
                if (LotIdToPuppet.TryGetValue(id, out var puppets))
                {
                    foreach (var puppet in puppets)
                    {
                        puppet.Value.Dispose();
                    }

                    LotIdToPuppet.Remove(id);
                }
            }
        }

        private void ProcessTick(uint myID, SurroundPuppetTick tick)
        {
            var isTransitioning = Screen.VisualVM != Screen.vm;
            ExpectedLots.Clear();
            ExpectedLots.UnionWith(LotIdToPuppet.Keys);

            foreach (ref var lot in tick.Lots.AsSpan())
            {
                if (lot.Outdated)
                {
                    ExpectedLots.Remove(lot.LotLocation);
                    continue;
                }

                if (!LotIdToPuppet.TryGetValue(lot.LotLocation, out var puppets))
                {
                    puppets = [];
                    LotIdToPuppet[lot.LotLocation] = puppets;
                }

                ExpectedAvatars.Clear();
                ExpectedAvatars.UnionWith(puppets.Keys);

                foreach (var puppet in lot.Puppets)
                {
                    if (puppet.PersistID == myID)
                    {
                        // You can't see a puppet of yourself...
                        continue;
                    }

                    if (!puppets.TryGetValue(puppet.PersistID, out var visualPuppet))
                    {
                        visualPuppet = new VisualSurroundPuppet(this, lot.LotLocation);
                        puppets[puppet.PersistID] = visualPuppet;
                    }

                    visualPuppet.SetPuppet(puppet, LastTimestamp);

                    ExpectedAvatars.Remove(puppet.PersistID);
                }

                foreach (var toRemove in ExpectedAvatars)
                {
                    if (puppets.TryGetValue(toRemove, out var visualPuppet))
                    {
                        visualPuppet.Dispose();
                        puppets.Remove(toRemove);
                    }
                }

                ExpectedLots.Remove(lot.LotLocation);
            }

            if (!isTransitioning)
            {
                DeleteLots(ExpectedLots);
            }
        }

        public bool ProcessInstantly(in SurroundPuppetTick tick)
        {
            ExpectedLots.Clear();
            ExpectedLots.UnionWith(LotIdToPuppet.Keys);

            foreach (ref var lot in tick.Lots.AsSpan())
            {
                if (LotIdToPuppet.TryGetValue(lot.LotLocation, out var puppets))
                {
                    ExpectedAvatars.Clear();
                    ExpectedAvatars.UnionWith(puppets.Keys);

                    foreach (ref var puppet in lot.Puppets.AsSpan())
                    {
                        if (puppets.TryGetValue(puppet.PersistID, out var visual))
                        {
                            // Check for changes that require instant playback?
                        }
                        else
                        {
                            // New avatar
                            return true;
                        }

                        ExpectedAvatars.Remove(puppet.PersistID);
                    }

                    // Deleted avatar (ignore)
                    // ExpectedAvatars.Count > 0
                }
                else
                {
                    // New lot
                    return true;
                }

                ExpectedLots.Remove(lot.LotLocation);
            }

            // If any lot is deleted
            return ExpectedLots.Count > 0;
        }

        private void EnqueueTick(in SurroundPuppetTick tick)
        {
            TickQueue.Enqueue(tick);

            if (ProcessInstantly(tick))
            {
                Instant = true;
            }
        }

        public void Process(FSOVMSurroundPuppets message)
        {
            foreach (var tick in message.Ticks)
            {
                EnqueueTick(tick);
            }
        }
        
        public bool IsPresentElsewhere(uint pid)
        {
            var vm = Screen.VisualVM;
            if (vm.Ready && !vm.FSOVAsyncLoading && vm.GetObjectByPersist(pid) != null)
            {
                return true;
            }

            foreach (var lot in LotIdToPuppet)
            {
                foreach (var puppet in lot.Value)
                {
                    if (!puppet.Value.IsLeaving)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
