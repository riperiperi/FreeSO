using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.Model
{
    public class VMWallLineBuilder
    {
        //x = a&0xFFF;
        //y = (a>>12)&0xFFF;
        //type = (a>>24);
        public Dictionary<uint, Vector2[]> LinesByLocation = new Dictionary<uint, Vector2[]>();
        public List<Vector2[]> Lines = new List<Vector2[]>();

        public void AddLine(int obsX, int obsY, int type)
        {
            //types:
            //0: line across y (x static)
            //1: line across x (y static)
            //2: diag (low y to high y, high x to low x)
            //3: diag (low x,y to high x,y)

            uint myID = (uint)(obsX | (obsY << 12) | (type << 24));
            if (LinesByLocation.ContainsKey(myID)) return;

            Vector2[] prev = null;
            Vector2[] post = null;
            Vector2[] line = null;
            switch (type)
            {
                case 0:
                    LinesByLocation.TryGetValue((uint)(obsX | ((obsY - 16) << 12) | (type << 24)), out prev);
                    LinesByLocation.TryGetValue((uint)(obsX | ((obsY + 16) << 12) | (type << 24)), out post);
                    if (prev != null)
                    {
                        prev[1].Y += 16;
                        line = prev;
                    } else if (post != null)
                    {
                        post[0].Y -= 16;
                        line = post;
                    } else
                    {
                        line = new Vector2[] { new Vector2(obsX, obsY), new Vector2(obsX, obsY + 16) };
                        Lines.Add(line);
                    }
                    break;
                case 1:
                    LinesByLocation.TryGetValue((uint)((obsX - 16) | (obsY << 12) | (type << 24)), out prev);
                    LinesByLocation.TryGetValue((uint)((obsX + 16) | (obsY << 12) | (type << 24)), out post);
                    if (prev != null)
                    {
                        prev[1].X += 16;
                        line = prev;
                    }
                    else if (post != null)
                    {
                        post[0].X -= 16;
                        line = post;
                    }
                    else
                    {
                        line = new Vector2[] { new Vector2(obsX, obsY), new Vector2(obsX + 16, obsY) };
                        Lines.Add(line);
                    }
                    break;
                case 2:
                    LinesByLocation.TryGetValue((uint)((obsX + 16) | ((obsY - 16) << 12) | (type << 24)), out prev);
                    LinesByLocation.TryGetValue((uint)((obsX - 16) | ((obsY + 16) << 12) | (type << 24)), out post);
                    if (prev != null)
                    {
                        prev[1].Y += 16;
                        prev[1].X -= 16;
                        line = prev;
                    }
                    else if (post != null)
                    {
                        post[0].Y -= 16;
                        post[0].X += 16;
                        line = post;
                    }
                    else
                    {
                        line = new Vector2[] { new Vector2(obsX, obsY), new Vector2(obsX - 16, obsY + 16) };
                        Lines.Add(line);
                    }
                    break;
                case 3:
                    LinesByLocation.TryGetValue((uint)((obsX - 16) | ((obsY - 16) << 12) | (type << 24)), out prev);
                    LinesByLocation.TryGetValue((uint)((obsX + 16) | ((obsY + 16) << 12) | (type << 24)), out post);
                    if (prev != null)
                    {
                        prev[1].Y += 16;
                        prev[1].X += 16;
                        line = prev;
                    }
                    else if (post != null)
                    {
                        post[0].Y -= 16;
                        post[0].X -= 16;
                        line = post;
                    }
                    else
                    {
                        line = new Vector2[] { new Vector2(obsX, obsY), new Vector2(obsX + 16, obsY + 16) };
                        Lines.Add(line);
                    }
                    break;
            }
            LinesByLocation[myID] = line;

        }
    }
}
