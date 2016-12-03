using FSO.Common;
using FSO.Common.Rendering.Framework.Model;
using FSO.LotView.Model;
using FSO.SimAntics;
using FSO.SimAntics.NetPlay.Model;
using FSO.SimAntics.NetPlay.Model.Commands;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Panels.LotControls
{
    public class UICheatHandler
    {
        //NOTE: users can only perform these actions if the server lets them.
        private UILotControl Control;
        private VM vm
        {
            get { return Control.vm;  }
        }
        private UpdateState LastState;

        public UICheatHandler(UILotControl owner) {
            Control = owner;
        }

        public void Update(UpdateState state)
        {
            LastState = state;
        }

        public void SubmitCommand(string msg)
        {
            var state = LastState;
            if (state == null) return;
            var spaceIndex = msg.IndexOf(' ');
            if (spaceIndex == -1) spaceIndex = msg.Length;
            var cmd = msg.Substring(1, spaceIndex - 1);
            var args = msg.Substring(Math.Min(msg.Length, spaceIndex + 1), Math.Max(0, msg.Length - (spaceIndex + 1)));
            string response = "("+msg+") ";
            var tilePos = vm.Context.World.State.WorldSpace.GetTileAtPosWithScroll(new Vector2(state.MouseState.X, state.MouseState.Y) / FSOEnvironment.DPIScaleFactor);
            try {
                switch (cmd.ToLowerInvariant())
                {
                    case "roomat":
                        //!roomat
                        LotTilePos targetrPos = new LotTilePos((short)(tilePos.X*16), (short)(tilePos.Y*16), vm.Context.World.State.Level);
                        var room = vm.Context.GetRoomAt(targetrPos);
                        response += "Room at (" + targetrPos.TileX + ", " + targetrPos.TileY + ", " + targetrPos.Level + ") is "+room+"\r\n";
                        var roomInfo = vm.Context.RoomInfo[room];
                        foreach (var obj in roomInfo.Room.AdjRooms)
                        {
                            var info = vm.Context.RoomInfo[obj];
                            response += "adjacent room: " + obj;
                            response += "\r\n";
                        }
                        break;
                    case "objat":
                        //!objat (objects at mouse position)
                        LotTilePos targetPos = LotTilePos.FromBigTile((short)tilePos.X, (short)tilePos.Y, vm.Context.World.State.Level);
                        if (args == "oow") targetPos = LotTilePos.OUT_OF_WORLD;
                        var objs = vm.Context.ObjectQueries.GetObjectsAt(targetPos);
                        response += "Objects at (" + targetPos.TileX + ", " + targetPos.TileY + ", " + targetPos.Level + ")\r\n"; 
                        foreach (var obj in objs)
                        {
                            response += ObjectSummary(obj);
                            response += "\r\n";
                        }
                        break;
                    case "del":
                        //!del objectID
                        vm.SendCommand(new VMNetDeleteObjectCmd()
                        {
                            ObjectID = short.Parse(args),
                            CleanupAll = true
                        });
                        response += "Sent deletion command.";
                        break;
                    default:
                        response += "Unknown command.";
                        break;
                }
            } catch (Exception e)
            {
                response += "Bad command.";
            }
            vm.SignalChatEvent(new VMChatEvent(0, VMChatEventType.Generic, response));
        }

        public string ObjectSummary(VMEntity obj)
        {
            return obj.ToString() + " | " + obj.ObjectID + " | " + "container: " + obj.Container;
        }
    }
}
