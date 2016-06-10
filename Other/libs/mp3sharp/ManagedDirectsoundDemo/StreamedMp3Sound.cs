using System;
using System.IO;
using Microsoft.DirectX.DirectSound;

namespace Mp3Sharp
{
	/// <summary>
	/// A modified version of the StreamedSound class that sets the frequency of the Secondary Buffer based on the
	/// frequency of the first frame of the MP3 file.
	/// </summary>
	public class StreamedMp3Sound : StreamedSound
	{
		public StreamedMp3Sound(Device device, Mp3Stream mp3SourceStream)
			: base(device, mp3SourceStream, SoundUtil.CreateWaveFormat(22050, 16, 2))
		{
		}

		protected override void OnBufferInitializing()
		{
			Mp3Stream stream = Stream as Mp3Stream;
			if (stream == null) throw new ApplicationException("The stream used by the StreamedMp3Sound class should be of type Mp3Stream.");

			if (stream.Frequency < 0) stream.DecodeFrames(1);
			if (stream.Frequency > 0 && stream.ChannelCount > 0)
			{
				this.WaveFormat = SoundUtil.CreateWaveFormat(stream.Frequency, 16, stream.ChannelCount);
			}

		}
	}
}
