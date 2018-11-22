using FSO.Client.UI.Framework;
using FSO.Common;
using FSO.Common.Rendering.Framework.Model;
using FSO.LotView.LMap;
using FSO.LotView.Model;
using FSO.SimAntics;
using FSO.SimAntics.NetPlay.Model;
using FSO.SimAntics.NetPlay.Model.Commands;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Panels.LotControls
{
    public class UICheatHandler : UIContainer
    {
        //NOTE: users can only perform these actions if the server lets them.
        private UILotControl Control;
        private VM vm
        {
            get { return Control.vm;  }
        }
        private UpdateState LastState;
        private Texture2D DebugTexture;

        private float tod = 0;

        public UICheatHandler(UILotControl owner) {
            Control = owner;
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
            LastState = state;
        }

        public override void PreDraw(UISpriteBatch batch)
        {
            base.PreDraw(batch);

            //DebugTexture = vm.Context.World.State.Light.LightMapDirection;
            return;

            var tilePos = vm.Context.World.EstTileAtPosWithScroll(new Vector2(LastState.MouseState.X, LastState.MouseState.Y) / FSOEnvironment.DPIScaleFactor);
            LotTilePos targetrPos = new LotTilePos((short)(tilePos.X * 16), (short)(tilePos.Y * 16), vm.Context.World.State.Level);
            var room = vm.Context.GetRoomAt(targetrPos);

            //var roomInfo = vm.Context.RoomInfo[1];
            //var roomObjs = roomInfo.Entities.Select(x => (x == x.MultitileGroup.Objects[0])?x.MultitileGroup.LightBounds():null);
            //DebugTexture = vm.Context.World.State.Light.DebugShadows(roomObjs.Where(x => x != null).Select(x => x.Value).ToList(), new Vector2(-1000, -1000)); //tilePos * 16);


            var light = new LightData()
            {
                //LightPos = tilePos * 16,
                LightPos = new Vector2(-1000, -1000),
                LightType = LightType.OUTDOORS
            };

            Matrix Transform = Microsoft.Xna.Framework.Matrix.Identity;

            Transform *= Microsoft.Xna.Framework.Matrix.CreateRotationY((float)((tod + 0.5) * Math.PI * 2.0)); //Controls the rotation of the sun/moon around the city. 
            Transform *= Microsoft.Xna.Framework.Matrix.CreateRotationZ((float)(Math.PI * (45.0 / 180.0))); //Sun is at an angle of 45 degrees to horizon at it's peak. idk why, it's winter maybe? looks nice either way
            Transform *= Microsoft.Xna.Framework.Matrix.CreateRotationY((float)(Math.PI * 0.3)); //Offset from front-back a little. This might need some adjusting for the nicest sunset/sunrise locations.

            var lightPos = new Vector3(0, 0, -3000);
            lightPos = Vector3.Transform(lightPos, Transform);
            if (lightPos.Y < 0) lightPos *= -1;
            light.LightPos = new Vector2(lightPos.X, lightPos.Z);
            light.LightDir = -light.LightPos;
            light.LightDir.Normalize();
            lightPos.Normalize();
            light.FalloffMultiplier = (float)Math.Sqrt(lightPos.X * lightPos.X + lightPos.Z * lightPos.Z) / lightPos.Y;

            var roomInfo = vm.Context.RoomInfo[1]; //vm.Context.RoomInfo[vm.Context.RoomInfo[room].Room.LightBaseRoom];
            if (roomInfo.Room.WallLines != null)
            {
                //var wallRect = roomInfo.Room.WallObs.Select(x => new Rectangle(x.x1 + 2, x.y1 + 2, (x.x2 - x.x1) - 3, (x.y2 - x.y1) - 3)).ToList();
                var wallLines = roomInfo.Room.WallLines;
                //var wallRect = roomInfo.Room.WallObs.Select(x => new Rectangle(x.x1, x.y1, (x.x2 - x.x1), (x.y2 - x.y1))).ToList();
                DebugTexture = vm.Context.World.State.Light.DebugShadows(wallLines, light);
                batch.GraphicsDevice.SetRenderTarget(null);
            }

            //return;

            //var roomInfo = vm.Context.RoomInfo[vm.Context.RoomInfo[room].Room.LightBaseRoom];
            var roomObjs = vm.Context.RoomInfo[1].Entities.Select(x => (x == x.MultitileGroup.Objects[0]) ? x.MultitileGroup.LightBounds() : null);
            //DebugTexture = vm.Context.World.State.Light.DebugShadows(roomObjs.Where(x => x != null).Select(x => x.Value).ToList(), light);
            DebugTexture = vm.Context.World.State.Light.DebugLightMap();
            vm.Context.World.State.Light.ResetDraw();


            tod += 0.002f;
            tod %= 1f;
        }

        public override void Draw(UISpriteBatch batch)
        {
            base.Draw(batch);
            //DebugTexture = Common.Utils.TextureGenerator.GetTerrainNoise(batch.GraphicsDevice);
            if (DebugTexture != null)
            {
                DrawLocalTexture(batch, DebugTexture, new Vector2(20, 20));
            }
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
            var tilePos = vm.Context.World.EstTileAtPosWithScroll(new Vector2(state.MouseState.X, state.MouseState.Y) / FSOEnvironment.DPIScaleFactor);
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
                    case "debugroutes":
                        var on = args.ToLowerInvariant() == "true" || args == "1";
                        SimAntics.Engine.VMRoutingFrame.DEBUG_DRAW = on;
                        response += "Debug Routes Set: " + on;
                        break;
                    default:
                        response += "Unknown command.";
                        break;
                }
            } catch (Exception e)
            {
                response += "Bad command.";
            }
            vm.SignalChatEvent(new VMChatEvent(null, VMChatEventType.Generic, response));
        }

        public string ObjectSummary(VMEntity obj)
        {
            return obj.ToString() + " | " + obj.ObjectID + " | " + "container: " + obj.Container 
                + "owner: " + ((obj.TSOState as SimAntics.Model.TSOPlatform.VMTSOObjectState)?.OwnerID ?? 0);

        }
    }
}
