/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Un4seen.Bass;
using TSOClient.Code.Utils;
using TSOClient.LUI;

namespace TSOClient.Code.Sound
{
    public class SoundManager
    {
        #region Music

        //TODO: Might want to phase these functions out into their own class if this
        //      class becomes very big...

        private List<MusicTrack> m_Tracks = new List<MusicTrack>();

        private double m_MusicFade = 0;
        private int m_MusicChannel = -1;
        private string[] m_MusicArray;
        private string m_NewMusic = "";
        private SYNCPROC m_EndedEvent;
        private int m_CurrentTrackNum;

        /// <summary>
        /// Sets the game background music
        /// </summary>
        /// <param name="path"></param>
        /// <param name="loop"></param>
        public int PlayBackgroundMusic(string[] paths)
        {
            m_MusicArray = shuffleArray(paths);
            m_CurrentTrackNum = 0;
            AdvanceMusic();
            return m_MusicChannel;
        }

        private void AdvanceMusic()
        {
            m_NewMusic = m_MusicArray[m_CurrentTrackNum];
            m_CurrentTrackNum = (m_CurrentTrackNum + 1) % m_MusicArray.Length;
        }

        private string[] shuffleArray(string[] input)
        {
            Random random = new Random();

            List<String> outList = new List<String>();
            for (int i = 0; i < input.Length; i++)
            {
                outList.Insert((int)Math.Floor(random.NextDouble() * (outList.Count + 1)), input[i]);
            }

            return (string[])outList.ToArray();
        }

        public void UpdateMusicVolume()
        {
            float maxVolume = GlobalSettings.Default.MusicVolume / 10.0f;
            if (m_MusicChannel != -1)
            {
                if (m_NewMusic == "")
                {
                    Bass.BASS_ChannelSetAttribute(m_MusicChannel, BASSAttribute.BASS_ATTRIB_VOL, maxVolume);
                }
                else
                {
                    Bass.BASS_ChannelSetAttribute(m_MusicChannel, BASSAttribute.BASS_ATTRIB_VOL, maxVolume * (float)Math.Max(0, (m_MusicFade - 0.5) / 1.5));
                }
            }
        }

        public void MusicUpdate()
        {
            if (m_NewMusic != "")
            {
                float maxVolume = GlobalSettings.Default.MusicVolume / 10.0f;
                m_MusicFade -= 1.0 / 60.0;
                if (m_MusicChannel != -1) Bass.BASS_ChannelSetAttribute(m_MusicChannel, BASSAttribute.BASS_ATTRIB_VOL, maxVolume * (float)Math.Max(0, (m_MusicFade - 0.5) / 1.5));
                if (m_MusicFade <= 0)
                {
                    if (m_MusicChannel != -1) { Bass.BASS_ChannelStop(m_MusicChannel); }
                    if (m_NewMusic != "none")
                    {
                        m_MusicChannel = LoadMusicTrack(m_NewMusic, 1, false);
                        Bass.BASS_ChannelSetAttribute(m_MusicChannel, BASSAttribute.BASS_ATTRIB_VOL, maxVolume);
                        m_EndedEvent = new SYNCPROC(MusicEnded);

                        Bass.BASS_ChannelSetSync(m_MusicChannel, BASSSync.BASS_SYNC_END, 0, m_EndedEvent, (IntPtr)0);
                        m_MusicFade = 2;
                    }
                    m_NewMusic = "";
                }
            }
        }

        private void MusicEnded(int Handle, int Channel, int Data, IntPtr ptr)
        {
            m_MusicFade = 0;
            AdvanceMusic();
        }

        /// <summary>
        /// Play UI sounds
        /// </summary>
        /// <param name="id"></param>
        public void PlayUISound(int id)
        {
            PlaySound(UISounds.GetSound(id));
        }

        /// <summary>
        /// Play a pre-loaded sound
        /// </summary>
        /// <param name="sound"></param>
        public void PlaySound(UISound sound)
        {
            if (sound != null)
            {
                Bass.BASS_ChannelPlay(sound.ThisChannel, false);
            }
        }


        /// <summary>
        /// Starts streaming music from a designated file.
        /// </summary>
        /// <param name="Path">The path to the file.</param>
        /// <param name="ID">The ID of the file.</param>
        /// <param name="Loop">Whether or not to loop the playback.</param>
        /// <returns>The music track's channel.</returns>
        public int LoadMusicTrack(string Path, int ID, bool Loop)
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
        /// Stops the music track. We only need one, right?
        /// </summary>
        public void StopMusictrack()
        {
            m_NewMusic = "none";
        }

        /// <summary>
        /// Checks whether or not a music track is currently playing.
        /// </summary>
        /// <param name="Channel">The channel of the music track.</param>
        /// <returns>True if it is playing, false otherwise.</returns>
        public bool IsMusictrackPlaying(int Channel)
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
        public void RemoveAllMusictracks()
        {
            m_Tracks.Clear();
        }

        #endregion
    }
}
