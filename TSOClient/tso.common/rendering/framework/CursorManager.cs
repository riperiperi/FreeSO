/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Reflection;
using System.ComponentModel;
using Microsoft.Xna.Framework;
using System.IO;

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
        IBeam
    }

    /// <summary>
    /// Manages cursors in the game.
    /// </summary>
    public class CursorManager
    {
        public static CursorManager INSTANCE;

        private Dictionary<CursorType, Cursor> m_CursorMap;
        private GameWindow Window;
        public CursorType CurrentCursor { get; internal set;} = CursorType.Normal;

        public CursorManager(GameWindow window)
        {
            INSTANCE = this;
            m_CursorMap = new Dictionary<CursorType, Cursor>();
            this.Window = window;
        }

        public void SetCursor(CursorType type)
        {
            /*
            if (m_CursorMap.ContainsKey(type))
            {
                CurrentCursor = type;
                if (type != CursorType.Normal) Cursor.Current = m_CursorMap[type];
            }*/
        }

        public void Init(string basepath)
        {

            var map = new Dictionary<CursorType, string>(){
                {CursorType.Normal, "arrow.cur"},
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
                {CursorType.LivePerson, "liveperson.cur"}
            };

            foreach(var item in map){
                m_CursorMap.Add(item.Key,
                    LoadCustomCursor(
                        Path.Combine(basepath, @"uigraphics\shared\cursors\" + item.Value)
                    ));
            }

            m_CursorMap.Add(CursorType.IBeam, Cursors.IBeam);
        }



        private static Cursor LoadCustomCursor(string path)
        {
            IntPtr hCurs = LoadCursorFromFile(path);
            if (hCurs == IntPtr.Zero) throw new Win32Exception();
            var curs = new Cursor(hCurs);
            // Note: force the cursor to own the handle so it gets released properly
            var fi = typeof(Cursor).GetField("ownHandle", BindingFlags.NonPublic | BindingFlags.Instance);
            fi.SetValue(curs, true);
            return curs;
        }
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr LoadCursorFromFile(string path);
    }
}
