/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.SimAntics.Model;
using Microsoft.Xna.Framework;
using FSO.LotView.Model;
using FSO.Files.Formats.IFF.Chunks;
using FSO.LotView.Components;
using FSO.Vitaboy;
using FSO.SimAntics.Utils;
using FSO.Common.Utils;
using FSO.SimAntics.Engine.Routing;
using FSO.SimAntics.Model.Routing;
using FSO.SimAntics.Primitives;

namespace FSO.SimAntics.Engine
{
    /// <summary>
    /// Determines the path to a set destination from an avatar and provides a system to walk them there. First finds the
    /// set of room portals to get to the target's room (calls their portal functions in order), then pathfinds to the relevant 
    /// position once in the same room.
    /// 
    /// If in the same room or a flag is set to "ignore rooms", the room step is ignored and the character is directly routed 
    /// to the specified position. Pathfinders are normally pushed to the stack, but behave completely differently from
    /// stack frames. You can imagine an example case, where the avatar is currently routing to a door, but the final destination
    /// is the piano.
    /// 
    /// STACK:
    /// Private: Interaction - Play (piano)
    /// Private: route to piano and sit (piano)
    /// VMPathFinder: Go on top of, opposing direction. (this is passed as a position and direction.)
    /// Semi-global: Portal function (door)
    /// Semi-global: Goto door and gace through (door)
    /// VMPathFinder: Go on top of, same direction. (passed as position direction, but this time destination is in same room, so we call no portal functions)
    /// 
    /// Once the first portal function returns true, we will pop back to the initial VMPathFinder to route to the second or the object.
    /// This can repeat many times. 
    /// 
    /// However, if a portal function returns false, we will need to re-evaluate the portal route to the
    /// destination, discounting the route from the previous portal to the portal we failed to route to. (nodes are no longer connected) 
    /// The Sims 1 does not do this, but routing problems annoy me as much as everyone else. :)
    /// </summary>
    public class VMRoutingFrame : VMStackFrame
    {

        private static ushort ROUTE_FAIL_TREE = 398;
        private static uint GOTO_GUID = 0x000007C4;
        private static short SHOO_INTERACTION = 3;
        private static ushort SHOO_TREE = 4107;

        public Stack<VMRoomPortal> Rooms;
        public LinkedList<Point> WalkTo;
        private double WalkDirection;
        private short WalkStyle;
        private double TargetDirection;
        private bool Walking = false;
        private bool IgnoreRooms;

        public VMRoutingFrameState State = VMRoutingFrameState.INITIAL;
        public int PortalTurns = 0;
        public int WaitTime = 0;

        private bool AttemptedChair = false;
        private float TurnTweak = 0;
        private int TurnFrames = 0;

        private int MoveTotalFrames = 0;
        private int MoveFrames = 0;
        private int Velocity = 0;

        public bool CallFailureTrees = false;

        private HashSet<VMAvatar> AvatarsToConsider = new HashSet<VMAvatar>();

        private LotTilePos PreviousPosition;
        private LotTilePos CurrentWaypoint = LotTilePos.OUT_OF_WORLD;

        public List<VMFindLocationResult> Choices;
        public VMFindLocationResult CurRoute;

        public bool InitRoutes(List<VMFindLocationResult> choices) //returns false if we can't find a single route
        {
            var parent = GetParentFrame();
            if (parent != null)
            {
                AvatarsToConsider = new HashSet<VMAvatar>(parent.AvatarsToConsider);
            }
            else
            {
                foreach (var obj in VM.Entities)
                {
                    if (obj is VMAvatar)
                    {
                        var colAvatar = (VMAvatar)obj;
                        var colTopFrame = colAvatar.Thread.Stack.LastOrDefault();

                        if (colTopFrame != null && colTopFrame is VMRoutingFrame)
                        {
                            var colRoute = (VMRoutingFrame)colTopFrame;
                            if (colRoute.State == VMRoutingFrameState.WAIT_LET_BY || colRoute.State == VMRoutingFrameState.WAIT_SHOO) AvatarsToConsider.Add(colAvatar);
                        }
                    }
                }
            }

            Choices = choices; //should be ordered by most preferred first, with a little bit of random shuffling to keep things interesting for "wander"
            //style movements. Also includes flags dictating if this route goes through walls etc.
            var found = VMRouteFailCode.NoValidGoals;
            while (found != VMRouteFailCode.Success && Choices.Count > 0) {
                found = AttemptRoute(Choices[0]);
                Choices.RemoveAt(0);
            }
            if (found == VMRouteFailCode.Success) return true;
            else HardFail(found, null);
            return false;
        }

