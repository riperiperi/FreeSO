/*
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
	/// <summary> The <code>AudioDeviceBase</code> class provides a simple thread-safe
	/// implementation of the <code>AudioDevice</code> interface. 
	/// Template methods are provided for subclasses to override and
	/// in doing so provide the implementation for the main operations
	/// of the <code>AudioDevice</code> interface. 
	/// 
	/// @since	0.0.8
	/// </summary>
	/// <author> 	Mat McGowan
	/// 
	/// </author>
	/*
	* REVIEW:  It is desirable to be able to use the decoder whe
	*			in the implementation of open(), but the decoder
	*			has not yet read a frame, and so much of the 
	*			desired information (sample rate, channels etc.)
	*			are not available. 
	*/
	public abstract class AudioDeviceBase : AudioDevice
	{
		/// <summary> Sets the open state for this audio device. 
		/// </summary>
		virtual protected internal bool Open
		{
			set
			{
				this.open_Renamed_Field = value;
			}
			
		}
		/// <summary> Determines if this audio device is open or not. 
		/// 
		/// </summary>
		/// <returns> <code>true</code> if the audio device is open,
		/// <code>false</code> if it is not. 	 
		/// 
		/// </returns>
		//UPGRADE_NOTE: Synchronized keyword was removed from method 'isOpen'. Lock expression was added. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1027"'
		virtual public bool IsOpen
		{
			get
			{
				lock (this)
				{
					return open_Renamed_Field;
				}
			}
			
		}
		/// <summary> Retrieves the decoder that provides audio data to this
		/// audio device.
		/// 
		/// </summary>
		/// <returns> The associated decoder. 
		/// 
		/// </returns>
		virtual protected internal Decoder Decoder
		{
			get
			{
				return decoder;
			}
			
		}
		private bool open_Renamed_Field = false;
		
		private Decoder decoder = null;
		
		//UPGRADE_NOTE: Synchronized keyword was removed from method 'open'. Lock expression was added. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1027"'
		/// <summary> Opens this audio device. 
		/// 
		/// </summary>
		/// <param name="decoder	The">decoder that will provide audio data
		/// to this audio device. 
		/// 
		/// </param>
		public virtual void  open(Decoder decoder)
		{
			lock (this)
			{
				if (!Open)
				{
					this.decoder = decoder;
					openImpl();
					Open = true;
				}
			}
		}
		
		/// <summary> Template method to provide the 
		/// implementation for the opening of the audio device. 
		/// </summary>
		protected internal virtual void  openImpl()
		{
		}
		
		
		
		//UPGRADE_NOTE: Synchronized keyword was removed from method 'close'. Lock expression was added. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1027"'
		/// <summary> Closes this audio device. If the device is currently playing 
		/// audio, playback is stopped immediately without flushing
		/// any buffered audio data. 
		/// </summary>
		public virtual void  close()
		{
			lock (this)
			{
				if (Open)
				{
					closeImpl();
					Open = false;
					decoder = null;
				}
			}
		}
		
		/// <summary> Template method to provide the implementation for
		/// closing the audio device. 
		/// </summary>
		protected internal virtual void  closeImpl()
		{
		}
		
		/// <summary> Writes audio data to this audio device. Audio data is
		/// assumed to be in the output format of the decoder. This
		/// method may return before the data has actually been sounded
		/// by the device if the device buffers audio samples. 
		/// 
		/// </summary>
		/// <param name="samples	The">samples to write to the audio device.
		/// </param>
		/// <param name="offs		The">offset into the array of the first sample to write.
		/// </param>
		/// <param name="len		The">number of samples from the array to write. 
		/// @throws JavaLayerException if the audio data could not be
		/// written to the audio device. 
		/// If the audio device is not open, this method does nthing. 
		/// 
		/// </param>
		public virtual void  write(short[] samples, int offs, int len)
		{
			if (Open)
			{
				writeImpl(samples, offs, len);
			}
		}
		
		/// <summary> Template method to provide the implementation for
		/// writing audio samples to the audio device. 
		/// 
		/// </summary>
		/// <seealso cref="">write()
		/// 
		/// </seealso>
		protected internal virtual void  writeImpl(short[] samples, int offs, int len)
		{
		}
		
		/// <summary> Waits for any buffered audio samples to be played by the
		/// audio device. This method should only be called prior 
		/// to closing the device. 
		/// </summary>
		public virtual void  flush()
		{
			if (Open)
			{
				flushImpl();
			}
		}
		
		/// <summary> Template method to provide the implementation for 
		/// flushing any buffered audio data. 
		/// </summary>
		protected internal virtual void  flushImpl()
		{
		}
		
	}
}