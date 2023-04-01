using FSO.LotView.Model;
using FSO.SimAntics.Model.Routing;
using FSO.SimAntics.Model;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;
using System.Linq;
using FSO.SimAntics.NetPlay.Model.Commands;
using FSO.Common.Utils;
using FSO.Vitaboy;
using FSO.SimAntics.Marshals.Threads;

namespace FSO.SimAntics.Engine
{
    internal struct VMDirectControlState
    {
        public int X;
        public int Z;

        public VMDirectControlInput Input;

        public int XVelocity;
        public int ZVelocity;
        public int IdleFrames;
        public int Reserved;
        public int Reserved2;
    }

    public struct VMDirectControlInput
    {
        public int ID;
        public int InputIntensity; // Measured in percent.
        public short Direction;
        public short LookDirectionInt;
        public Vector3 LookDirectionReal;
        public bool Sprint;
    }

    public class VMPortalObstacle : VMEntityObstacle {
        public VMPortalObstacle(int x1, int y1, int x2, int y2, VMEntity ent) : base(x1, y1, x2, y2, ent) { }
    }

    /// <summary>
    /// Provides direct control over an avatar. Movement is handled by a direction and velocity, with some smooth acceleration + deceleration.
    /// The character cannot walk out of the current room. If they walk into a portal object, they will automatically trigger its portal function.
    /// If the character collides with a wall, they're pushed out of it in a way that simulates sliding.
    /// Returns True/False when any interaction is queued.
    /// 
    /// Also does visual simulation a few frames ahead for latency compensation.
    /// </summary>
    public class VMDirectControlFrame : VMStackFrame
    {
        private const int PRECISION = 0x8000;
        private const int SPRINT_MULTIPLIER = 200; // Measured in percent.
        private const int MAX_SPEED = PRECISION; // Speed is measured in (PRECISION)th subtiles per tick.
        private const int ACCEL = PRECISION / 20; // Rate to get up to top speed.

        private VMDirectControlState State;
        private VMDirectControlInput UserInput;

        private List<VMDirectControlInput> ClientInputs = new List<VMDirectControlInput>();

        // Animations for mixing:
        // 0: idle
        // 1: walk half
        // 2: walk
        // 3: run

        public VMDirectControlFrame()
        {

        }

        public void Init()
        {
            (Caller as VMAvatar)?.SetPersonData(VMPersonDataVariable.Priority, 1);
        }

        public void InitAnimations()
        {
            var obj = (VMAvatar)Caller;

            var pool = VM.Context.RoomInfo[VM.Context.GetRoomAt(Caller.Position)].Room.IsPool;
            var anims = (pool) ? obj.SwimAnimations : obj.WalkAnimations;

            // We set up a very specific collection of animations.
            // The original game gets its walk animation by confusingly combining two of the walk animations and running them at 1.5x speed.
            // We also want to store the standing pose in the first animation slot so that we can blend into and out of it with velocity.

            obj.Animations.Clear();
            var anim = PlayAnim(anims[3], obj); //stand animation
            anim.Weight = 0f;
            anim.Loop = true;

            var hWalkAnim = anims[(pool) ? 21 : (obj.IsPet ? 20 : 25)];
            if (hWalkAnim == "") hWalkAnim = anims[20];
            anim = PlayAnim(hWalkAnim, obj); //Walk Half
            anim.Weight = 0.33f;
            anim.Speed = 1.5f;
            anim.Loop = true;

            anim = PlayAnim(anims[20], obj); //Walk Full
            anim.Weight = 0.66f;
            anim.Speed = 1.5f;
            anim.Loop = true;

            anim = PlayAnim(anims[21], obj); //Run full
            anim.Weight = 0f;
            anim.Speed = 1.5f * (25 / 30f);
            anim.Loop = true;
            anim.CurrentFrame = 12.5f;
        }

        public void SendControls(VMDirectControlInput input)
        {
            State.Input = input;
        }

        public void SendUserControls(VMDirectControlInput input)
        {
            UserInput = input;
        }

