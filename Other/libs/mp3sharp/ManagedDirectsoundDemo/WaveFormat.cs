using System;
using System.Runtime.InteropServices;

namespace Mp3Sharp
{

	public enum WaveFormats
	{
		Pcm = 1,
		Float = 3
	}

	[StructLayout(LayoutKind.Sequential)] 
	public class WaveFmt
	{
		public short FormatTag;
		public short ChannelCount;
		public int SamplesPerSecond;
		public int AverageBytesPerSecond;
		public short BlockAlign;
		public short BitsPerSample;
		public short CBSize;

		public WaveFmt(int samplingRate, short bitsPerSample, short numChannels)
		{
			FormatTag = (short)WaveFormats.Pcm;
			ChannelCount = (short)numChannels;
			SamplesPerSecond = samplingRate;
			BitsPerSample = (short)bitsPerSample;
			CBSize = 0;
               
			BlockAlign = (short)(numChannels * (bitsPerSample / 8));
			AverageBytesPerSecond = SamplesPerSecond * BlockAlign;
		}
	}

}