        public void SoftFail(VMRouteFailCode code, VMEntity blocker)
        {
            var found = VMRouteFailCode.NoValidGoals;
            while (found != VMRouteFailCode.Success && Choices.Count > 0)
            {
                found = AttemptRoute(Choices[0]);
                Choices.RemoveAt(0);
            }

            if (found != VMRouteFailCode.Success) HardFail(code, blocker);
        }

        private void HardFail(VMRouteFailCode code, VMEntity blocker)
        {
            State = VMRoutingFrameState.FAILED;
            if (CallFailureTrees)
            {
                ((VMAvatar)Caller).SetPersonData(VMPersonDataVariable.Priority, 100); //TODO: what is this meant to be? what dictates it? 
                //probably has to do with interaction priority.
                //we just set it to 100 here so that failure trees work.
                var bhav = Global.Resource.Get<BHAV>(ROUTE_FAIL_TREE);
                Thread.ExecuteSubRoutine(this, bhav, CodeOwner, new VMSubRoutineOperand(new short[] { (short)code, (blocker==null)?(short)0:blocker.ObjectID, 0, 0 }));
            }
        }

        private VMRouteFailCode AttemptRoute(VMFindLocationResult route) { //returns false if there is no room portal route to the destination room.
            CurRoute = route;

            WalkTo = null; //reset routing state
            Walking = false;
            AttemptedChair = false;
            TurnTweak = 0;

            var avatar = (VMAvatar)Caller;

            //if we are routing to a chair, let it take over.
            if (route.Chair != null)
            {
                AttemptedChair = false;
                return VMRouteFailCode.Success;
            }

            Rooms = new Stack<VMRoomPortal>();

            var DestRoom = VM.Context.GetRoomAt(route.Position);
            var MyRoom = VM.Context.GetRoomAt(avatar.Position);

            IgnoreRooms = (route.Flags & SLOTFlags.IgnoreRooms) > 0;

            if (DestRoom == MyRoom || IgnoreRooms) return VMRouteFailCode.Success; //we don't have to do any room finding for this
            else {
                //find shortest room traversal to destination. Simple A* pathfind.
                //Portals are considered nodes to allow multiple portals between rooms to be considered.

                var openSet = new List<VMRoomPortal>(); //we use this like a queue, but we need certain functions for sorted queue that are only provided by list.
                var closedSet = new HashSet<VMRoomPortal>();

                var gScore = new Dictionary<VMRoomPortal, double>();
                var fScore = new Dictionary<VMRoomPortal, double>();
                var parents = new Dictionary<VMRoomPortal, VMRoomPortal>();

                var StartPortal = new VMRoomPortal(Caller.ObjectID, MyRoom); //consider the sim as a portal to this room (as a starting point)
                openSet.Add(StartPortal);
                gScore[StartPortal] = 0;
                fScore[StartPortal] = GetDist(Caller.Position, route.Position);

                while (openSet.Count != 0) {
                    var current = openSet[0];
                    openSet.RemoveAt(0);

                    if (current.TargetRoom == DestRoom) {
                        //this portal gets us to the room.
                        while (current != StartPortal) //push previous portals till we get to our first "portal", the sim in its current room (we have already "traversed" this portal)
                        {
                            Rooms.Push(current);
                            current = parents[current];
                        }
                        return VMRouteFailCode.Success;
                    }

                    closedSet.Add(current);

                    var portals = VM.Context.RoomInfo[current.TargetRoom].Portals;

                    foreach (var portal in portals) { //evaluate all neighbor portals
                        if (closedSet.Contains(portal)) continue; //already evaluated!

                        var pos = VM.GetObjectById(portal.ObjectID).Position;
                        var gFromCurrent = gScore[current] + GetDist(VM.GetObjectById(current.ObjectID).Position, pos);
                        var newcomer = !openSet.Contains(portal);

                        if (newcomer || gFromCurrent < gScore[portal]) { 
                            parents[portal] = current; //best parent for now
                            gScore[portal] = gFromCurrent;
                            fScore[portal] = gFromCurrent + GetDist(pos, route.Position);
                            if (newcomer) { //add and move to relevant position
                                OpenSetSortedInsert(openSet, fScore, portal);
                            } else { //remove and reinsert to refresh sort
                                openSet.Remove(portal);
                                OpenSetSortedInsert(openSet, fScore, portal);
                            }
                        }
                    }
                }

                return VMRouteFailCode.NoRoomRoute;
            }
        }