        private VMAnimationState PlayAnim(string name, VMAvatar avatar)
        {
            var animation = FSO.Content.Content.Get().AvatarAnimations.Get(name + ".anim");
            var state = new VMAnimationState(animation, false);
            avatar.Animations.Add(state);
            return state;
        }

        private bool WithinRange(VMEntity ent, int x, int y)
        {
            return Math.Abs(x - ent.Position.x) <= 32 && Math.Abs(y - ent.Position.y) <= 32;
        }

        public VMObstacleSet GetObstacles()
        {
            LotTilePos startPos = Caller.Position;

            var myRoom = VM.Context.GetRoomAt(startPos);
            if (myRoom == 0) return new VMObstacleSet();

            var roomInfo = VM.Context.RoomInfo[myRoom];

            int bx = (roomInfo.Room.Bounds.X - 1) << 4;
            int by = (roomInfo.Room.Bounds.Y - 1) << 4;
            int width = (roomInfo.Room.Bounds.Width + 2) << 4;
            int height = (roomInfo.Room.Bounds.Height + 2) << 4;

            var obstacles = new VMObstacleSet(roomInfo.Room.RoutingObstacles);//new List<VMObstacle>();

            obstacles.Add(new VMObstacle(bx - 16, by - 16, bx + width + 16, by));
            obstacles.Add(new VMObstacle(bx - 16, by + height, bx + width + 16, by + height + 16));

            obstacles.Add(new VMObstacle(bx - 16, by - 16, bx, by + height + 16));
            obstacles.Add(new VMObstacle(bx + width, by - 16, bx + width + 16, by + height + 16));

            var considerAvatars = !Caller.GetFlag(VMEntityFlags.AllowPersonIntersection);

            foreach (var obj in roomInfo.Entities)
            {
                var ft = obj.Footprint;

                // TODO: predictive movement for other avatars?

                var flags = (VMEntityFlags)obj.GetValue(VMStackObjectVariable.Flags);
                if (obj != Caller && ft != null &&
                    WithinRange(obj, startPos.x, startPos.y) &&
                    (obj is VMGameObject || considerAvatars) &&
                    ((flags & VMEntityFlags.DisallowPersonIntersection) > 0 || (flags & VMEntityFlags.AllowPersonIntersection) == 0)
                    && (!(Caller.ExecuteEntryPoint(5, VM.Context, true, obj, new short[] { obj.ObjectID, 1, 0, 0 })
                        || obj.ExecuteEntryPoint(5, VM.Context, true, Caller, new short[] { Caller.ObjectID, 1, 0, 0 }))))
                    obstacles.Add(new VMEntityObstacle(ft.x1 - 3, ft.y1 - 3, ft.x2 + 3, ft.y2 + 3, obj));
            }

            foreach (var portal in roomInfo.Portals)
            {
                var obj = VM.GetObjectById(portal.ObjectID);
                var otherPortals = obj.MultitileGroup.Objects.Where(ent => 
                    ent != obj &&
                    (ent.Portal || ((VMPlacementFlags)ent.GetValue(VMStackObjectVariable.PlacementFlags) & (VMPlacementFlags.InAir | VMPlacementFlags.OnFloor)) == VMPlacementFlags.InAir));

                var ft = obj.Footprint;
                obstacles.Add(new VMPortalObstacle(ft.x1 - 3, ft.y1 - 3, ft.x2 + 3, ft.y2 + 3, obj));

                foreach (var otherPortal in otherPortals)
                {
                    if (otherPortal.Position.Level == obj.Position.Level)
                    {
                        ft = otherPortal.Footprint;
                        obstacles.Add(new VMEntityObstacle(ft.x1 - 3, ft.y1 - 3, ft.x2 + 3, ft.y2 + 3, otherPortal));
                    }
                }
            }

            return obstacles;
        }

        private List<VMObstacle> CollisionTest(VMObstacleSet obstacles, int x, int z)
        {
            Point point = new Point(x / PRECISION, z / PRECISION);
            return obstacles.AllIntersect(new VMObstacle(point - new Point(1), point + new Point(1)));
        }

