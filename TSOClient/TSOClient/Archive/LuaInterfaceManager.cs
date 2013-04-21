using System;
using System.Collections.Generic;
using System.Text;
using LuaInterface;
using LogThis;

namespace TSOClient
{
    public class LuaInterfaceManager
    {
        public static Lua LuaVM = new Lua();
        private static int m_Retries = 0;

        /// <summary>
        /// Finds a Lua function.
        /// From: http://forum.junowebdesign.com/net-programming/30025-luainterface-explained.html
        /// </summary>
        /// <param name="Name">The full path to the script containing the function.</param>
        /// <returns>The Lua function.</returns>
        public static  LuaFunction FindFunc(string Name)
        {
            try
            {
                LuaFunction retfunc = LuaVM.GetFunction(Name);
                return retfunc;
            }
            catch (Exception e)
            {
                Log.LogThis("Couldn't find Lua function!\r\n " + e.Message, eloglevel.error);  
                return null;
            }
        }

        /// <summary>
        /// Calls a Lua function defined in a script.
        /// From: http://forum.junowebdesign.com/net-programming/30025-luainterface-explained.html
        /// </summary>
        /// <param name="Name">The full path to the Lua script.</param>
        public static void CallFunction(string Name)
        {
            if (FindFunc(Name) == null)
            {
                return;
            }
            try
            {
                LuaFunction retfunc = LuaVM.GetFunction(Name);
                lock (LuaVM)
                {
                    retfunc.Call();
                }

            }
            catch (Exception e)
            {
                Log.LogThis("Couldn't call Lua function!\r\n " + e.Message, eloglevel.error);
            }
        }

        /// <summary>
        /// Calls a Lua function defined in a script.
        /// From: http://forum.junowebdesign.com/net-programming/30025-luainterface-explained.html
        /// </summary>
        /// <param name="Name">The full path to the Lua script.</param>
        public static void CallFunction(string Name, params object[] Arguments)
        {
            if (FindFunc(Name) == null)
            {
                return;
            }
            try
            {
                LuaFunction retfunc = LuaVM.GetFunction(Name);
                lock (LuaVM)
                {
                    retfunc.Call(Arguments);
                }
            }
            catch (Exception e)
            {
                Log.LogThis("Couldn't call Lua function!\r\n " + e.Message, eloglevel.error);
            }
        }

        /// <summary>
        /// Failsafe wrapper for Lua.DoFile().
        /// Basically executes all the code in a script.
        /// From: http://forum.junowebdesign.com/net-programming/30025-luainterface-explained.html
        /// </summary>
        /// <param name="Path">The path to the script.</param>
        public static void RunFileInThread(string Path)
        {
            try
            {
                LuaVM.DoFile(Path);
            }
            catch (Exception ex)
            {
                m_Retries++;

                
                if (m_Retries > 10)
                {
                    Log.LogThis("Error in: " + Path, eloglevel.warn);
                    Log.LogThis("Lua syntax error!\r\n " + ex.ToString(), eloglevel.error);
                }
                else
                {
                    Log.LogThis("Recoverable Lua Error", eloglevel.warn);
                    RunFileInThread(Path);
                }
            }
        }

        /// <summary>
        /// Exports a native object (class or variable) to Lua.
        /// </summary>
        /// <param name="ObjectName">The name of the object as it will appear to Lua.</param>
        /// <param name="ExportableObj">The object to export.</param>
        public static void ExportObject(string ObjectName, object ExportableObj)
        {
            LuaVM[ObjectName] = ExportableObj;
        }
    }
}
