/*
* 06/04/01		Too fast playback fixed. mdm@techie.com
* 29/01/00		Initial version. mdm@techie.com
/*-----------------------------------------------------------------------
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
*----------------------------------------------------------------------
*/
namespace javazoom.jl.player
{
	using System;
	using javazoom.jl.decoder;
	using javax.sound.sampled;
	/// <summary> The <code>JavaSoundAudioDevice</code> implements an audio
	/// device by using the JavaSound API.
	/// *
	/// @since 0.0.8
	/// </summary>
	/// <author>  Mat McGowan
	/// 
	/// </author>
	public class JavaSoundAudioDevice:AudioDeviceBase
	{
		public JavaSoundAudioDevice()
		{
			InitBlock();
		}
		private void  InitBlock()
		{
			byteBuf = new sbyte[1024];
		}
		virtual protected internal AudioFormat AudioFormat
		{
			get
			{
				if (fmt == null)
				{
					Decoder decoder = Decoder;
					fmt = new AudioFormat(decoder.OutputFrequency, 16, decoder.OutputChannels, true, false);
				}
				return fmt;
			}
			
			set
			{
				fmt = value;
			}
			
		}
		virtual protected internal DataLine.Info SourceLineInfo
		{
			get
			{
				AudioFormat fmt = AudioFormat;
				DataLine.Info info = new DataLine.Info(typeof(SourceDataLine), fmt, 4000);
				return info;
			}
			
		}
		override public int Position
		{
			get
			{
				int pos = 0;
				if (source != null)
				{
					pos = (int) (source.MicrosecondPosition / 1000);
				}
				return pos;
			}
			
		}
		private SourceDataLine source = null;
		
		private AudioFormat fmt = null;
		
		//UPGRADE_NOTE: The initialization of  'byteBuf' was moved to method 'InitBlock'. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1005"'
		private sbyte[] byteBuf;
		
		
		
		
		public virtual void  open(AudioFormat fmt)
		{
			if (!Open)
			{
				setAudioFormat(fmt);
				openImpl();
				Open = true;
			}
		}
		
		protected internal override void  openImpl()
		{
		}
		
		
		// createSource fix.
		protected internal virtual void  createSource()
		{
			//UPGRADE_NOTE: Exception 'java.lang.Throwable' was converted to 'System.Exception' which has different behavior. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1100"'
			System.Exception t = null;
			try
			{
				Line line = AudioSystem.getLine(SourceLineInfo);
				if (line is SourceDataLine)
				{
					source = (SourceDataLine) line;
					source.open(fmt, millisecondsToBytes(fmt, 2000));
					/*
					if (source.isControlSupported(FloatControl.Type.MASTER_GAIN))
					{
					FloatControl c = (FloatControl)source.getControl(FloatControl.Type.MASTER_GAIN);
					c.setValue(c.getMaximum());
					}*/
					source.start();
				}
			}
			catch (System.SystemException ex)
			{
				t = ex;
			}
			catch (System.ApplicationException ex)
			{
				t = ex;
			}
			catch (LineUnavailableException ex)
			{
				t = ex;
			}
			if (source == null)
				throw new JavaLayerException("cannot obtain source audio line", t);
		}
		
		public virtual int millisecondsToBytes(AudioFormat fmt, int time)
		{
			//UPGRADE_WARNING: Narrowing conversions may produce unexpected results in C#. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1042"'
			return (int) (time * (fmt.SampleRate * fmt.Channels * fmt.SampleSizeInBits) / 8000.0);
		}
		
		protected internal override void  closeImpl()
		{
			if (source != null)
			{
				source.close();
			}
		}
		
		protected internal override void  writeImpl(short[] samples, int offs, int len)
		{
			if (source == null)
				createSource();
			
			sbyte[] b = toByteArray(samples, offs, len);
			source.write(b, 0, len * 2);
		}
		
		protected internal virtual sbyte[] getByteArray(int length)
		{
			if (byteBuf.Length < length)
			{
				byteBuf = new sbyte[length + 1024];
			}
			return byteBuf;
		}
		
		protected internal virtual sbyte[] toByteArray(short[] samples, int offs, int len)
		{
			sbyte[] b = getByteArray(len * 2);
			int idx = 0;
			short s;
			while (len-- > 0)
			{
				s = samples[offs++];
				b[idx++] = (sbyte) s;
				b[idx++] = (sbyte) (SupportClass.URShift(s, 8));
			}
			return b;
		}
		
		protected internal override void  flushImpl()
		{
			if (source != null)
			{
				source.drain();
			}
		}
		
		
		/// <summary> Runs a short test by playing a short silent sound.
		/// </summary>
		public virtual void  test()
		{
			try
			{
				open(new AudioFormat(22050, 16, 1, true, false));
				short[] data = new short[22050 / 10];
				write(data, 0, data.Length);
				flush();
				close();
			}
			catch (System.SystemException ex)
			{
				throw new JavaLayerException("Device test failed: " + ex);
			}
		}
	}
}