/*
* 12/12/99	 0.0.7 Renamed class, additional constructor arguments 
*			 and larger write buffers. mdm@techie.com.
*
* 15/02/99 ,Java Conversion by E.B ,ebsp@iname.com, JavaLayer
*/
namespace javazoom.jl.converter
{
	using System;
	using Obuffer = javazoom.jl.decoder.Obuffer;
	/// <summary> Implements an Obuffer by writing the data to
	/// a file in RIFF WAVE format. 
	/// 
	/// @since 0.0
	/// </summary>
	
	
	internal class WaveFileObuffer:Obuffer
	{
		private void  InitBlock()
		{
			myBuffer = new short[2];
		}
		private short[] buffer;
		private short[] bufferp;
		private int channels;
		private WaveFile outWave;
		
		/// <summary> Creates a new WareFileObuffer instance. 
		/// 
		/// </summary>
		/// <param name="">number_of_channels	
		/// The number of channels of audio data
		/// this buffer will receive. 
		/// 
		/// </param>
		/// <param name="freq	The">sample frequency of the samples in the buffer.
		/// 
		/// </param>
		/// <param name="fileName	The">filename to write the data to.
		/// 
		/// </param>
		public WaveFileObuffer(int number_of_channels, int freq, System.String FileName)
		{
			InitBlock();
			if (FileName == null)
				throw new System.NullReferenceException("FileName");
			
			buffer = new short[OBUFFERSIZE];
			bufferp = new short[MAXCHANNELS];
			channels = number_of_channels;
			
			for (int i = 0; i < number_of_channels; ++i)
				bufferp[i] = (short) i;
			
			outWave = new WaveFile();
			
			int rc = outWave.OpenForWrite(FileName, null, freq, (short) 16, (short) channels);
		}

		public WaveFileObuffer(int number_of_channels, int freq, System.IO.Stream stream)
		{
			InitBlock();
			
			buffer = new short[OBUFFERSIZE];
			bufferp = new short[MAXCHANNELS];
			channels = number_of_channels;
			
			for (int i = 0; i < number_of_channels; ++i)
				bufferp[i] = (short) i;
			
			outWave = new WaveFile();
			
			int rc = outWave.OpenForWrite(null, stream, freq, (short) 16, (short) channels);
		}


		/// <summary> Takes a 16 Bit PCM sample.
		/// </summary>
		public override void  append(int channel, short value_Renamed)
		{
			buffer[bufferp[channel]] = value_Renamed;
			bufferp[channel] = (short) (bufferp[channel] + channels);
		}
		
		/// <summary> Write the samples to the file (Random Acces).
		/// </summary>
		//UPGRADE_NOTE: The initialization of  'myBuffer' was moved to method 'InitBlock'. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1005"'
		internal short[] myBuffer;
		public override void  write_buffer(int val)
		{
			
			int k = 0;
			int rc = 0;
			
			rc = outWave.WriteData(buffer, bufferp[0]);
			// REVIEW: handle RiffFile errors. 
			/*
			for (int j=0;j<bufferp[0];j=j+2)
			{
			
			//myBuffer[0] = (short)(((buffer[j]>>8)&0x000000FF) | ((buffer[j]<<8)&0x0000FF00));
			//myBuffer[1] = (short) (((buffer[j+1]>>8)&0x000000FF) | ((buffer[j+1]<<8)&0x0000FF00));
			myBuffer[0] = buffer[j];
			myBuffer[1] = buffer[j+1];
			rc = outWave.WriteData (myBuffer,2);
			}
			*/
			for (int i = 0; i < channels; ++i)
				bufferp[i] = (short) i;
		}
		
		public void  close(bool justWriteLengthBytes)
		{
			outWave.Close(justWriteLengthBytes);
		}

		public override void  close()
		{
			outWave.Close();
		}


		/// <summary>*
		/// </summary>
		public override void  clear_buffer()
		{
		}
		
		/// <summary>*
		/// </summary>
		public override void  set_stop_flag()
		{
		}
		
		/*
		* Create STDOUT buffer
		*
		*
		public static Obuffer create_stdout_obuffer(MPEG_Args maplay_args)
		{
		Obuffer thebuffer = null;
		int mode = maplay_args.MPEGheader.mode();
		int which_channels = maplay_args.which_c;
		if (mode == Header.single_channel || which_channels != MPEG_Args.both)
		thebuffer = new FileObuffer(1,maplay_args.output_filename);
		else
		thebuffer = new FileObuffer(2,maplay_args.output_filename);
		return(thebuffer);
		}
		*/
	}
}