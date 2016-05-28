using System;
using Microsoft.DirectX.DirectSound;

namespace Mp3Sharp
{

	/// <summary>
	/// Utility functions for working with sound.
	/// </summary>
	public class SoundUtil
	{
		private SoundUtil() { }

		/// <summary>
		/// Helper method for creating WaveFormat instances
		/// </summary>
		/// <param name="samplingRate">Sampling rate</param>
		/// <param name="bitsPerSample">Bits per sample</param>
		/// <param name="numChannels">Channels</param>
		/// <returns></returns>
		public static WaveFormat CreateWaveFormat(int samplingRate, short bitsPerSample, short numChannels)
		{
			WaveFormat wf = new WaveFormat();

			wf.FormatTag = WaveFormatTag.Pcm;
			wf.SamplesPerSecond = samplingRate;
			wf.BitsPerSample = bitsPerSample;
			wf.Channels = numChannels;

			wf.BlockAlign = (short)(wf.Channels * (wf.BitsPerSample / 8));
			wf.AverageBytesPerSecond = wf.SamplesPerSecond * wf.BlockAlign;

			return wf;
		}

	}
}