        /// <summary>
        /// Pathfinds to the destination position from the current. The room pathfind should get us to the same room before we do this.
        /// </summary>
        private bool AttemptWalk() 
        {
            //find shortest path to destination tile. Simple A* pathfind.
            //portals are used to traverse floors, so we do not care about the floor each point is on.
            //when evaluating possible adjacent tiles we use the Caller's current floor.

            LotTilePos startPos = Caller.Position;
            CurrentWaypoint = LotTilePos.OUT_OF_WORLD;
            var myRoom = VM.Context.GetRoomAt(startPos);

            var roomInfo = VM.Context.RoomInfo[myRoom];
            var obstacles = new List<VMObstacle>();

            int bx = (roomInfo.Room.Bounds.X-1) << 4;
            int by = (roomInfo.Room.Bounds.Y-1) << 4;
            int width = (roomInfo.Room.Bounds.Width+2) << 4;
            int height = (roomInfo.Room.Bounds.Height+2) << 4;
            obstacles.Add(new VMObstacle(bx-16, by-16, bx+width+16, by));
            obstacles.Add(new VMObstacle(bx-16, by+height, bx+width+16, by+height+16));

            obstacles.Add(new VMObstacle(bx-16, by-16, bx, by+height+16));
            obstacles.Add(new VMObstacle(bx+width, by-16, bx+width+16, by+height+16));

            foreach (var obj in roomInfo.Entities)
            {
                var ft = obj.Footprint;

                var flags = (VMEntityFlags)obj.GetValue(VMStackObjectVariable.Flags);
                if (ft != null && (obj is VMGameObject || AvatarsToConsider.Contains(obj)) && ((flags & VMEntityFlags.DisallowPersonIntersection) > 0 || (flags & VMEntityFlags.AllowPersonIntersection) == 0))
                    obstacles.Add(new VMObstacle(ft.x1-3, ft.y1-3, ft.x2+3, ft.y2+3));
            }

            obstacles.AddRange(roomInfo.Room.WallObs);
            if (!IgnoreRooms) obstacles.AddRange(roomInfo.Room.RoomObs);

            var router = new VMRectRouter(obstacles);
            
            var startPoint = new Point((int)startPos.x, (int)startPos.y);
            var endPoint = new Point((int)CurRoute.Position.x, (int)CurRoute.Position.y);

            if (startPoint == endPoint)
            {
                WalkTo = new LinkedList<Point>();
                return true;
            }

            WalkTo = router.Route(startPoint, endPoint);
            if (WalkTo != null)
            {
                if (WalkTo.First.Value != endPoint) WalkTo.RemoveFirst();
            }

            return (WalkTo != null);
        }

        private void OpenSetSortedInsert(List<VMRoomPortal> set, Dictionary<VMRoomPortal, double> fScore, VMRoomPortal portal)
        {
            var myScore = fScore[portal];
            for (int i = 0; i < set.Count; i++)
            {
                if (myScore < fScore[set[i]])
                {
                    set.Insert(i, portal);
                    return;
                }
            }
            set.Add(portal);
        }

        private double GetDist(LotTilePos pos1, LotTilePos pos2)
        {
            return Math.Sqrt(Math.Pow(pos1.x - pos2.x, 2) + Math.Pow(pos1.y - pos2.y, 2))/16.0 + Math.Abs(pos1.Level-pos2.Level)*10;
        }

