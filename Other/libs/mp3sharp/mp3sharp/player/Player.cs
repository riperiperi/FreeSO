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
	/// <summary> The <code>Player</code> class implements a simple player for playback
	/// of an MPEG audio stream. 
	/// 
	/// </summary>
	/// <author> 	Mat McGowan
	/// @since	0.0.8
	/// 
	/// </author>
	
	// REVIEW: the audio device should not be opened until the
	// first MPEG audio frame has been decoded. 
	public class Player
	{
		/// <summary> Returns the completed status of this player.
		/// 
		/// </summary>
		/// <returns>	true if all available MPEG audio frames have been
		/// decoded, or false otherwise. 
		/// 
		/// </returns>
		//UPGRADE_NOTE: Synchronized keyword was removed from method 'isComplete'. Lock expression was added. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1027"'
		virtual public bool Complete
		{
			get
			{
				lock (this)
				{
					return complete;
				}
			}
			
		}
		/// <summary> Retrieves the position in milliseconds of the current audio
		/// sample being played. This method delegates to the <code>
		/// AudioDevice</code> that is used by this player to sound
		/// the decoded audio samples. 
		/// </summary>
		virtual public int Position
		{
			get
			{
				int position = lastPosition;
				
				AudioDevice out_Renamed = audio;
				if (out_Renamed != null)
				{
					position = out_Renamed.Position;
				}
				return position;
			}
			
		}
		/// <summary> The current frame number. 
		/// </summary>
		private int frame = 0;
		
		/// <summary> The MPEG audio bitstream. 
		/// </summary>
		// javac blank final bug. 
		/*final*/ private Bitstream bitstream;
		
		/// <summary> The MPEG audio decoder. 
		/// </summary>
		/*final*/ private Decoder decoder;
		
		/// <summary> The AudioDevice the audio samples are written to. 
		/// </summary>
		private AudioDevice audio;
		
		/// <summary> Has the player been closed?
		/// </summary>
		private bool closed = false;
		
		/// <summary> Has the player played back all frames from the stream?
		/// </summary>
		private bool complete = false;
		
		private int lastPosition = 0;
		
		/// <summary> Creates a new <code>Player</code> instance. 
		/// </summary>
		public Player(System.IO.Stream stream):this(stream, null)
		{
		}
		
		public Player(System.IO.Stream stream, AudioDevice device)
		{
			bitstream = new Bitstream(stream);
			decoder = new Decoder();
			
			if (device != null)
			{
				audio = device;
			}
			else
			{
				FactoryRegistry r = FactoryRegistry.systemRegistry();
				audio = r.createAudioDevice();
			}
			audio.open(decoder);
		}
		
		public virtual void  play()
		{
			play(System.Int32.MaxValue);
		}
		
		/// <summary> Plays a number of MPEG audio frames. 
		/// 
		/// </summary>
		/// <param name="frames	The">number of frames to play. 
		/// </param>
		/// <returns>	true if the last frame was played, or false if there are
		/// more frames. 
		/// 
		/// </returns>
		public virtual bool play(int frames)
		{
			bool ret = true;
			
			while (frames-- > 0 && ret)
			{
				ret = decodeFrame();
			}
			
			if (!ret)
			{
				// last frame, ensure all data flushed to the audio device. 
				AudioDevice out_Renamed = audio;
				if (out_Renamed != null)
				{
					out_Renamed.flush();
					lock (this)
					{
						complete = (!closed);
						close();
					}
				}
			}
			return ret;
		}
		
		//UPGRADE_NOTE: Synchronized keyword was removed from method 'close'. Lock expression was added. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1027"'
		/// <summary> Cloases this player. Any audio currently playing is stopped
		/// immediately. 
		/// </summary>
		public virtual void  close()
		{
			lock (this)
			{
				AudioDevice out_Renamed = audio;
				if (out_Renamed != null)
				{
					closed = true;
					audio = null;
					// this may fail, so ensure object state is set up before
					// calling this method. 
					out_Renamed.close();
					lastPosition = out_Renamed.Position;
					try
					{
						bitstream.close();
					}
					catch (BitstreamException ex)
					{
					}
				}
			}
		}
		
		
		
		/// <summary> Decodes a single frame.
		/// 
		/// </summary>
		/// <returns> true if there are no more frames to decode, false otherwise.
		/// 
		/// </returns>
		protected internal virtual bool decodeFrame()
		{
			try
			{
				AudioDevice out_Renamed = audio;
				if (out_Renamed == null)
					return false;
				
				Header h = bitstream.readFrame();
				
				if (h == null)
					return false;
				
				// sample buffer set when decoder constructed
				SampleBuffer output = (SampleBuffer) decoder.decodeFrame(h, bitstream);
				
				lock (this)
				{
					out_Renamed = audio;
					if (out_Renamed != null)
					{
						out_Renamed.write(output.Buffer, 0, output.BufferLength);
					}
				}
				
				bitstream.closeFrame();
			}
			catch (System.SystemException ex)
			{
				throw new JavaLayerException("Exception decoding audio frame", ex);
			}
			/*
			catch (IOException ex)
			{
			System.out.println("exception decoding audio frame: "+ex);
			return false;	
			}
			catch (BitstreamException bitex)
			{
			System.out.println("exception decoding audio frame: "+bitex);
			return false;	
			}
			catch (DecoderException decex)
			{
			System.out.println("exception decoding audio frame: "+decex);
			return false;				
			}*/
			return true;
		}
	}
}