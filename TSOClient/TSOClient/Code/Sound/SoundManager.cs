using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Un4seen.Bass;
using TSOClient.LUI;

namespace TSOClient.Code.Sound
{
    public class SoundManager
    {
        #region Music

        //TODO: Might want to phase these functions out into their own class if this
        //      class becomes very big...

        private List<MusicTrack> m_Tracks = new List<MusicTrack>();


        /// <summary>
        /// Sets the game background music
        /// </summary>
        /// <param name="path"></param>
        /// <param name="loop"></param>
        public int PlayBackgroundMusic(string path)
        {
            return LoadMusicTrack(path, 1, true);
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
        /// Stops a music track.
        /// </summary>
        /// <param name="Channel">The channel of the music track.</param>
        public void StopMusictrack(int Channel)
        {
            Bass.BASS_ChannelStop(Channel);
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