        private bool PushEntryPoint(int entryPoint, VMEntity ent) {
            if (ent.EntryPoints[entryPoint].ActionFunction != 0)
            {
                bool Execute;
                if (ent.EntryPoints[entryPoint].ConditionFunction != 0) //check if we can definitely execute this...
                {
                    var Behavior = ent.GetBHAVWithOwner(ent.EntryPoints[entryPoint].ConditionFunction, VM.Context);
                    Execute = (VMThread.EvaluateCheck(VM.Context, Caller, new VMQueuedAction()
                    {
                        Callee = ent,
                        CodeOwner = Behavior.owner,
                        StackObject = ent,
                        Routine = VM.Assemble(Behavior.bhav),
                    }) == VMPrimitiveExitCode.RETURN_TRUE);

                }
                else
                {
                    Execute = true;
                }

                if (Execute)
                {
                    //push it onto our stack, except now the object owns our soul! when we are returned to we can evaluate the result and determine if the action failed.
                    var Behavior = ent.GetBHAVWithOwner(ent.EntryPoints[entryPoint].ActionFunction, VM.Context);
                    var routine = VM.Assemble(Behavior.bhav);
                    var childFrame = new VMStackFrame
                    {
                        Routine = routine,
                        Caller = Caller,
                        Callee = ent,
                        CodeOwner = Behavior.owner,
                        StackObject = ent
                    };
                    childFrame.Args = new short[routine.Arguments];
                    Thread.Push(childFrame);
                    return true;
                }
                else
                {
                    return false; //could not execute portal function. todo: re-evaluate room route
                }
            }
            else
            {
                return false;
            }
        }

