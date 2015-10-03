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

        //each within-room route gets these allowances separately.
        private static int WAIT_TIMEOUT = 10 * 30; //10 seconds
        private static int MAX_RETRIES = 10;

        public Stack<VMRoomPortal> Rooms;
        public LinkedList<Point> WalkTo;
        private double WalkDirection;
        private double TargetDirection;
        private bool Walking = false;
        private bool IgnoreRooms;

        public VMRoutingFrameState State = VMRoutingFrameState.INITIAL;
        public int PortalTurns = 0;
        public int WaitTime = 0;
        public int Timeout = WAIT_TIMEOUT;
        public int Retries = MAX_RETRIES;

        private bool AttemptedChair = false;
        private float TurnTweak = 0;
        private int TurnFrames = 0;

        private int MoveTotalFrames = 0;
        private int MoveFrames = 0;
        private int Velocity = 0;
        private VMRoutingFrame ParentRoute;

        private short WalkStyle
        {
            get
            {
                return Caller.GetValue(VMStackObjectVariable.WalkStyle);
            }
        }

        public bool CallFailureTrees = false;

        private HashSet<VMAvatar> AvatarsToConsider = new HashSet<VMAvatar>();

        private LotTilePos PreviousPosition;
        private LotTilePos CurrentWaypoint = LotTilePos.OUT_OF_WORLD;

        public SLOTItem Slot;
        public VMEntity Target;
        public List<VMFindLocationResult> Choices;
        public VMFindLocationResult CurRoute;

        private void Init()
        {
            ParentRoute = GetParentFrame();
            if (ParentRoute != null)
            {
                AvatarsToConsider = new HashSet<VMAvatar>(ParentRoute.AvatarsToConsider);
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
                            if (colRoute.WaitTime > 0) AvatarsToConsider.Add(colAvatar);
                        }
                    }
                }
            }
        }

        public bool InitRoutes(SLOTItem slot, VMEntity target)
        {
            Init();

            Slot = slot;
            Target = target;
            var found = AttemptRoute(null);

            if (found == VMRouteFailCode.Success) return true;
            else HardFail(found, null);
            return false;
        }

        public bool InitRoutes(List<VMFindLocationResult> choices) //returns false if we can't find a single route
        {
            Init();

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
            var avatar = (VMAvatar)Caller;
            if (CallFailureTrees)
            {
                avatar.SetPersonData(VMPersonDataVariable.Priority, 100); //TODO: what is this meant to be? what dictates it? 
                //probably has to do with interaction priority.
                //we just set it to 100 here so that failure trees work.
                var bhav = Global.Resource.Get<BHAV>(ROUTE_FAIL_TREE);
                Thread.ExecuteSubRoutine(this, bhav, CodeOwner, new VMSubRoutineOperand(new short[] { (short)code, (blocker==null)?(short)0:blocker.ObjectID, 0, 0 }));
            }
            avatar.SetPersonData(VMPersonDataVariable.RouteResult, (short)code);
        }

        private bool DoRoomRoute(VMFindLocationResult route)
        {
            Rooms = new Stack<VMRoomPortal>();

            LotTilePos dest;
            if (Slot != null)
            {
                //take destination pos from object. Estimate room closeness using distance to object, not destination.
                dest = Target.Position;
            }
            else
            {
                if (route != null) dest = route.Position;
                else return false; //???
            }

            var DestRoom = VM.Context.GetRoomAt(dest);
            var MyRoom = VM.Context.GetRoomAt(Caller.Position);

            IgnoreRooms = (Slot == null && (route.Flags & SLOTFlags.IgnoreRooms) > 0) || (Slot != null && (Slot.Rsflags & SLOTFlags.IgnoreRooms) > 0);

            if (DestRoom == MyRoom || IgnoreRooms) return true; //we don't have to do any room finding for this
            else
            {
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
                fScore[StartPortal] = GetDist(Caller.Position, dest);

                while (openSet.Count != 0)
                {
                    var current = openSet[0];
                    openSet.RemoveAt(0);

                    if (current.TargetRoom == DestRoom)
                    {
                        //this portal gets us to the room.
                        while (current != StartPortal) //push previous portals till we get to our first "portal", the sim in its current room (we have already "traversed" this portal)
                        {
                            Rooms.Push(current);
                            current = parents[current];
                        }
                        return true;
                    }

                    closedSet.Add(current);

                    var portals = VM.Context.RoomInfo[current.TargetRoom].Portals;

                    foreach (var portal in portals)
                    { //evaluate all neighbor portals
                        if (closedSet.Contains(portal)) continue; //already evaluated!

                        var pos = VM.GetObjectById(portal.ObjectID).Position;
                        var gFromCurrent = gScore[current] + GetDist(VM.GetObjectById(current.ObjectID).Position, pos);
                        var newcomer = !openSet.Contains(portal);

                        if (newcomer || gFromCurrent < gScore[portal])
                        {
                            parents[portal] = current; //best parent for now
                            gScore[portal] = gFromCurrent;
                            fScore[portal] = gFromCurrent + GetDist(pos, dest);
                            if (newcomer)
                            { //add and move to relevant position
                                OpenSetSortedInsert(openSet, fScore, portal);
                            }
                            else
                            { //remove and reinsert to refresh sort
                                openSet.Remove(portal);
                                OpenSetSortedInsert(openSet, fScore, portal);
                            }
                        }
                    }
                }

                return false;
            }
        }

        private VMRouteFailCode AttemptRoute(VMFindLocationResult route) { //returns false if there is no room portal route to the destination room.
            //if route is not null, we are on a DIRECT route, where either the SLOT has been resolved or a route has already been passed to us.
            //resets some variables either way, so that the route can start again.

            CurRoute = route;

            WalkTo = null; //reset routing state
            Walking = false;
            AttemptedChair = false;
            TurnTweak = 0;

            var avatar = (VMAvatar)Caller;

            return (DoRoomRoute(route)) ? VMRouteFailCode.Success : VMRouteFailCode.NoRoomRoute;
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
                if (obj != Caller && ft != null && (obj is VMGameObject || AvatarsToConsider.Contains(obj)) && ((flags & VMEntityFlags.DisallowPersonIntersection) > 0 || (flags & VMEntityFlags.AllowPersonIntersection) == 0))
                    obstacles.Add(new VMObstacle(ft.x1-3, ft.y1-3, ft.x2+3, ft.y2+3));
            }

            obstacles.AddRange(roomInfo.Room.WallObs);
            if (!IgnoreRooms) obstacles.AddRange(roomInfo.Room.RoomObs);

            var startPoint = new Point((int)startPos.x, (int)startPos.y);
            var endPoint = new Point((int)CurRoute.Position.x, (int)CurRoute.Position.y);

            foreach (var rect in obstacles)
            {
                if (rect.HardContains(startPoint)) return false;
            }

            var router = new VMRectRouter(obstacles);

            if (startPoint == endPoint)
            {
                State = VMRoutingFrameState.TURN_ONLY;
                return true;
            }

            WalkTo = router.Route(startPoint, endPoint);
            if (WalkTo != null)
            {
                if (WalkTo.First.Value != endPoint) WalkTo.RemoveFirst();
                AdvanceWaypoint();
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
            var avatar = (VMAvatar)Caller;
            avatar.Velocity = new Vector3(0, 0, 0);

            if (State != VMRoutingFrameState.FAILED && avatar.GetFlag(VMEntityFlags.InteractionCanceled) && avatar.GetPersonData(VMPersonDataVariable.NonInterruptable) == 0)
            {
                HardFail(VMRouteFailCode.Interrupted, null);
                return VMPrimitiveExitCode.CONTINUE;
            }

            if (WaitTime > 0)
            {
                if (Velocity > 0) Velocity--;

                avatar.Animations[0].Weight = (8 - Velocity) / 8f;
                avatar.Animations[1].Weight = (Velocity / 8f) * 0.66f;
                avatar.Animations[2].Weight = (Velocity / 8f) * 0.33f;

                WaitTime--;
                Timeout--;
                if (Timeout <= 0)
                {
                    //try again. not sure if we should reset timeout for the new route
                    SoftFail(VMRouteFailCode.NoPath, null);
                    if (State != VMRoutingFrameState.FAILED) {
                        Velocity = 0;
                        State = VMRoutingFrameState.WALKING;
                    }
                } else return VMPrimitiveExitCode.CONTINUE_NEXT_TICK;
            }

            switch (State)
            {
                case VMRoutingFrameState.STAND_FUNC:
                    if (Thread.LastStackExitCode == VMPrimitiveExitCode.RETURN_TRUE)
                    {
                        State = VMRoutingFrameState.INITIAL;
                        if (avatar.GetPersonData(VMPersonDataVariable.Posture) == 1) avatar.SetPersonData(VMPersonDataVariable.Posture, 0);
                    }
                    else
                        SoftFail(VMRouteFailCode.CantStand, null);
                    return VMPrimitiveExitCode.CONTINUE;
                case VMRoutingFrameState.INITIAL:
                case VMRoutingFrameState.ROOM_PORTAL:
                    //check if the room portal that just finished succeeded.
                    if (State == VMRoutingFrameState.ROOM_PORTAL) { 
                        if (Thread.LastStackExitCode != VMPrimitiveExitCode.RETURN_TRUE)
                        {
                            HardFail(VMRouteFailCode.NoRoomRoute, null); //todo: reattempt room route with portal we tried removed.
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

                    //if we're here, room route is OK. start routing to a destination.
                    if (Choices == null)
                    {
                        //perform slot parse.
                        if (Slot == null)
                        {
                            HardFail(VMRouteFailCode.Unknown, null);
                            return VMPrimitiveExitCode.CONTINUE; //this should never happen. If it does, someone has used the routing system incorrectly.
                        }

                        var parser = new VMSlotParser(Slot);

                        Choices = parser.FindAvaliableLocations(Target, VM.Context, avatar);
                        if (Choices.Count == 0)
                        {
                            HardFail(parser.FailCode, parser.Blocker);
                            return VMPrimitiveExitCode.CONTINUE;
                        }
                        else
                        {
                            CurRoute = Choices[0];
                            Choices.RemoveAt(0);
                        }
                    }

                    //do we need to sit in a seat? it should take over.
                    if (CurRoute.Chair != null)
                    {
                        if (!AttemptedChair)
                        {
                            AttemptedChair = true;
                            if (PushEntryPoint(26, CurRoute.Chair)) return VMPrimitiveExitCode.CONTINUE;
                            else
                            {
                                SoftFail(VMRouteFailCode.CantSit, null);
                                return VMPrimitiveExitCode.CONTINUE;
                            }
                        }
                        else
                        {
                            if (Thread.LastStackExitCode == VMPrimitiveExitCode.RETURN_TRUE) return VMPrimitiveExitCode.RETURN_TRUE;
                            else
                            {
                                SoftFail(VMRouteFailCode.CantSit, null);
                                return VMPrimitiveExitCode.CONTINUE;
                            }
                        }
                    }

                    //If we are sitting, and the target is not this seat we need to call the stand function on the object we are contained within.
                    if (avatar.GetPersonData(VMPersonDataVariable.Posture) == 1)
                    {
                        State = VMRoutingFrameState.STAND_FUNC;
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

                    //no chair, we just need to walk to the spot. Start the within-room routing.
                    if (WalkTo == null)
                    {
                        if (!AttemptWalk())
                        {
                            SoftFail(VMRouteFailCode.NoPath, null);
                            return VMPrimitiveExitCode.CONTINUE;
                        }
                    }
                    if (State != VMRoutingFrameState.TURN_ONLY) BeginWalk();

                    return VMPrimitiveExitCode.CONTINUE;
                case VMRoutingFrameState.FAILED:
                    return VMPrimitiveExitCode.RETURN_FALSE;
                case VMRoutingFrameState.TURN_ONLY:
                    return (EndWalk()) ? VMPrimitiveExitCode.RETURN_TRUE : VMPrimitiveExitCode.CONTINUE;
                case VMRoutingFrameState.SHOOED:
                    StartWalkAnimation();
                    State = VMRoutingFrameState.WALKING;
                    return VMPrimitiveExitCode.CONTINUE;
                case VMRoutingFrameState.BEGIN_TURN:
                case VMRoutingFrameState.END_TURN:
                    if (avatar.CurrentAnimationState.EndReached)
                    {
                        if (State == VMRoutingFrameState.BEGIN_TURN)
                        {
                            State = VMRoutingFrameState.WALKING;
                            WalkDirection = TargetDirection;
                            avatar.RadianDirection = (float)TargetDirection;
                            StartWalkAnimation();
                            return VMPrimitiveExitCode.CONTINUE;
                        }
                        else
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
                case VMRoutingFrameState.WALKING:
                    if (WalkTo == null)
                    {
                        if (!AttemptWalk())
                        {
                            SoftFail(VMRouteFailCode.NoPath, null);
                        }
                        return VMPrimitiveExitCode.CONTINUE;
                    }

                    if (WalkTo.Count == 0 && MoveTotalFrames - MoveFrames <= 28 && CanPortalTurn()) //7+6+5+4...
                    {
                        //tail off
                        if (Velocity > 1) Velocity--;
                    }
                    else
                    {
                        //get started
                        if (Velocity < 8) Velocity++;
                    }

                    avatar.Animations[0].Weight = (8 - Velocity) / 8f;
                    avatar.Animations[1].Weight = (Velocity / 8f) * 0.66f;
                    avatar.Animations[2].Weight = (Velocity / 8f) * 0.33f;

                    MoveFrames += Velocity;
                    if (MoveFrames >= MoveTotalFrames)
                    {
                        MoveFrames = MoveTotalFrames;
                        //move us to the final spot, then attempt an advance.
                    }
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
                    if (result.Status != VMPlacementError.Success && result.Status != VMPlacementError.CantBeThroughWall)
                    {
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

                            //we already attempted to move around this avatar... if this happens too much give up.
                            if (AvatarsToConsider.Contains(colAvatar) && --Retries <= 0)
                            {
                                SoftFail(VMRouteFailCode.NoPath, avatar);
                                return VMPrimitiveExitCode.CONTINUE;
                            }

                            if (colTopFrame != null && colTopFrame is VMRoutingFrame)
                            {
                                colRoute = (VMRoutingFrame)colTopFrame;
                                routeAround = (colRoute.WaitTime > 0);
                            }
                            if (routeAround) AvatarsToConsider.Add(colAvatar);
                        }

                        if (result.Object != null && result.Object is VMGameObject)
                        {
                            //this should not happen often. An object has blocked our path due to some change in its position.
                            //repeated occurances indicate that we are stuck in something.
                            //todo: is this safe for the robot lot?
                            if (--Retries <= 0)
                            {
                                SoftFail(VMRouteFailCode.NoPath, avatar);
                                return VMPrimitiveExitCode.CONTINUE;
                            }
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

                                    //DO NOT push tree if:
                                    // - sim is already being shooed. (the parent of the top routing frame is in state "SHOOED" or shoo interaction is present)
                                    // - we are being shooed.
                                    // - sim is waiting on someone they just shooed. (presumably can move out of our way once they finish waiting)
                                    //instead just wait a small duration and try again later.

                                    //cases where we cannot continue moving increase the retry count. if this is greater than RETRY_COUNT then we fail.
                                    //not sure how the original game works.


                                    if (CanShooAvatar((VMAvatar)result.Object))
                                    {
                                        AvatarsToConsider.Remove((VMAvatar)result.Object);
                                        VMEntity callee = VM.Context.CreateObjectInstance(GOTO_GUID, new LotTilePos(result.Object.Position), Direction.NORTH).Objects[0];
                                        if (colRoute != null)
                                        {
                                            colRoute.State = VMRoutingFrameState.SHOOED;
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
                                    }
                                    else
                                    {
                                        WalkInterrupt(60); //wait for a little while, they're moving out of the way.
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
                            return VMPrimitiveExitCode.CONTINUE;
                        }

                    }
                    Caller.VisualPosition = Vector3.Lerp(PreviousPosition.ToVector3(), CurrentWaypoint.ToVector3(), MoveFrames / (float)MoveTotalFrames);

                    var velocity = Vector3.Lerp(PreviousPosition.ToVector3(), CurrentWaypoint.ToVector3(), Velocity / (float)MoveTotalFrames) - PreviousPosition.ToVector3();
                    velocity.Z = 0;
                    avatar.Velocity = velocity;

                    if (MoveTotalFrames == MoveFrames)
                    {
                        MoveTotalFrames = 0;
                        while (MoveTotalFrames == 0)
                        {
                            var remains = AdvanceWaypoint();
                            if (!remains)
                            {
                                MoveTotalFrames = -1;
                                if (EndWalk()) return VMPrimitiveExitCode.RETURN_TRUE;
                            }
                        }
                    }
                return VMPrimitiveExitCode.CONTINUE_NEXT_TICK;
            }
            return VMPrimitiveExitCode.GOTO_FALSE; //???
        }

        private bool CanShooAvatar(VMAvatar avatar)
        {
            VMRoutingFrame topRoute = null;
            //look for top frame
            for (int i = avatar.Thread.Stack.Count - 1; i >= 0; i--)
            {
                var frame = avatar.Thread.Stack[i];
                if (frame is VMRoutingFrame)
                {
                    topRoute = (VMRoutingFrame)frame;
                }
            }

            //check both the top route frame its parent for ones postponed by shooing, 
            //as the top frame will most likely be walking as part of the shoo, and the postponed frame will be the parent.
            //check the parent too just in case they got shooed this frame by another avatar
            if (topRoute != null && (topRoute.State == VMRoutingFrameState.SHOOED || (topRoute.ParentRoute != null && topRoute.ParentRoute.State == VMRoutingFrameState.SHOOED)))
                return false;

            //check if the shoo interaction is present on their action queue. callee GUID (destination obj) and 
            //interaction id must match.
            if (avatar.Thread.Queue.Any(x => x.Callee.Object.GUID == GOTO_GUID && x.InteractionNumber == SHOO_INTERACTION)) return false;

            return true;
        }

        public void WalkInterrupt(int waitTime)
        {
            
            var avatar = (VMAvatar)Caller;

            if (State == VMRoutingFrameState.WALKING)
            {
                //only wait if we're walking
                avatar.Velocity = new Vector3(0, 0, 0);
                WaitTime = Math.Max(waitTime, WaitTime);
            }
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
            var rf = ParentRoute;
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

            obj.Animations.Clear();
            var anim = PlayAnim(animName, obj);
            if (WalkStyle == 1) anim.Speed = 2f;
            TurnTweak /= anim.Anim.NumFrames/anim.Speed;
            if (off == 1) TurnTweak = -TurnTweak;
            return true;
        }

        private void StartWalkAnimation()
        {
            var obj = (VMAvatar)Caller;
            Walking = true;
            if (obj.Animations.Count == 3 && 
                obj.Animations[0].Anim.Name == obj.WalkAnimations[3] &&
                obj.Animations[1].Anim.Name == obj.WalkAnimations[(WalkStyle == 1) ? 21 : 20]) return; //hacky check to test if we're already doing a walking animation. 

            //we set up a very specific collection of animations.
            //The original game gets its walk animation by confusingly combining two of the walk animations and running them at 1.5x speed.
            //We also want to store the standing pose in the first animation slot so that we can blend into and out of it with velocity.

            obj.Animations.Clear();
            var anim = PlayAnim(obj.WalkAnimations[3], obj); //stand animation (TODO: what about swimming?)
            anim.Weight = 0f;
            anim.Loop = true;
            anim = PlayAnim(obj.WalkAnimations[(WalkStyle==1)?21:20], obj); //Run full:Walk Full
            anim.Weight = 0.66f;
            anim.Speed = 1.5f;
            anim.Loop = true;
            anim = PlayAnim(obj.WalkAnimations[(WalkStyle == 1) ? 21 : 25], obj); //Run full:Walk Half
            anim.Weight = 0.33f;
            anim.Speed = 1.5f;
            anim.Loop = true;
        }

        private VMAnimationState PlayAnim(string name, VMAvatar avatar)
        {
            var animation = FSO.Content.Content.Get().AvatarAnimations.Get(name + ".anim");
            var state = new VMAnimationState(animation, false);
            avatar.Animations.Add(state);
            return state;
        }

        private bool AdvanceWaypoint()
        {
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

            MoveTotalFrames = ((LotTilePos.Distance(CurrentWaypoint, Caller.Position) * 20) / 2);
            if (((VMAvatar)Caller).IsPet) MoveTotalFrames *= 2;
            MoveTotalFrames = Math.Max(1, MoveTotalFrames/((WalkStyle == 1) ? 3 : 1));

            WalkDirection = Caller.RadianDirection;
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
        TURN_ONLY,
        END_TURN,

        SHOOED, //recalculate route once the stack gets back here.
        ROOM_PORTAL,
        STAND_FUNC,
        FAILED
    }
}
