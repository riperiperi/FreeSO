using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Audio;
using NAudio.Wave;
using System.IO;
using tso.content.model;

namespace tso.sound
{
    /// <summary>
    /// Audio engine that provides music loops, sound effects and world sounds
    /// </summary>
    public class AudioEngine
    {
        public AudioEngine()
        {
            //
        }

        public void PlayMusic(AudioReference reference)
        {
            //@"C:\Program Files\Maxis\The Sims Online\TSOClient\music\stations\countryd\cntryd1_5df26ad0.mp3"
            var file = Load(reference.FilePath); ;
            //var file = Load(@"C:\Program Files\Maxis\The Sims Online\TSOClient\sounddata\tvstations\tv_comedy_cartoon\tv_c1_12.xa"); ;

            var output = new DirectSoundOut();
            output.Init(file);
            output.Play();
        }



        public IWaveProvider Load(string fileName)
        {
            WaveStream readerStream = null;
            if (fileName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)){
                readerStream = new WaveFileReader(fileName);
                if (readerStream.WaveFormat.Encoding != WaveFormatEncoding.Pcm && readerStream.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
                {
                    readerStream = WaveFormatConversionStream.CreatePcmStream(readerStream);
                    readerStream = new BlockAlignReductionStream(readerStream);
                }
            }else if (fileName.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)){
                readerStream = new Mp3FileReader(fileName);
            }else if (fileName.EndsWith(".aiff")){
                readerStream = new AiffFileReader(fileName);
            }else{
                readerStream = new MediaFoundationReader(fileName);
            }
            return readerStream;
        }

    }
}