        public VMPrimitiveExitCode Tick()
        {
            if (State == VMRoutingFrameState.FAILED)
            {
                return VMPrimitiveExitCode.RETURN_FALSE;
            }

            var avatar = (VMAvatar)Caller;
            avatar.Velocity = new Vector3(0, 0, 0);
            if (WaitTime > 0)
            {
                WaitTime--;
                return VMPrimitiveExitCode.CONTINUE_NEXT_TICK;
            }

            if (State == VMRoutingFrameState.WAIT_LET_BY)
            {
                Velocity = 0;
                State = VMRoutingFrameState.WALKING;
                StartWalkAnimation();
            }

            if (CurRoute.Chair != null) {
                if (!AttemptedChair)
                {
                    AttemptedChair = true;
                    if (PushEntryPoint(26, CurRoute.Chair)) return VMPrimitiveExitCode.CONTINUE;
                    else
                    {
                        SoftFail(VMRouteFailCode.CantSit, null);
                        return VMPrimitiveExitCode.CONTINUE;
                    }
                } else
                {
                    if (Thread.LastStackExitCode == VMPrimitiveExitCode.RETURN_TRUE) return VMPrimitiveExitCode.RETURN_TRUE;
                    else {
                        SoftFail(VMRouteFailCode.CantSit, null);
                        return VMPrimitiveExitCode.CONTINUE;
                    }
                }
            }
            //If we are sitting, and the target is not this seat we need to call the stand function on the object we are contained within.

            if (avatar.GetPersonData(VMPersonDataVariable.Posture) == 1)
            {
                //push it onto our stack, except now the portal owns our soul! when we are returned to we can evaluate the result and determine if the route failed.
                var chair = Caller.Container;

                if (chair == null) avatar.SetPersonData(VMPersonDataVariable.Posture, 0); //we're sitting, but are not bound to a chair. Just instantly get up..
                else
                {
                    if (!PushEntryPoint(27, chair)) //27 is stand. TODO: set up an enum for these
                        HardFail(VMRouteFailCode.CantStand, null);
                    return VMPrimitiveExitCode.CONTINUE;
                }
            }

            if (Rooms.Count > 0)
            { //push portal function of next portal
                var portal = Rooms.Pop();
                var ent = VM.GetObjectById(portal.ObjectID);
                State = VMRoutingFrameState.ROOM_PORTAL;
                if (!PushEntryPoint(15, ent)) //15 is portal function
                    SoftFail(VMRouteFailCode.NoRoomRoute, null); //could not execute portal function
                return VMPrimitiveExitCode.CONTINUE;
            }
            else
            { //direct routing to a position - all required portals have been reached.
                if (State == VMRoutingFrameState.BEGIN_TURN || State == VMRoutingFrameState.END_TURN)
                {
                    if (avatar.CurrentAnimationState.EndReached) {
                        if (State == VMRoutingFrameState.BEGIN_TURN)
                        {
                            State = VMRoutingFrameState.WALKING;
                            WalkDirection = TargetDirection;
                            avatar.RadianDirection = (float)TargetDirection;
                            StartWalkAnimation();
                            return VMPrimitiveExitCode.CONTINUE;
                        } else
                        {
                            if (!CurRoute.FaceAnywhere) avatar.RadianDirection = CurRoute.RadianDirection;
                            return VMPrimitiveExitCode.RETURN_TRUE; //we are here!
                        }
                    }
                    else
                    {
                        avatar.RadianDirection += TurnTweak; //while we're turning, adjust our direction
                        return VMPrimitiveExitCode.CONTINUE_NEXT_TICK;
                    }
                    
                }
                else
                {
                    if (WalkTo == null)
                    {
                        if (AttemptWalk()) return VMPrimitiveExitCode.CONTINUE;
                        else
                        {
                            SoftFail(VMRouteFailCode.NoPath, null);
                            return VMPrimitiveExitCode.CONTINUE;
                        }
                    }
                    else
                    {
                        if (!Walking)
                        {
                            WalkStyle = avatar.GetValue(VMStackObjectVariable.WalkStyle);
                            var remains = AdvanceWaypoint();
                            if (!remains)
                            {
                                if (EndWalk()) return VMPrimitiveExitCode.RETURN_TRUE;
                            }
                            else BeginWalk();
                            return VMPrimitiveExitCode.CONTINUE;
                        }
                        else
                        {
                            if (WalkTo.Count == 0 && MoveTotalFrames-MoveFrames <= 28) //7+6+5+4...
                            {
                                //tail off
                                if (Velocity > 1) Velocity--;
                            }
                            else
                            {
                                //get started
                                if (Velocity < 8) Velocity++;
                            }

                            MoveFrames += Velocity;
                            if (MoveFrames >= MoveTotalFrames)
                            {
                                var remains = AdvanceWaypoint();
                                if (!remains)
                                {
                                    if (EndWalk()) return VMPrimitiveExitCode.RETURN_TRUE;
                                }
                            }
                            else
                            {
                                if (avatar.CurrentAnimationState.EndReached) StartWalkAnimation();
                                //normal sims can move 0.05 units in a frame.

                                if (TurnFrames > 0)
                                {
                                    avatar.RadianDirection = (float)(TargetDirection + DirectionUtils.Difference(TargetDirection, WalkDirection) * (TurnFrames / 10.0));
                                    TurnFrames--;
                                }
                                else avatar.RadianDirection = (float)TargetDirection;


                                var diff = CurrentWaypoint - PreviousPosition;
                                diff.x = (short)((diff.x * MoveFrames) / MoveTotalFrames);
                                diff.y = (short)((diff.y * MoveFrames) / MoveTotalFrames);

                                var storedDir = avatar.RadianDirection;
                                var result = Caller.SetPosition(PreviousPosition + diff, Direction.NORTH, VM.Context);
                                avatar.RadianDirection = storedDir;
                                if (result.Status != VMPlacementError.Success && result.Status != VMPlacementError.CantBeThroughWall) {
                                    //route failure, either something changed or we hit an avatar
                                    //on both cases try again, but if we hit an avatar then detect if they have a routing frame and stop us for a bit.
                                    //we stop the first collider because in most cases they're walking across our walk direction and the problem will go away after a second once they're past.
                                    //
                                    //if we need to route around a stopped avatar we add them to our "avatars to consider" set.

                                    bool routeAround = true;
                                    VMRoutingFrame colRoute = null;

                                    if (result.Object != null && result.Object is VMAvatar)
                                    {
                                        var colAvatar = (VMAvatar)result.Object; 
                                        var colTopFrame = colAvatar.Thread.Stack.LastOrDefault();

                                        if (colTopFrame != null && colTopFrame is VMRoutingFrame)
                                        {
                                            colRoute = (VMRoutingFrame)colTopFrame;
                                            routeAround = (colRoute.State == VMRoutingFrameState.WAIT_LET_BY || colRoute.State == VMRoutingFrameState.WAIT_SHOO);
                                        }
                                        if (routeAround) AvatarsToConsider.Add(colAvatar);
                                    }

                                    if (routeAround)
                                    {
                                        var oldWalk = new LinkedList<Point>(WalkTo);
                                        if (AttemptWalk()) return VMPrimitiveExitCode.CONTINUE;
                                        else
                                        {
                                            //failed to walk around the object
                                            if (result.Object is VMAvatar)
                                            {
                                                WalkTo = oldWalk;
                                                //if they're a person, shoo them away.
                                                //if they're in a routing frame we can push the tree directly onto their stack
                                                //otherwise we push an interaction and just hope they move (todo: does tso do this?)

                                                VMEntity callee = VM.Context.CreateObjectInstance(GOTO_GUID, new LotTilePos(result.Object.Position), Direction.NORTH).Objects[0];
                                                if (colRoute != null)
                                                {
                                                    colRoute.WalkTo = null;
                                                    colRoute.AvatarsToConsider.Add(avatar); //just to make sure they don't try route through us.

                                                    var tree = callee.GetBHAVWithOwner(SHOO_TREE, VM.Context);
                                                    result.Object.Thread.ExecuteSubRoutine(colRoute, tree.bhav, tree.owner, new VMSubRoutineOperand());

                                                    WalkInterrupt(60);
                                                }
                                                else
                                                {
                                                    callee.PushUserInteraction(SHOO_INTERACTION, result.Object, VM.Context);
                                                    WalkInterrupt(60);
                                                }
                                                return VMPrimitiveExitCode.CONTINUE;
                                            }
                                            SoftFail(VMRouteFailCode.DestTileOccupied, result.Object);
                                            return VMPrimitiveExitCode.CONTINUE;
                                        }
                                    }
                                    else
                                    {
                                        WalkInterrupt(30);
                                    }

                                }
                                Caller.VisualPosition = Vector3.Lerp(PreviousPosition.ToVector3(), CurrentWaypoint.ToVector3(), MoveFrames / (float)MoveTotalFrames);

                                var velocity = Vector3.Lerp(PreviousPosition.ToVector3(), CurrentWaypoint.ToVector3(), Velocity / (float)MoveTotalFrames) - PreviousPosition.ToVector3();
                                velocity.Z = 0;
                                avatar.Velocity = velocity;
                            }
                        }
                        return VMPrimitiveExitCode.CONTINUE_NEXT_TICK;
                    }
                }
            }

            //This is unreachable.
            //return VMPrimitiveExitCode.RETURN_FALSE;
        }

