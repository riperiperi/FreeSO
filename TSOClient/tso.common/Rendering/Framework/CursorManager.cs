using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using FSO.Common.Utils;

namespace FSO.Common.Rendering.Framework
{
    public enum CursorType
    {
        Normal,
        ArrowUp,
        ArrowUpLeft,
        ArrowUpRight,
        ArrowDown,
        ArrowDownLeft,
        ArrowDownRight,
        ArrowLeft,
        ArrowRight,
        LiveNothing,
        LiveObjectUnavail,
        LivePerson,
        IBeam,

        SimsRotate,
        SimsRotateNE,
        SimsRotateSE,
        SimsRotateSW,
        SimsRotateNW,

        SimsMove,
        SimsPlace,

        Hourglass,

        LiveObjectAvail,
        LiveObject1Star,
        LiveObject2Star,
        LiveObject3Star,
        LiveObject4Star,
        LiveObject5Star,
        LiveObjectSpecial,
    }

    /// <summary>
    /// Manages cursors in the game.
    /// </summary>
    public class CursorManager
    {
        public static CursorManager INSTANCE;

        private Dictionary<CursorType, MouseCursor> m_CursorMap;
        private GraphicsDevice GD;
        public CursorType CurrentCursor { get; internal set;} = CursorType.Normal;

        public CursorManager(GraphicsDevice gd)
        {
            INSTANCE = this;
            m_CursorMap = new Dictionary<CursorType, MouseCursor>();
            this.GD = gd;
        }

        public void SetCursor(CursorType type)
        {
            if (m_CursorMap.ContainsKey(type))
            {
                CurrentCursor = type;
                Mouse.SetCursor(m_CursorMap[type]);
            }
        }

        public Dictionary<CursorType, string> GenMap()
        {
            return new Dictionary< CursorType, string> (){
                //{CursorType.Normal, "arrow.cur"},
                { CursorType.ArrowUp, "up.cur"},
                { CursorType.ArrowUpLeft, "upleft.cur"},
                { CursorType.ArrowUpRight, "upright.cur"},
                { CursorType.ArrowDown, "down.cur"},
                { CursorType.ArrowDownLeft, "downleft.cur"},
                { CursorType.ArrowDownRight, "downright.cur"},
                { CursorType.ArrowLeft, "left.cur"},
                { CursorType.ArrowRight, "right.cur"},
                { CursorType.LiveNothing, "livenothing.cur"},
                { CursorType.LiveObjectAvail, "liveobjectavail.cur"},
                { CursorType.LiveObjectUnavail, "liveobjectunavail.cur"},
                { CursorType.LivePerson, "liveperson.cur"},

                { CursorType.SimsRotate, "simsrotate.cur" },
                { CursorType.SimsRotateNE, "simsrotatene.cur" },
                { CursorType.SimsRotateNW, "simsrotatenw.cur" },
                { CursorType.SimsRotateSE, "simsrotatese.cur" },
                { CursorType.SimsRotateSW, "simsrotatesw.cur" },

                { CursorType.SimsMove, "simsmove.cur" },
                { CursorType.SimsPlace, "simsplace.cur" },

                { CursorType.Hourglass, "hourglass.cur" }
            };
        }

        public void Init(string basepath, bool ts1)
        {
            var map = GenMap();
            var curPath = "UIGraphics/Shared/cursors/";
            if (!ts1) curPath = curPath.ToLowerInvariant();
            foreach (var item in map)
            {
                m_CursorMap.Add(item.Key,
                    LoadCustomCursor(
                        Path.Combine(basepath, curPath, item.Value)
                    ));
            }

            var starMax = 5;
            var stars = LoadUpgradeCursors(Path.Combine(basepath, curPath, "liveobjectavail.cur"), starMax);
            for (int i=0; i<starMax; i++)
            {
                m_CursorMap.Add(CursorType.LiveObject1Star + i, stars[i]);
            }

            m_CursorMap.Add(CursorType.IBeam, MouseCursor.IBeam);
            //m_CursorMap.Add(CursorType.Hourglass, MouseCursor.Wait);
            m_CursorMap.Add(CursorType.Normal, MouseCursor.Arrow);
        }

        private MouseCursor[] LoadUpgradeCursors(string path, int maxStars)
        {
            return CurLoader.LoadUpgradeCursors(GD, File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read), maxStars);
        }

        private MouseCursor LoadCustomCursor(string path)
        {
            return CurLoader.LoadMonoCursor(GD, File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read));
        }
    }
}
