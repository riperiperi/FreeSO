/* 
* 12/12/99  Initial Version based on FileObuffer.	mdm@techie.com.
* 
* FileObuffer:
* 15/02/99 ,Java Conversion by E.B ,ebsp@iname.com, JavaLayer
*
*----------------------------------------------------------------------------- 
*  This program is free software; you can redistribute it and/or modify
*  it under the terms of the GNU General Public License as published by
*  the Free Software Foundation; either version 2 of the License, or
*  (at your option) any later version.
*
*  This program is distributed in the hope that it will be useful,
*  but WITHOUT ANY WARRANTY; without even the implied warranty of
*  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
*  GNU General Public License for more details.
*
*  You should have received a copy of the GNU General Public License
*  along with this program; if not, write to the Free Software
*  Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
*----------------------------------------------------------------------------
*/
namespace javazoom.jl.decoder
{
	using System;
	
	/// <summary> The <code>SampleBuffer</code> class implements an output buffer
	/// that provides storage for a fixed size block of samples. 
	/// 
	/// 
	/// </summary>
	internal class SampleBuffer:Obuffer
	{
		virtual public int ChannelCount
		{
			get
			{
				return this.channels;
			}
			
		}
		virtual public int SampleFrequency
		{
			get
			{
				return this.frequency;
			}
			
		}
		virtual public short[] Buffer
		{
			get
			{
				return this.buffer;
			}
			
		}
		virtual public int BufferLength
		{
			get
			{
				return bufferp[0];
			}
			
		}
		private short[] buffer;
		private int[] bufferp;
		private int channels;
		private int frequency;
		
		/// <summary> Constructor
		/// </summary>
		public SampleBuffer(int sample_frequency, int number_of_channels)
		{
			buffer = new short[OBUFFERSIZE];
			bufferp = new int[MAXCHANNELS];
			channels = number_of_channels;
			frequency = sample_frequency;
			
			for (int i = 0; i < number_of_channels; ++i)
				bufferp[i] = (short) i;
		}
		
		
		
		
		
		/// <summary> Takes a 16 Bit PCM sample.
		/// </summary>
		public override void  append(int channel, short value_Renamed)
		{
			buffer[bufferp[channel]] = value_Renamed;
			bufferp[channel] += channels;
		}
		
		public override void  appendSamples(int channel, float[] f)
		{
			int pos = bufferp[channel];
			
			short s;
			float fs;
			for (int i = 0; i < 32; )
			{
				fs = f[i++];
				fs = (fs > 32767.0f?32767.0f:(fs < - 32767.0f?- 32767.0f:fs));
				
				//UPGRADE_WARNING: Narrowing conversions may produce unexpected results in C#. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1042"'
				s = (short) fs;
				buffer[pos] = s;
				pos += channels;
			}
			
			bufferp[channel] = pos;
		}
		
		
		/// <summary> Write the samples to the file (Random Acces).
		/// </summary>
		public override void  write_buffer(int val)
		{
			
			//for (int i = 0; i < channels; ++i) 
			//	bufferp[i] = (short)i;
		}
		
		public override void  close()
		{
		}
		
		/// <summary>*
		/// </summary>
		public override void  clear_buffer()
		{
			for (int i = 0; i < channels; ++i)
				bufferp[i] = (short) i;
		}
		
		/// <summary>*
		/// </summary>
		public override void  set_stop_flag()
		{
		}
	}
}