        public void WalkInterrupt(int waitTime)
        {
            //TODO: stop animation.
            var avatar = (VMAvatar)Caller;
            PlayAnim(avatar.WalkAnimations[3], avatar);

            if (State == VMRoutingFrameState.WALKING)
            {
                avatar.Velocity = new Vector3(0, 0, 0);
                State = VMRoutingFrameState.WAIT_LET_BY;
            }
            WaitTime = Math.Max(waitTime, WaitTime);
        }

        private VMRoutingFrame GetParentFrame()
        {
            //look for parent frame
            for (int i = Thread.Stack.Count - 2; i >= 0; i--)
            {
                var frame = Thread.Stack[i];
                if (frame is VMRoutingFrame)
                {
                    return (VMRoutingFrame)frame;
                    
                }
            }
            return null;
        }

        private bool CanPortalTurn()
        {
            var rf = GetParentFrame();
            if (rf == null) rf = this;
            return (rf.State != VMRoutingFrameState.ROOM_PORTAL || rf.PortalTurns++ == 0);
        }

        private void BeginWalk()
        { //faces the avatar towards the initial walk direction and begins walking.
            WalkDirection = Caller.RadianDirection;
            var directionDiff = DirectionUtils.Difference(Caller.RadianDirection, TargetDirection);

            bool hardStart = CanPortalTurn();
            Velocity = (hardStart) ? 0 : 8;

            if (hardStart && Turn(directionDiff))
            {
                State = VMRoutingFrameState.BEGIN_TURN;
            }
            else
            {
                State = VMRoutingFrameState.WALKING;
                Caller.RadianDirection = (float)TargetDirection;
                StartWalkAnimation();
            }
        }

