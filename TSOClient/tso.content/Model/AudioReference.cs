using System.IO;

namespace FSO.Content.Model
{
    /// <summary>
    /// A reference to an audio file.
    /// </summary>
    public class AudioReference
    {
        /// <summary>
        /// ID of this audio file.
        /// </summary>
        public uint ID { get; set; }

        /// <summary>
        /// Type of this audio.
        /// </summary>
        public AudioType Type { get; set; }

        /// <summary>
        /// Path to this audio file.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Name of the audio file.
        /// </summary>
        public string Name
        {
            get
            {
                return Path.GetFileName(FilePath);
            }
        }
    }
}
