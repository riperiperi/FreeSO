/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Reflection;
using System.ComponentModel;
using Microsoft.Xna.Framework;
using System.IO;
using Microsoft.Xna.Framework.Input;
using FSO.Files.Utils;
using Microsoft.Xna.Framework.Graphics;

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
        LiveObjectAvail,
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

        Hourglass
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

        public void Init(string basepath)
        {

            var map = new Dictionary<CursorType, string>(){
                //{CursorType.Normal, "arrow.cur"},
                {CursorType.ArrowUp, "up.cur"},
                {CursorType.ArrowUpLeft, "upleft.cur"},
                {CursorType.ArrowUpRight, "upright.cur"},
                {CursorType.ArrowDown, "down.cur"},
                {CursorType.ArrowDownLeft, "downleft.cur"},
                {CursorType.ArrowDownRight, "downright.cur"},
                {CursorType.ArrowLeft, "left.cur"},
                {CursorType.ArrowRight, "right.cur"},
                {CursorType.LiveNothing, "livenothing.cur"},
                {CursorType.LiveObjectAvail, "liveobjectavail.cur"},
                {CursorType.LiveObjectUnavail, "liveobjectunavail.cur"},
                {CursorType.LivePerson, "liveperson.cur"},

                {CursorType.SimsRotate, "simsrotate.cur" },
                {CursorType.SimsRotateNE, "simsrotatene.cur" },
                {CursorType.SimsRotateNW, "simsrotatenw.cur" },
                {CursorType.SimsRotateSE, "simsrotatese.cur" },
                {CursorType.SimsRotateSW, "simsrotatesw.cur" },

                {CursorType.SimsMove, "simsmove.cur" },
                {CursorType.SimsPlace, "simsplace.cur" },

                {CursorType.Hourglass, "hourglass.cur" }
            };

            foreach(var item in map){
                m_CursorMap.Add(item.Key,
                    LoadCustomCursor(
                        Path.Combine(basepath, "uigraphics/shared/cursors/" + item.Value)
                    ));
            }
            
            m_CursorMap.Add(CursorType.IBeam, MouseCursor.IBeam);
            //m_CursorMap.Add(CursorType.Hourglass, MouseCursor.Wait);
            m_CursorMap.Add(CursorType.Normal, MouseCursor.Arrow);
        }



        private MouseCursor LoadCustomCursor(string path)
        {
            return CurLoader.LoadMonoCursor(GD, File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read));
        }
    }
}