        private bool EndWalk() //returns true if we should exit immediately.
        {
            var avatar = (VMAvatar)Caller;
            WalkDirection = Caller.RadianDirection;
            TargetDirection = CurRoute.RadianDirection;

            avatar.SetPersonData(VMPersonDataVariable.RouteEntryFlags, (short)CurRoute.RouteEntryFlags);
            avatar.Velocity = new Vector3();

            var directionDiff = DirectionUtils.Difference(Caller.RadianDirection, TargetDirection);
            if (!CurRoute.FaceAnywhere && CanPortalTurn() && Turn(directionDiff))
            {
                State = VMRoutingFrameState.END_TURN;
            }
            else
            {
                if (!CurRoute.FaceAnywhere) avatar.RadianDirection = CurRoute.RadianDirection;
                return true; //we are here!
            }
            return false;
        }

        private bool Turn(double directionDiff)
        {
            var obj = (VMAvatar)Caller;
            int off = (directionDiff > 0) ? 0 : 1;
            var absDiff = Math.Abs(directionDiff);

            string animName;
            if (absDiff >= Math.PI - 0.01) //full 180 turn
            {
                animName = obj.WalkAnimations[4 + off];
                TurnTweak = 0;
            }
            else if (absDiff >= (Math.PI / 2) - 0.01) //>=90 degree turn
            {
                animName = obj.WalkAnimations[6 + off];
                TurnTweak = (float)(absDiff - (Math.PI / 2));
            }
            else if (absDiff >= (Math.PI / 4) - 0.01) //>=45 degree turn
            {
                animName = obj.WalkAnimations[8 + off];
                TurnTweak = (float)(absDiff - (Math.PI / 4));
            }
            else
            {
                //turning animation not needed for small rotations!
                return false;
            }

            var anim = PlayAnim(animName, obj);
            TurnTweak /= anim.NumFrames;
            if (off == 1) TurnTweak = -TurnTweak;
            return true;
        }

        private void StartWalkAnimation()
        {
            var obj = (VMAvatar)Caller;
            var anim = PlayAnim(obj.WalkAnimations[(WalkStyle==1)?21:20], obj); //TODO: maybe an enum for this too. Maybe just an enum for everything.
            Walking = true;
        }

        private Animation PlayAnim(string name, VMAvatar avatar)
        {
            var animation = FSO.Content.Content.Get().AvatarAnimations.Get(name + ".anim");

            avatar.CurrentAnimation = animation;
            avatar.CurrentAnimationState = new VMAnimationState();
            avatar.Avatar.LeftHandGesture = SimHandGesture.Idle;
            avatar.Avatar.RightHandGesture = SimHandGesture.Idle;

            foreach (var motion in animation.Motions)
            {
                if (motion.TimeProperties == null) { continue; }

                foreach (var tp in motion.TimeProperties)
                {
                    foreach (var item in tp.Items)
                    {
                        avatar.CurrentAnimationState.TimePropertyLists.Add(item);
                    }
                }
            }

            /** Sort time property lists by time **/
            avatar.CurrentAnimationState.TimePropertyLists.Sort(new TimePropertyListItemSorter());
            return animation;
        }

        private bool AdvanceWaypoint()
        {
            if (CurrentWaypoint != LotTilePos.OUT_OF_WORLD) Caller.Position = CurrentWaypoint;
            if (WalkTo.Count == 0) return false;

            var point = WalkTo.First.Value;
            WalkTo.RemoveFirst();
            if (WalkTo.Count > 0)
            {
                CurrentWaypoint = new LotTilePos((short)point.X, (short)point.Y, Caller.Position.Level);
            }
            else CurrentWaypoint = CurRoute.Position; //go directly to position at last

            PreviousPosition = Caller.Position;
            MoveFrames = 0;
            MoveTotalFrames = ((LotTilePos.Distance(CurrentWaypoint, Caller.Position)*20)/2)/((WalkStyle == 1) ? 2 : 1);

            WalkDirection = TargetDirection;
            TargetDirection = Math.Atan2(CurrentWaypoint.x - Caller.Position.x, Caller.Position.y - CurrentWaypoint.y); //y+ as north. x+ is -90 degrees.
            TurnFrames = 10;
            return true;
        }
    }

    public enum VMRoutingFrameState
    {
        INITIAL,
    
        BEGIN_TURN,
        WALKING,
        END_TURN,

        WAIT_LET_BY,
        WAIT_SHOO,

        ROOM_PORTAL,
        FAILED
    }
}
