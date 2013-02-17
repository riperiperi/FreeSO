/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using TSOClient.LUI;
using Un4seen.Bass;

namespace TSOClient
{
    /// <summary>
    /// Functions called from Lua.
    /// </summary>
    public class LuaFunctions
    {

        #region Music

        //TODO: Might want to phase these functions out into their own class if this
        //      class becomes very big...

        private static List<MusicTrack> m_Tracks = new List<MusicTrack>();

        /// <summary>
        /// Starts streaming music from a designated file.
        /// </summary>
        /// <param name="Path">The path to the file.</param>
        /// <param name="ID">The ID of the file.</param>
        /// <param name="Loop">Whether or not to loop the playback.</param>
        /// <returns>The music track's channel.</returns>
        public static int LoadMusictrack(string Path, int ID, bool Loop)
        {
            int Channel = Bass.BASS_StreamCreateFile(Path, 0, 0, BASSFlag.BASS_DEFAULT);

            if (Loop)
                Bass.BASS_ChannelFlags(Channel, BASSFlag.BASS_MUSIC_LOOP, BASSFlag.BASS_MUSIC_LOOP);

            MusicTrack Track = new MusicTrack(ID, Channel);
            m_Tracks.Add(Track);

            Bass.BASS_ChannelPlay(Channel, false);

            return Channel;
        }

        /// <summary>
        /// Stops a music track.
        /// </summary>
        /// <param name="Channel">The channel of the music track.</param>
        public static void StopMusictrack(int Channel)
        {
            Bass.BASS_ChannelStop(Channel);
        }

        /// <summary>
        /// Checks whether or not a music track is currently playing.
        /// </summary>
        /// <param name="Channel">The channel of the music track.</param>
        /// <returns>True if it is playing, false otherwise.</returns>
        public static bool IsMusictrackPlaying(int Channel)
        {
            if (Bass.BASS_ChannelIsActive(Channel) == BASSActive.BASS_ACTIVE_PLAYING)
                return true;
            else if (Bass.BASS_ChannelIsActive(Channel) == BASSActive.BASS_ACTIVE_PAUSED ||
                Bass.BASS_ChannelIsActive(Channel) == BASSActive.BASS_ACTIVE_STOPPED)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Removes all musical tracks, regardless of whether they are currently playing or not.
        /// Called when exiting the application from Lua or when changing a screen.
        /// </summary>
        public static void RemoveAllMusictracks()
        {
            m_Tracks.Clear();
        }

        #endregion

        /// <summary>
        /// Called from Lua, in order to exit the application.
        /// </summary>
        /// <param name="ExitCode">The code to pass to the OS when exiting (usually 0).</param>
        public static void ApplicationExit(int ExitCode)
        {
            Environment.Exit(ExitCode);
        }

        /// <summary>
        /// Called at application startup, to process the application's settings from
        /// a Lua script.
        /// </summary>
        /// <param name="Path">The path to the Lua script containing the settings.</param>
        public static void ReadSettings(string Path)
        {
            LuaInterfaceManager.RunFileInThread(Path);
            
            GlobalSettings.Default.ShowHints = (bool)LuaInterfaceManager.LuaVM["ShowHints"];
            GlobalSettings.Default.CurrentLang = (string)LuaInterfaceManager.LuaVM["CurrentLang"];
            
            GlobalSettings.Default.LoginServerIP = (string)LuaInterfaceManager.LuaVM["LoginServerIP"];
            GlobalSettings.Default.LoginServerPort = (int)(double)LuaInterfaceManager.LuaVM["LoginServerPort"];

            GlobalSettings.Default.GraphicsWidth = (int)(double)LuaInterfaceManager.LuaVM["CurrentWidth"];
            GlobalSettings.Default.GraphicsHeight = (int)(double)LuaInterfaceManager.LuaVM["CurrentHeight"];
        }
    }
}
