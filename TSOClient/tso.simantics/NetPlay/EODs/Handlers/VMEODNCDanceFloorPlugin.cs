using FSO.SimAntics.NetPlay.EODs.Handlers.Data;
using FSO.SimAntics.NetPlay.Model.Commands;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FSO.SimAntics.NetPlay.EODs.Handlers
{
    public class VMEODNCDanceFloorPlugin : VMEODHandler
    {
        public VMEODClient ControllerClient;
        public VMEODNightclubControllerPlugin ControllerPlugin;
        public int ScreenWidth;
        public int ScreenHeight;
        public Rectangle ScreenRect;
        public Point ScreenCtr;
        public int ScreenDiag;
        public short[] ScreenTiles;
        public byte[] ScreenData;
        public int[] Ratings = new int[] { 0, 0, 0, 0 };

        public VMEODNCAnimTypes Animation;
        public short Direction;
        public byte Color;

        public VMEODNCRandomAnims RandomAnimation;
        public string TextMessage;
        public int TempTimer; //timer for temporary patterns

        public List<VMEODNCDanceParticle> Particles = new List<VMEODNCDanceParticle>();

        public string[] TextMessages = new string[]
        {
            //normal messages
            "FreeSO",
            "PARTY",
            "DANCE",
            "~(o.o)~",
            //good messages
            ";)",
            "WOW",
            "\\(o.o)/",
            "<3",
            //bad messages
            "BOO",
            "LAME",
            ":(",
            "YOU SUCK"
        };

        public string[] DiamondImage =
        {
            "....#....",
            "...###...",
            "..#####..",
            ".#######.",
            "#########",
            ".#######.",
            "..#####..",
            "...###...",
            "....#...."
        };

        public string[] HeartImage =
        {
            ".........",
            ".###.###.",
            "#########",
            "#########",
            "#########",
            ".#######.",
            "..#####..",
            "...###...",
            "....#...."
        };
        
        public string[] StarImage =
        {
            ".........",
            "....#....",
            "...###...",
            "#########",
            ".#######.",
            ".#######.",
            "###...###",
            "#.......#",
            ".........",
        };

        public int[] PatternMemory = new int[] { 0, 0, 0, 0, 0, 0 };

        public VMEODNCDanceFloorPlugin(VMEODServer server) : base(server)
        {
            //PlaintextHandlers["close"] = P_Close;
            //PlaintextHandlers["press_button"] = P_DanceButton;
            SimanticsHandlers[(short)VMEODNCDanceFloorEvents.DiscoverTiles] = S_DiscoverTiles;
            SimanticsHandlers[(short)VMEODNCDanceFloorEvents.SetAnimation] = S_SetAnimation;
            SimanticsHandlers[(short)VMEODNCDanceFloorEvents.SetRatings] = S_SetRatings;
        }
       

        public void S_DiscoverTiles(short evt, VMEODClient client)
        {
            var tiles = Server.vm.Context.ObjectQueries.GetObjectsByGUID(0xD481CEE5);

            if (tiles == null) return;

            int minX = 999, minY = 999, maxX = 0, maxY = 0;
            foreach (var tile in tiles)
            {
                if (tile.Position.TileX < minX) minX = tile.Position.TileX;
                if (tile.Position.TileY < minY) minY = tile.Position.TileY;
                if (tile.Position.TileX > maxX) maxX = tile.Position.TileX;
                if (tile.Position.TileY > maxY) maxY = tile.Position.TileY;
            }

            ScreenWidth = (maxX - minX) + 1;
            ScreenHeight = (maxY - minY) + 1;
            ScreenDiag = (int)Math.Ceiling(Math.Sqrt(ScreenWidth * ScreenWidth + ScreenHeight * ScreenHeight));

            ScreenTiles = new short[ScreenWidth * ScreenHeight];
            ScreenData = new byte[ScreenWidth * ScreenHeight];
            foreach (var tile in tiles)
            {
                var x = tile.Position.TileX - minX;
                var y = tile.Position.TileY - minY;
                var ind = x + y * ScreenWidth;
                ScreenTiles[ind] = tile.ObjectID;
            }

            ScreenRect = new Rectangle(minX, minY, maxX - minY, maxY - minX);
            ScreenCtr = new Point((minX + maxX) / 2, (minY + maxY) / 2);
        }

        public void S_SetAnimation(short evt, VMEODClient client)
        {
            var temps = client.Invoker.Thread.TempRegisters;
            var anim = (VMEODNCAnimTypes)temps[0];
            //arrow and line types have been replaced by our particle system.
            Color = (byte)temps[1];
            if (anim == VMEODNCAnimTypes.Line || anim == VMEODNCAnimTypes.Arrow) return;
            Animation = (VMEODNCAnimTypes)temps[0];
            if (Animation > VMEODNCAnimTypes.Random) TempTimer = 0;
            Direction = temps[2];
        }

        public void S_SetRatings(short evt, VMEODClient client)
        {
            var temps = client.Invoker.Thread.TempRegisters;

            for (int i = 0; i < 4; i++)
                Ratings[i] = temps[i];
        }

        public void AddParticle(VMEODNCParticleType type, float direction, int frame, int groupID)
        {
            var particle = new VMEODNCDanceParticle()
            {
                Type = type,
                Direction = direction,
                Frame = frame,
                GroupID = groupID,
                Color = (byte)(groupID * 2 + 1)
            };
            Particles.Add(particle);
        }

        public void ClearScreen()
        {
            ClearScreen(0);
        }

        public void ClearScreen(byte color)
        {
            for (int i=0; i< ScreenData.Length; i++)
            {
                ScreenData[i] = color;
            }
        }

        public void DrawChar(char c, int x, int y, byte color)
        {
            for (int oy = 0; oy < 6; oy++)
            {
                var ny = oy + y;
                if (ny < 0 || ny >= ScreenHeight) continue;
                var line = VMEOD3x5Font.GetFontLine(c, oy);
                for (int ox = 0; ox < 3; ox++)
                {
                    var nx = ox + x;
                    if (nx < 0 || nx >= ScreenWidth) continue;

                    if (((line >> (3-ox)) & 1) > 0) ScreenData[nx + ny * ScreenWidth] = color;
                }
            }
        }

        public void DrawString(string s, int x, int y, byte color)
        {
            foreach (char c in s)
            {
                if (x > -3 && x < ScreenWidth)
                {
                    DrawChar(c, x, y, color);
                }
                x += 4;
            }
        }

        public void DrawRect(Rectangle rect, byte color)
        {
            rect = Rectangle.Intersect(rect, new Rectangle(0, 0, ScreenWidth, ScreenHeight));
            for (int oy=0; oy<rect.Height; oy++)
            {
                var ny = oy + rect.Y;
                for (int ox = 0; ox < rect.Width; ox++)
                {
                    var nx = ox + rect.X;
                    ScreenData[nx + ny * ScreenWidth] = color;
                }
            }
        }

        public void DrawImage(int x, int y, string[] image, byte color)
        {
            var w = image[0].Length;

            var xorig = x;
            for (int yo=0; yo<w; yo++)
            {
                x = xorig;
                for (int xo = 0; xo < w; xo++)
                {
                    if (x >= 0 && x < ScreenWidth && y >= 0 && y < ScreenHeight && image[yo][xo] == '#') 
                        ScreenData[x + y * ScreenWidth] = color;
                    x++;
                }
                y++;
            }
        }
            
        private void LineLow(int x1, int y1, int x2, int y2, byte color)
        {
            var dx = x2 - x1;
            var dy = y2 - y1;
            var yi = (dy < 0) ? -1 : 1;
            dy *= yi;

            var D = 2 * dy - dx;
            var y = y1;
            for (int x = x1; x <= x2; x++)
            {
                if (x >= 0 && x < ScreenWidth && y >= 0 && y < ScreenHeight)
                    ScreenData[x + y * ScreenWidth] = color;
                if (D > 0)
                {
                    y += yi;
                    D = D - 2 * dx;
                }
                D = D + 2 * dy;
            }
        }

        private void LineHigh(int x1, int y1, int x2, int y2, byte color)
        {
            var dx = x2 - x1;
            var dy = y2 - y1;
            var xi = (dx < 0) ? -1 : 1;
            dx *= xi;

            var D = 2 * dx - dy;
            var x = x1;
            for (int y = y1; y <= y2; y++)
            {
                if (x >= 0 && x < ScreenWidth && y >= 0 && y < ScreenHeight)
                    ScreenData[x + y * ScreenWidth] = color;
                if (D > 0)
                {
                    x += xi;
                    D = D - 2 * dy;
                }
                D = D + 2 * dx;
            }
        }

        public void DrawLine(int x1, int y1, int x2, int y2, byte color)
        {
            if (Math.Abs(y2 - y1) < Math.Abs(x2 - x1)) {
                if (x1 > x2)
                    LineLow(x2, y2, x1, y1, color);
                else
                    LineLow(x1, y1, x2, y2, color);
            } else {
                if (y1 > y2)
                    LineHigh(x2, y2, x1, y1, color);
                else
                    LineHigh(x1, y1, x2, y2, color);
            }

        }

        public void DrawOutline(Vector2 v1, Vector2 v2, byte color)
        {
            var x1 = (int)Math.Round(v1.X);
            var y1 = (int)Math.Round(v1.Y);
            var x2 = (int)Math.Round(v2.X);
            var y2 = (int)Math.Round(v2.Y);
            DrawLine(x1 + 1, y1, x2 + 1, y2, color);
            DrawLine(x1, y1 + 1, x2, y2 + 1, color);
            DrawLine(x1 - 1, y1, x2 - 1, y2, color);
            DrawLine(x1, y1 - 1, x2, y2 - 1, color);
        }

        public void DrawLine(Vector2 v1, Vector2 v2, byte color)
        {
            DrawLine((int)Math.Round(v1.X), (int)Math.Round(v1.Y), (int)Math.Round(v2.X), (int)Math.Round(v2.Y), color);
        }

        private int TileWrap;
        private int Tock;
        public override void Tick()
        {
            base.Tick();

            if (ControllerPlugin == null) ControllerPlugin = Server.vm.EODHost.GetFirstHandler<VMEODNightclubControllerPlugin>();

            if (Tock % 3 == 0)
            {
                //S_DiscoverTiles(0, ControllerClient);
                if (ScreenTiles != null)
                {
                    DrawAnimation(TileWrap++);
                } else
                {
                    S_DiscoverTiles(1, ControllerClient);
                }
            }
            Tock++;
        }

        public float GetDirection(Point toPosition)
        {
            return (float)Math.Atan2(ScreenCtr.X - toPosition.X, ScreenCtr.Y - toPosition.Y);
        }

        public void DrawAnimation(int frame)
        {
            // mat = Matrix.CreateRotationZ(frame / 20f);
            var repeat = frame % ScreenDiag;
            var lit = (frame / 10) % 2;
            var col = (byte)(Color + lit);

            switch (Animation) {
                case VMEODNCAnimTypes.Off:
                    ClearScreen(0);
                    break;
                case VMEODNCAnimTypes.Fill:
                    ClearScreen(col);
                    break;

                case VMEODNCAnimTypes.DancersBonus:
                    ClearScreen(10);
                    DrawImage(ScreenWidth / 2 - 4, ScreenHeight / 2 - 4, HeartImage, col);
                    if (TempTimer++ > 75) Animation = VMEODNCAnimTypes.Random;
                    break;
                case VMEODNCAnimTypes.DJsBonus:
                    ClearScreen(10);
                    DrawImage(ScreenWidth / 2 - 4, ScreenHeight / 2 - 4, DiamondImage, col);
                    if (TempTimer++ > 75) Animation = VMEODNCAnimTypes.Random;
                    break;
                case VMEODNCAnimTypes.AllBonus:
                    ClearScreen(10);
                    DrawImage(ScreenWidth / 2 - 4, ScreenHeight / 2 - 4, StarImage, col);
                    if (TempTimer++ > 75) Animation = VMEODNCAnimTypes.Random;
                    break;
                default:
                    //random
                    var rand = new Random();
                    if (frame % (10*6) == 0)
                    {

                        RandomAnimation = (VMEODNCRandomAnims)rand.Next(5);
                        switch (RandomAnimation)
                        {
                            case VMEODNCRandomAnims.Text:
                                var bank = 0;
                                var avgRating = Ratings.Average();
                                if (avgRating < 25) bank = 2;
                                else if (avgRating > 75) bank = 1;
                                TextMessage = TextMessages[rand.Next(4) + bank];
                                break;
                            case VMEODNCRandomAnims.RainbowTunnel:
                                for (int i = 0; i < 6; i++) PatternMemory[i] = 0;
                                break;
                            default:
                                for (int i = 0; i < 6; i++) PatternMemory[i] = rand.Next(int.MaxValue);
                                break;
                        }
                    }

                    var sumRating = Ratings.Sum() + 50;
                    switch (RandomAnimation)
                    {
                        case VMEODNCRandomAnims.Circles:

                            var first = (repeat <= ScreenDiag / 2) ? 3 : 0;
                            var second = 3 - first;

                            var fr = (repeat <= ScreenDiag / 2) ? ((frame + ScreenDiag/2) % ScreenDiag) : repeat;
                            var sr = (repeat > ScreenDiag / 2) ? ((frame + ScreenDiag / 2) % ScreenDiag) : repeat;
                            if (repeat == 0)
                            {
                                PatternMemory[0] = RandomCol(rand, sumRating); //color
                                PatternMemory[1] = rand.Next(ScreenWidth); //x
                                PatternMemory[2] = rand.Next(ScreenHeight); //y
                            }
                            if (repeat == ScreenDiag/2) {
                                PatternMemory[3] = RandomCol(rand, sumRating); //color
                                PatternMemory[4] = rand.Next(ScreenWidth); //x
                                PatternMemory[5] = rand.Next(ScreenHeight); //y
                            }
                            for (int y = 0; y < ScreenHeight; y++)
                            {
                                for (int x = 0; x < ScreenWidth; x++)
                                {
                                    var dx = x - PatternMemory[1+ first];
                                    var dy = y - PatternMemory[2 + first];
                                    if (Math.Sqrt(dx * dx + dy * dy) <= fr) ScreenData[x + y * ScreenWidth] = (byte)(lit + PatternMemory[0 + first]);
                                    dx = x - PatternMemory[1 + second];
                                    dy = y - PatternMemory[2 + second];
                                    if (Math.Sqrt(dx * dx + dy * dy) <= sr) ScreenData[x + y * ScreenWidth] = (byte)(lit + PatternMemory[0 + second]);
                                }
                            }
                            break;
                        case VMEODNCRandomAnims.RandomDissolve:
                            for (int y = 0; y < ScreenHeight; y++)
                            {
                                for (int x = 0; x < ScreenWidth; x++)
                                {
                                    if (rand.Next(4) == 0)
                                    {
                                        var dist = rand.Next(sumRating);
                                        byte color = 0;

                                        for (int i = 0; i < 4; i++)
                                        {
                                            if (dist < Ratings[i])
                                            {
                                                color = (byte)(i * 2 + 1);
                                                break;
                                            }
                                            dist -= Ratings[i];
                                            if (i == 3) color = 9;
                                        }
                                        ScreenData[x + y * ScreenWidth] = (byte)(color + 1); //color
                                    }
                                }
                            }
                            break;
                        case VMEODNCRandomAnims.RainbowTunnel:
                            /*
                            if (repeat == 0)
                            {
                                PatternMemory[0] = rand.Next(5) * 2 + 1; //color
                                PatternMemory[1] = rand.Next(ScreenWidth); //x
                                PatternMemory[2] = rand.Next(ScreenHeight); //y
                            }*/

                            for (int y = 0; y < ScreenHeight; y++)
                            {
                                for (int x = 0; x < ScreenWidth; x++)
                                {
                                    var dx = x - ScreenWidth/2;
                                    var dy = y - ScreenHeight/2;
                                    var dist = Math.Sqrt(dx * dx + dy * dy);

                                    dist -= frame / 3f;
                                    var color = -((int)((dist - ScreenDiag) / 2) % 5);
                                    
                                    ScreenData[x + y * ScreenWidth] = (byte)(color * 2 + 1 + lit);
                                }
                            }
                            break;

                        case VMEODNCRandomAnims.Matrix:
                            var fallDist = ScreenHeight + 4;
                            for (int x = 0; x < ScreenWidth; x++)
                            {
                                var fallFreq = ScreenHeight + ((PatternMemory[x % 6] + x) % 9) - 4;
                                var fallPosition = (((frame % fallFreq) * fallDist)/fallFreq) % fallDist;
                                var color = (byte)((x % 5) * 2 + 1);
                                for (int y = 0; y < ScreenHeight; y++)
                                {
                                    if (y <= fallPosition - 3) ScreenData[x + y * ScreenWidth] = 0;
                                    else if (y < fallPosition && y > fallPosition - 3) ScreenData[x + y * ScreenWidth] = (byte)(color + 1);
                                    else if (y == fallPosition) ScreenData[x + y * ScreenWidth] = color;
                                }
                            }
                            break;

                        case VMEODNCRandomAnims.Text:
                            ClearScreen((byte)((10 + lit) % 11));
                            var s = TextMessage ?? "FreeSO";
                            var scrollX = TileWrap % (ScreenWidth + (s.Length + 1) * 4);
                            DrawString(s, ScreenWidth - scrollX, (ScreenHeight - 1) / 2 - 2, col);
                            var border = ScreenHeight / 2 - 3;
                            DrawRect(new Rectangle(0, 0, ScreenWidth, border), col);
                            DrawRect(new Rectangle(0, ScreenHeight-border, ScreenWidth, border), col);
                            break;
                    }
                    //var s = "FreeSO Nightclub";
                    //var scrollX = TileWrap % (ScreenWidth + (s.Length + 1) * 4);
                    //DrawString(s, ScreenWidth - scrollX, (ScreenHeight - 1) / 2 - 2, 1);
                    break;
            }

            //var s = "FreeSO Nightclub";
            //var scrollX = TileWrap % (ScreenWidth + (s.Length + 1) * 4);
            //DrawString(s, ScreenWidth - scrollX, (ScreenHeight - 1) / 2 - 2, 1);

            for (int i=0; i<Particles.Count; i++)
            {
                var p = Particles[i];
                if (!p.Tick(this))
                    Particles.RemoveAt(i--);
                else
                    p.Frame++;
            }

            Server.vm.SendCommand(new VMNetBatchGraphicCmd()
            {
                Objects = ScreenTiles,
                Graphics = ScreenData
            });
        }

        public byte RandomCol(Random rand, int sumRating)
        {
            var dist = rand.Next(sumRating);
            byte color = 0;

            for (int i = 0; i < 4; i++)
            {
                if (dist < Ratings[i])
                {
                    color = (byte)(i * 2 + 1);
                    break;
                }
                dist -= Ratings[i];
                if (i == 3) color = 9;
            }
            return (byte)(color + 1); //color
        }

        public override void OnConnection(VMEODClient client)
        {
            if (client.Avatar != null)
            {
                client.Send("dance_show", "");
            }
            else
            {
                //we're the dance floor controller!
                ControllerClient = client;
            }
        }
    }

    public class VMEODNCDanceParticle
    {
        public int GroupID;
        public VMEODNCParticleType Type;
        public byte Color;

        public float Direction;
        public int Frame;

        public bool Tick(VMEODNCDanceFloorPlugin p)
        {
            var mat = Matrix.CreateRotationZ(-Direction);
            var ctr = new Vector2(p.ScreenWidth, p.ScreenHeight) / 2 + new Vector2(-0.25f);
            var sd = p.ScreenDiag;
            var sw = p.ScreenWidth;
            var sh = p.ScreenHeight;
            switch (Type)
            {
                case VMEODNCParticleType.Line:
                    //by default we're an x line, starting from the bottom and going up.
                    var base1 = new Vector2(-sd, sd / 2 - Frame);
                    var base2 = new Vector2(sd, sd / 2 - Frame);
                    base1 = Vector2.Transform(base1, mat) + ctr;
                    base2 = Vector2.Transform(base2, mat) + ctr;
                    p.DrawOutline(base1, base2, 0);
                    p.DrawLine(base1, base2, Color);
                    return (Frame < sd);
                case VMEODNCParticleType.Arrow:
                    //draws an arrow towards the correct direction
                    var arrowr = Frame;
                    var arrow = new Vector2[]
                    {
                        new Vector2(0, 16 - arrowr),
                        new Vector2(0, 8 - arrowr),
                        new Vector2(5, 13 - arrowr),
                        new Vector2(-5, 13 - arrowr),

                        new Vector2(0, 9.25f - arrowr),
                        new Vector2(5, 14.25f - arrowr),
                        new Vector2(-5, 14.25f - arrowr)
                    }.Select(x => Vector2.Transform(x, mat) + ctr).ToArray();

                    p.DrawOutline(arrow[0], arrow[1], 0);
                    p.DrawOutline(arrow[1], arrow[2], 0);
                    p.DrawOutline(arrow[1], arrow[3], 0);
                    p.DrawOutline(arrow[4], arrow[5], 0);
                    p.DrawOutline(arrow[4], arrow[6], 0);

                    p.DrawLine(arrow[0], arrow[1], Color);
                    p.DrawLine(arrow[1], arrow[2], Color);
                    p.DrawLine(arrow[1], arrow[3], Color);
                    p.DrawLine(arrow[4], arrow[5], Color);
                    p.DrawLine(arrow[4], arrow[6], Color);

                    return (Frame < (sd + 8));
                case VMEODNCParticleType.Rect:
                    //shrinks a rectangle in from the screen edges
                    var rect = new Rectangle(Frame, Frame, sw - Frame * 2, sh - Frame * 2);

                    p.DrawRect(new Rectangle(rect.Left-1, rect.Top-1, 3, rect.Height), 0);
                    p.DrawRect(new Rectangle(rect.Right - 2, rect.Top-1, 3, rect.Height), 0);
                    p.DrawRect(new Rectangle(rect.Left-1, rect.Top-1, rect.Width, 3), 0);
                    p.DrawRect(new Rectangle(rect.Left-1, rect.Bottom - 2, rect.Width, 3), 0);

                    p.DrawRect(new Rectangle(rect.Left, rect.Top, 1, rect.Height), Color);
                    p.DrawRect(new Rectangle(rect.Right - 1, rect.Top, 1, rect.Height), Color);
                    p.DrawRect(new Rectangle(rect.Left, rect.Top, rect.Width, 1), Color);
                    p.DrawRect(new Rectangle(rect.Left, rect.Bottom - 1, rect.Width, 1), Color);
                    return (Frame < sw / 2);

                case VMEODNCParticleType.Colder:
                    //pulses out from the center, either at the top or bottom of the dance floor
                    var fm = Frame % (sw / 2);
                    var ypos = (GroupID % 2 == 0) ? 0 : sh-2;

                    var xp1 = sw / 2 - fm;
                    var xp2 = sw / 2 + fm;

                    p.DrawRect(new Rectangle(0, ypos, sw, 2), (byte)0);

                    p.DrawRect(new Rectangle(xp1 - 1, ypos, 3, 2), (byte)(Color + 1));
                    p.DrawRect(new Rectangle(xp2 - 1, ypos, 3, 2), (byte)(Color + 1));

                    p.DrawRect(new Rectangle(xp1, ypos, 1, 2), (byte)(Color));
                    p.DrawRect(new Rectangle(xp2, ypos, 1, 2), (byte)(Color));

                    return (Frame < sw * 2);
            }
            return false;
        }
    }

    public enum VMEODNCParticleType
    {
        Rect = 0,
        Line = 1,
        Arrow = 2,
        Colder = 3,
    }

    public enum VMEODNCDanceFloorEvents : short
    {
        Idle = 0,
        DiscoverTiles = 1,
        SetAnimation = 2,
        Tick = 3,
        SetRatings = 4
    }

    public enum VMEODNCAnimTypes : short
    {
        Off = 0,
        Fill = 1,
        Random = 2,
        Line = 3,
        Arrow = 4,
        DancersBonus = 5, //heart
        DJsBonus = 6, //diamond
        AllBonus = 7 //star
    }

    public enum VMEODNCRandomAnims : short
    {
        RandomDissolve = 0,
        RainbowTunnel = 1,
        Circles = 2,
        Matrix = 3,
        Text = 4,
    }
}