        private bool TryMove(VMObstacleSet obstacles, ref VMDirectControlState state, int framePredict)
        {
            // Calculate player input results
            ref var input = ref state.Input;

            if (input.InputIntensity > 0)
            {
                // Move in the direction specified by the user.

                double x = Math.Sin(Math.PI * input.Direction / 32767.0);
                double z = -Math.Cos(Math.PI * input.Direction / 32767.0);

                int accel = ACCEL * (input.Sprint ? 6 : 2);

                state.XVelocity += (int)(accel * x);
                state.ZVelocity += (int)(accel * z);
                state.IdleFrames = 0;
            }
            else
            {
                if (state.IdleFrames < 20)
                {
                    state.IdleFrames++;
                }
            }

            // Friction

            int multiplier = Math.Min(9, 20 - state.IdleFrames);

            state.XVelocity = (int)((state.XVelocity * multiplier) / 10L);
            state.ZVelocity = (int)((state.ZVelocity * multiplier) / 10L);

            int steps;
            if (state.XVelocity == 0 && state.ZVelocity == 0)
            {
                steps = 0;
            }
            if (Math.Abs(state.XVelocity) < 25000 && Math.Abs(state.ZVelocity) < 2500)
            {
                steps = 1;
            }
            else
            {
                steps = 3;
            }

            int currentX = state.X;
            int currentZ = state.Z;
            int newX = currentX;
            int newZ = currentZ;

            // Calculate movement
            for (int step = 0; step < steps; step++)
            {
                int xVel = state.XVelocity / steps;
                int zVel = state.ZVelocity / steps;

                if (steps > 1 && step == steps - 1)
                {
                    xVel = state.XVelocity - xVel * (steps - 1);
                    zVel = state.ZVelocity - zVel * (steps - 1);
                }

                VMEntity portal = null;
                newX = currentX + xVel;
                newZ = currentZ + zVel;

                var collisions = CollisionTest(obstacles, newX, newZ);

                if (collisions.Count > 0)
                {
                    // Couldn't move the full way due to a collision. Try to slide off of the collided obstacle.
                    var srcPoint = new Point(currentX, currentZ);

                    foreach (var col in collisions.OrderBy(col => col is VMEntityObstacle ? 0 : 1))
                    {
                        var point = new Point(newX, newZ);

                        if (!(col is VMPortalObstacle) && col.HardContainsHiP(point))
                        {
                            Point closest = col.ClosestEdgeContainedHiP(point.X, point.Y);

                            if (closest.X - point.X != 0)
                            {
                                // Closest to an X edge, limit X movement.

                                newX = closest.X;
                            }
                            else
                            {
                                newZ = closest.Y;
                            }

                            if (col is VMEntityObstacle ent)
                            {
                                // Does this entity have a portal?
                                var group = ent.Parent.MultitileGroup;

                                if (group.Objects.Any(obj => obj.Portal))
                                {
                                    var currentPos = CollisionTest(obstacles, currentX, currentZ);

                                    foreach (var pcol in currentPos)
                                    {
                                        if ((pcol is VMPortalObstacle portalObs) && portalObs.Parent.MultitileGroup == group)
                                        {
                                            portal = portalObs.Parent;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    var newPoint = new Point(newX, newZ);

                    if (CollisionTest(obstacles, newX, newZ).Any(obstacle => !(obstacle is VMPortalObstacle) && obstacle.HardContainsHiP(newPoint)))
                    {
                        // Don't move anywhere.
                        state.XVelocity /= 2;
                        state.ZVelocity /= 2;

                        return false;
                    }
                }

                if (portal != null && framePredict == 0)
                {
                    State = new VMDirectControlState();

                    sbyte portalLevel = portal.Position.Level;

                    // Face the center of the portal (at least, the tiles on this floor)
                    int xSum = 0;
                    int ySum = 0;
                    int total = 0;
                    foreach (var obj in portal.MultitileGroup.Objects)
                    {
                        if (obj.Position.Level == portalLevel && obj.Position != LotTilePos.OUT_OF_WORLD)
                        {
                            xSum += obj.Position.x;
                            ySum += obj.Position.y;
                            total++;
                        }
                    }

                    if (total == 0)
                    {
                        Caller.SetPosition(portal.Position, Direction.NORTH, VM.Context);
                    }
                    else
                    {
                        var direction = DirectionUtils.Normalize(Math.Atan2(xSum / total - portal.Position.x, portal.Position.y - ySum / total));

                        var result = (int)Math.Round((DirectionUtils.PosMod(direction, Math.PI * 2) / Math.PI) * 4);

                        Caller.SetPosition(portal.Position, (Direction)(1 << result), VM.Context);
                    }

                    PushEntryPoint(15, portal);

                    return true;
                }

                currentX = newX;
                currentZ = newZ;
            }

            // Try and move to the new location.
            var level = Caller.Position.Level;
            float lastDir = Caller.RadianDirection;

            if (framePredict != 0 || Caller.SetPosition(new LotTilePos((short)(newX / PRECISION), (short)(newZ / PRECISION), level), Direction.NORTH, VM.Context).Status == VMPlacementError.Success)
            {
                int xVelocity = newX - state.X;
                int zVelocity = newZ - state.Z;

                Caller.RadianDirection = lastDir;
                Caller.VisualPosition = new Vector3(state.X / (16f * PRECISION), state.Z / (16f * PRECISION), (level - 1) * 2.95f);
                (Caller as VMAvatar).VisualPositionStart = Caller.VisualPosition;
                (Caller as VMAvatar).Velocity = new Vector3(xVelocity / (16f * PRECISION), zVelocity / (16f * PRECISION), 0);

                state.X = newX;
                state.Z = newZ;
            }

            return false;
        }

        private int TickN = 0;
        private int LastLookaheads = 0;

        private void ProcessClientInputs(Tuple<float, bool> directionInputs)
        {
            if (VM.MyUID == Caller.PersistID)
            {
                VMDirectControlState dupeState = State;
                int lookaheads = 0;

                for (int i = 0; i < ClientInputs.Count; i++)
                {
                    var input = ClientInputs[i];

                    if (State.Input.ID - input.ID >= 0)
                    {
                        ClientInputs.RemoveAt(i--);
                        continue;
                    }

                    dupeState.Input = input;

                    var obstacles = GetObstacles();

                    TryMove(obstacles, ref dupeState, ++lookaheads);
                }

                int diff = LastLookaheads - lookaheads;

                for (int i = 0; i < diff; i++)
                {
                    var obstacles = GetObstacles();

                    dupeState.Input = UserInput;
                    TryMove(obstacles, ref dupeState, lookaheads + i + 1);
                }

                if (lookaheads > LastLookaheads || lookaheads == 0)
                {
                    LastLookaheads = lookaheads;
                }
                else
                {
                    TickN += Math.Abs(diff);
                    if (TickN > 30)
                    {
                        TickN = 0;
                        LastLookaheads--;
                    }
                }

                UpdateAnimation(ref dupeState, directionInputs);
            }
            else
            {
                UpdateAnimation(ref State, directionInputs);
            }
        }

        private Tuple<float, bool> UpdateDirection()
        {
            var avatar = (VMAvatar)Caller;

            float velocity = (float)Math.Sqrt(State.XVelocity * (float)State.XVelocity + State.ZVelocity * (float)State.ZVelocity);

            // Calculate direction

            // Walking direction control: Accept body facing directions within x degrees of the look direction, where x changes with move speed.
            // Body facing direction is influenced by movement first, limits second.

            // When body direction is out of range, move it into range quickly.
            // When moving, move to facing direction limited by range.

            float idleRange = (float)Math.PI / 6;
            float moveRange = (float)Math.PI / 2;

            float movementDirection = (float)Math.Atan2(State.XVelocity, -State.ZVelocity);
            float facingDirection = (float)Math.PI * State.Input.LookDirectionInt / 32767f;

            if (Math.Abs(DirectionUtils.Difference(facingDirection, movementDirection)) > Math.PI / 2)
            {
                // Flip movement direction.
                movementDirection = (float)DirectionUtils.Normalize(movementDirection - Math.PI);
            }

            float velocityComponent = Math.Min(1, velocity / 1000);
            float targetRange = MathHelper.Lerp(idleRange, moveRange, velocityComponent);

            float currentToMovementDist = (float)DirectionUtils.Difference(avatar.RadianDirection, movementDirection);

            float targetDirection = (float)DirectionUtils.Normalize(avatar.RadianDirection + currentToMovementDist * velocityComponent);

            float targetClamped = (float)DirectionUtils.Normalize(facingDirection + MathHelper.Clamp((float)DirectionUtils.Difference(facingDirection, targetDirection), -targetRange, targetRange));

            float finalMoveDelta = (float)DirectionUtils.Difference(avatar.RadianDirection, targetClamped) / (6 - velocityComponent * 2);

            avatar.RadianDirection = (float)DirectionUtils.Normalize(avatar.RadianDirection + finalMoveDelta);

            bool forward = Vector2.Dot(new Vector2((float)Math.Sin(avatar.RadianDirection), -(float)Math.Cos(avatar.RadianDirection)), new Vector2(State.XVelocity, State.ZVelocity)) >= 0;

            if (State.Input.LookDirectionReal != Vector3.Zero)
            {
                avatar.Avatar.HeadSeekTarget = Animator.CalculateHeadSeek(avatar.Avatar, State.Input.LookDirectionReal * 100, avatar.RadianDirection);
                avatar.Avatar.HeadSeek = avatar.Avatar.HeadSeekTarget;
                avatar.Avatar.HeadSeekWeight = 30f;
            }

            return new Tuple<float, bool>(finalMoveDelta, forward);
        }

        private void UpdateAnimation(ref VMDirectControlState state, Tuple<float, bool> directionInputs)
        {
            var avatar = (VMAvatar)Caller;
            if (avatar.Animations.Count != 4)
            {
                InitAnimations();
            }

            float velocity = (float)Math.Sqrt(state.XVelocity * (float)state.XVelocity + state.ZVelocity * (float)state.ZVelocity) + Math.Abs(directionInputs.Item1) * PRECISION * 2;
            bool forward = directionInputs.Item2;

            // Calculate animations

            var anims = avatar.Animations;

            int midSpeed = PRECISION;
            int qtrSpeed = midSpeed / 2;

            anims[0].Weight = Math.Max(0.001f, Math.Min(1, (qtrSpeed - velocity) / qtrSpeed));
            anims[1].Weight = Math.Max(0.001f, Math.Min(1, 1 - Math.Abs(qtrSpeed - velocity) / qtrSpeed));
            anims[3].Weight = Math.Max(0.001f, Math.Min(1, (velocity - midSpeed) / midSpeed));
            anims[2].Weight = Math.Max(0.001f, 1 - (avatar.Animations[0].Weight + avatar.Animations[1].Weight + avatar.Animations[3].Weight));

            float halfWalkModifier = 30 / 38f;
            float runModifier = 25 / 38f;

            anims[1].PlayingBackwards = !forward;
            anims[2].PlayingBackwards = !forward;
            anims[3].PlayingBackwards = !forward;

            float baseSpeed = 1.5f;
            baseSpeed *= 0.5f * anims[0].Weight + (1 / halfWalkModifier) * anims[1].Weight + anims[2].Weight + (1 / runModifier) * anims[3].Weight;

            anims[1].Speed = baseSpeed * halfWalkModifier; // runs at 30fps
            anims[2].Speed = baseSpeed; // runs at 38fps
            anims[3].Speed = baseSpeed * runModifier; //runs at 25fps
        }

        public VMPrimitiveExitCode Tick()
        {
            VM.Context.NextRandom(1); //rng cycle - for desync detect
            var avatar = (VMAvatar)Caller;

            if (avatar.GetPersonData(VMPersonDataVariable.Posture) != 0)
            {
                return VMPrimitiveExitCode.RETURN_TRUE;
            }

            State.X = (State.X % PRECISION) + avatar.Position.x * PRECISION;
            State.Z = (State.Z % PRECISION) + avatar.Position.y * PRECISION;

            TickN++;

            bool sprint = State.Input.Sprint;

            var obstacles = GetObstacles();

            if (TryMove(obstacles, ref State, 0))
            {
                avatar.Avatar.HeadSeekWeight = 0f;

                avatar.SetValue(VMStackObjectVariable.WalkStyle, (short)(sprint ? 1 : 0));

                return VMPrimitiveExitCode.CONTINUE;
            }

            var directionOutputs = UpdateDirection();

            // Animation is allowed to desync.
            ProcessClientInputs(directionOutputs);

            bool notified = (Thread.ActiveAction.NotifyIdle || Thread.Queue.Any(interaction => interaction.Mode != VMQueueMode.Idle));

            if (VM.MyUID == Caller.PersistID)
            {
                VM.SendCommand(new VMNetDirectControlCommand() { Input = UserInput });

                ClientInputs.Add(UserInput);
            }

            if (notified)
            {
                avatar.Avatar.HeadSeekWeight = 0f;
                avatar.SetValue(VMStackObjectVariable.WalkStyle, 0);
            }

            return notified ? VMPrimitiveExitCode.RETURN_TRUE : VMPrimitiveExitCode.CONTINUE_NEXT_TICK;
        }

        private bool PushEntryPoint(int entryPoint, VMEntity ent)
        {
            if (ent.EntryPoints[entryPoint].ActionFunction != 0)
            {
                bool Execute;
                if (ent.EntryPoints[entryPoint].ConditionFunction != 0) //check if we can definitely execute this...
                {
                    var Behavior = ent.GetRoutineWithOwner(ent.EntryPoints[entryPoint].ConditionFunction, VM.Context);
                    if (Behavior != null)
                    {
                        Execute = (VMThread.EvaluateCheck(VM.Context, Caller, new VMStackFrame()
                        {
                            Caller = Caller,
                            Callee = ent,
                            CodeOwner = Behavior.owner,
                            StackObject = ent,
                            Routine = Behavior.routine,
                            Args = new short[4]
                        }) == VMPrimitiveExitCode.RETURN_TRUE);
                    }
                    else Execute = true;

                }
                else
                {
                    Execute = true;
                }

                if (Execute)
                {
                    //push it onto our stack, except now the object owns our soul! when we are returned to we can evaluate the result and determine if the action failed.
                    var Behavior = ent.GetRoutineWithOwner(ent.EntryPoints[entryPoint].ActionFunction, VM.Context);
                    if (Behavior == null) return false; //invalid id
                    var routine = Behavior.routine;
                    var childFrame = new VMStackFrame
                    {
                        Routine = routine,
                        Caller = Caller,
                        Callee = ent,
                        CodeOwner = Behavior.owner,
                        StackObject = ent,
                        ActionTree = ActionTree
                    };
                    childFrame.Args = new short[routine.Arguments];
                    Thread.Push(childFrame);
                    return true;
                }
                else
                {
                    return false; //could not execute portal function.
                }
            }
            else
            {
                return false;
            }
        }

        #region VM Marshalling Functions
        public override VMStackFrameMarshal Save()
        {
            var start = base.Save();

            return new VMDirectControlFrameMarshal
            {
                RoutineID = start.RoutineID,
                InstructionPointer = start.InstructionPointer,
                Caller = start.Caller,
                Callee = start.Callee,
                StackObject = start.StackObject,
                CodeOwnerGUID = start.CodeOwnerGUID,
                Locals = start.Locals,
                Args = start.Args,
                ActionTree = start.ActionTree,
                //above is stack frame stuff

                State = State
            };
        }

        public override void Load(VMStackFrameMarshal input, VMContext context)
        {
            base.Load(input, context);

            var inD = (VMDirectControlFrameMarshal)input;

            State = inD.State;
        }

        public VMDirectControlFrame(VMStackFrameMarshal input, VMContext context, VMThread thread)
        {
            Thread = thread;
            Load(input, context);
        }
        #endregion
    }
}
