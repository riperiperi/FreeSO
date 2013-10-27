using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Reflection;
using System.ComponentModel;
using Microsoft.Xna.Framework;

namespace TSOClient.Code.Utils
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
        ArrowRight
    }

    public class CursorManager
    {
        private Dictionary<CursorType, Cursor> m_CursorMap;
        private GameWindow Window;
        public CursorType CurrentCursor = CursorType.Normal;

        public CursorManager(GameWindow window)
        {
            this.Window = window;
        }

        public void SetCursor(CursorType type)
        {
            if (type == CurrentCursor) { return; }

            if (m_CursorMap.ContainsKey(type))
            {
                CurrentCursor = type;
                Form winForm = (Form)Form.FromHandle(this.Window.Handle);
                winForm.Cursor = m_CursorMap[type];
            }
        }

        public void Init()
        {
            m_CursorMap = new Dictionary<CursorType, Cursor>();

            var map = new Dictionary<CursorType, string>(){
                {CursorType.Normal, "arrow.cur"},
                {CursorType.ArrowUp, "up.cur"},
                {CursorType.ArrowUpLeft, "upleft.cur"},
                {CursorType.ArrowUpRight, "upright.cur"},
                {CursorType.ArrowDown, "down.cur"},
                {CursorType.ArrowDownLeft, "downleft.cur"},
                {CursorType.ArrowDownRight, "downright.cur"},
                {CursorType.ArrowLeft, "left.cur"},
                {CursorType.ArrowRight, "right.cur"}
            };

            foreach(var item in map){
                m_CursorMap.Add(item.Key,
                    LoadCustomCursor(
                        GameFacade.GameFilePath(@"uigraphics\shared\cursors\" + item.Value)
                    ));
            }
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
