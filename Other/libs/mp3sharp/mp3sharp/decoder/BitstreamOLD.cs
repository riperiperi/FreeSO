using Support;
/*
* 12/12/99	 Based on Ibitstream. Exceptions thrown on errors,
*			 Tempoarily removed seek functionality. mdm@techie.com
*
* 02/12/99 : Java Conversion by E.B , ebsp@iname.com , JavaLayer
*
*----------------------------------------------------------------------
*  @(#) ibitstream.h 1.5, last edit: 6/15/94 16:55:34
*  @(#) Copyright (C) 1993, 1994 Tobias Bading (bading@cs.tu-berlin.de)
*  @(#) Berlin University of Technology
*
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
*
*  Changes made by Jeff Tsay :
*  04/14/97 : Added function prototypes for new syncing and seeking
*  mechanisms. Also made this file portable.
*-----------------------------------------------------------------------
*/
namespace javazoom.jl.decoder
{
	using System;
	/// <summary> The <code>Bistream</code> class is responsible for parsing
	/// an MPEG audio bitstream.
	/// *
	/// <b>REVIEW:</b> much of the parsing currently occurs in the
	/// various decoders. This should be moved into this class and associated
	/// inner classes.
	/// </summary>
	public sealed class Bitstream : BitstreamErrors
	{
		private void  InitBlock()
		{
			crc = new Crc16[1];
			syncbuf = new sbyte[4];
			frame_bytes = new sbyte[BUFFER_INT_SIZE * 4];
			framebuffer = new int[BUFFER_INT_SIZE];
			header = new Header();
		}
		
		/// <summary> Syncrhronization control constant for the initial
		/// synchronization to the start of a frame.
		/// </summary>
		internal static sbyte INITIAL_SYNC = 0;
		
		/// <summary> Syncrhronization control constant for non-iniital frame
		/// synchronizations.
		/// </summary>
		internal static sbyte STRICT_SYNC = 1;
		
		// max. 1730 bytes per frame: 144 * 384kbit/s / 32000 Hz + 2 Bytes CRC
		/// <summary> Maximum size of the frame buffer.
		/// </summary>
		private const int BUFFER_INT_SIZE = 433;
		
		
		/// <summary> The frame buffer that holds the data for the current frame.
		/// </summary>
		//UPGRADE_NOTE: Final was removed from the declaration of 'framebuffer '. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1003"'
		//UPGRADE_NOTE: The initialization of  'framebuffer' was moved to method 'InitBlock'. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1005"'
		private int[] framebuffer;
		
		/// <summary> Number of valid bytes in the frame buffer.
		/// </summary>
		private int framesize;
		
		/// <summary> The bytes read from the stream.
		/// </summary>
		//UPGRADE_NOTE: The initialization of  'frame_bytes' was moved to method 'InitBlock'. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1005"'
		private sbyte[] frame_bytes;
		
		/// <summary> Index into <code>framebuffer</code> where the next bits are
		/// retrieved.
		/// </summary>
		private int wordpointer;
		
		/// <summary> Number (0-31, from MSB to LSB) of next bit for get_bits()
		/// </summary>
		private int bitindex;
		
		/// <summary> The current specified syncword
		/// </summary>
		private int syncword;
		
		/// <summary>*
		/// </summary>
		private bool single_ch_mode;
		//private int 			current_frame_number;
		//private int				last_frame_number;
		
		//UPGRADE_NOTE: Final was removed from the declaration of 'bitmask '. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1003"'
		private int[] bitmask = new int[]{0, 0x00000001, 0x00000003, 0x00000007, 0x0000000F, 0x0000001F, 0x0000003F, 0x0000007F, 0x000000FF, 0x000001FF, 0x000003FF, 0x000007FF, 0x00000FFF, 0x00001FFF, 0x00003FFF, 0x00007FFF, 0x0000FFFF, 0x0001FFFF};
		
		//UPGRADE_NOTE: Final was removed from the declaration of 'source '. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1003"'
		private BackStream source;
		
		//UPGRADE_NOTE: Final was removed from the declaration of 'header '. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1003"'
		//UPGRADE_NOTE: The initialization of  'header' was moved to method 'InitBlock'. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1005"'
		private Header header;
		
		//UPGRADE_NOTE: Final was removed from the declaration of 'syncbuf '. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1003"'
		//UPGRADE_NOTE: The initialization of  'syncbuf' was moved to method 'InitBlock'. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1005"'
		private sbyte[] syncbuf;
		
		//UPGRADE_NOTE: The initialization of  'crc' was moved to method 'InitBlock'. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1005"'
		private Crc16[] crc;
		
		//private ByteArrayOutputStream	_baos = null; // E.B
		
		
		/// <summary> Construct a IBitstream that reads data from a
		/// given InputStream.
		/// *
		/// </summary>
		/// <param name="in	The">InputStream to read from.
		/// 
		/// </param>
		public Bitstream(BackStream in_Renamed)
		{
			InitBlock();
			if (in_Renamed == null)
				throw new System.NullReferenceException("in");
			
			source = in_Renamed; // ROB - fuck the SupportClass, let's roll our own. new SupportClass.BackInputStream(in_Renamed, 1024);
			
			//_baos = new ByteArrayOutputStream(); // E.B
			
			closeFrame();
			//current_frame_number = -1;
			//last_frame_number = -1;
		}
		
		public void  close()
		{
			try
			{
				//UPGRADE_TODO: Method 'java.io.FilterInputStream.close' was converted to 'System.IO.BinaryReader.Close' which has a different behavior. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1073_javaioFilterInputStreamclose"'
				source.Close();
				//_baos = null;
			}
			catch (System.IO.IOException ex)
			{
				throw newBitstreamException(javazoom.jl.decoder.BitstreamErrors_Fields.STREAM_ERROR, ex);
			}
		}
		
		/// <summary> Reads and parses the next frame from the input source.
		/// </summary>
		/// <returns> the Header describing details of the frame read,
		/// or null if the end of the stream has been reached.
		/// 
		/// </returns>
		public Header readFrame()
		{
			Header result = null;
			try
			{
				result = readNextFrame();
			}
			catch (BitstreamException ex)
			{
				if (ex.ErrorCode != javazoom.jl.decoder.BitstreamErrors_Fields.STREAM_EOF)
				{
					// wrap original exception so stack trace is maintained.
					throw newBitstreamException(ex.ErrorCode, ex);
				}
			}
			return result;
		}
		
		private Header readNextFrame()
		{
			if (framesize == - 1)
			{
				nextFrame();
			}
			
			return header;
		}
		
		
		/// <summary>*
		/// </summary>
		private void  nextFrame()
		{
			// entire frame is read by the header class.
			header.read_header(this, crc);
		}
		
		/// <summary> Unreads the bytes read from the frame.
		/// @throws BitstreamException
		/// </summary>
		// REVIEW: add new error codes for this.
		public void  unreadFrame()
		{
			if (wordpointer == - 1 && bitindex == - 1 && (framesize > 0))
			{
				try
				{
					//source.UnRead(SupportClass.ToByteArray(frame_bytes), 0, framesize);
					source.UnRead(framesize);
				}
				catch (System.IO.IOException ex)
				{
					throw newBitstreamException(javazoom.jl.decoder.BitstreamErrors_Fields.STREAM_ERROR);
				}
			}
		}
		
		public void  closeFrame()
		{
			framesize = - 1;
			wordpointer = - 1;
			bitindex = - 1;
		}
		
		/// <summary> Determines if the next 4 bytes of the stream represent a
		/// frame header.
		/// </summary>
		public bool isSyncCurrentPosition(int syncmode)
		{
			int read = readBytes(syncbuf, 0, 4);
			int headerstring = ((syncbuf[0] << 24) & (int) SupportClass.Identity(0xFF000000)) | ((syncbuf[1] << 16) & 0x00FF0000) | ((syncbuf[2] << 8) & 0x0000FF00) | ((syncbuf[3] << 0) & 0x000000FF);
			
			try
			{
				//source.UnRead(SupportClass.ToByteArray(syncbuf), 0, read);
				source.UnRead(read);
			}
			catch (System.IO.IOException ex)
			{
			}
			
			bool sync = false;
			switch (read)
			{
				
				case 0: 
					sync = true;
					break;
				
				case 4: 
					sync = isSyncMark(headerstring, syncmode, syncword);
					break;
				}
			
			return sync;
		}
		
		
		// REVIEW: this class should provide inner classes to
		// parse the frame contents. Eventually, readBits will
		// be removed.
		public int readBits(int n)
		{
			return get_bits(n);
		}
		
		public int readCheckedBits(int n)
		{
			// REVIEW: implement CRC check.
			return get_bits(n);
		}
		
		protected internal BitstreamException newBitstreamException(int errorcode)
		{
			return new BitstreamException(errorcode, null);
		}
		//UPGRADE_NOTE: Exception 'java.lang.Throwable' was converted to 'System.Exception' which has different behavior. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1100"'
		protected internal BitstreamException newBitstreamException(int errorcode, System.Exception throwable)
		{
			return new BitstreamException(errorcode, throwable);
		}
		
		
		/// <summary> Get next 32 bits from bitstream.
		/// They are stored in the headerstring.
		/// syncmod allows Synchro flag ID
		/// The returned value is False at the end of stream.
		/// </summary>
		
		internal int syncHeader(sbyte syncmode)
		{
			bool sync;
			int headerstring;
			
			// read additinal 2 bytes
			int bytesRead = readBytes(syncbuf, 0, 3);
			
			if (bytesRead != 3)
				throw newBitstreamException(javazoom.jl.decoder.BitstreamErrors_Fields.STREAM_EOF, null);
			
			//_baos.write(syncbuf, 0, 3); // E.B
			
			headerstring = ((syncbuf[0] << 16) & 0x00FF0000) | ((syncbuf[1] << 8) & 0x0000FF00) | ((syncbuf[2] << 0) & 0x000000FF);
			
			do 
			{
				headerstring <<= 8;
				
				if (readBytes(syncbuf, 3, 1) != 1)
					throw newBitstreamException(javazoom.jl.decoder.BitstreamErrors_Fields.STREAM_EOF, null);
				
				//_baos.write(syncbuf, 3, 1); // E.B
				
				headerstring |= (syncbuf[3] & 0x000000FF);
				
				sync = isSyncMark(headerstring, syncmode, syncword);
			}
			while (!sync);
			
			//current_frame_number++;
			//if (last_frame_number < current_frame_number) last_frame_number = current_frame_number;
			
			return headerstring;
		}
		
		public bool isSyncMark(int headerstring, int syncmode, int word)
		{
			bool sync = false;
			
			if (syncmode == INITIAL_SYNC)
			{
				//sync =  ((headerstring & 0xFFF00000) == 0xFFF00000);
				sync = ((headerstring & 0xFFE00000) == 0xFFE00000); // SZD: MPEG 2.5
			}
			else
			{
				//sync = ((headerstring & 0xFFF80C00) == word) 
				sync = ((headerstring & 0xFFE00000) == 0xFFE00000) // ROB -- THIS IS PROBABLY WRONG. A WEAKER CHECK.
					&& (((headerstring & 0x000000C0) == 0x000000C0) == single_ch_mode);
			}
			
			// filter out invalid sample rate
			if (sync)
			{
				sync = (((SupportClass.URShift(headerstring, 10)) & 3) != 3);
				if (!sync) 	Console.WriteLine("INVALID SAMPLE RATE DETECTED");
			}
			// filter out invalid layer
			if (sync)
			{
				sync = (((SupportClass.URShift(headerstring, 17)) & 3) != 0);
				if (!sync) Console.WriteLine("INVALID LAYER DETECTED");
			}
			// filter out invalid version
			if (sync)
			{
				sync = (((SupportClass.URShift(headerstring, 19)) & 3) != 1);
				if (!sync) Console.WriteLine("INVALID VERSION DETECTED");
			}
			
			return sync;
		}
		
		/// <summary> Reads the data for the next frame. The frame is not parsed
		/// until parse frame is called.
		/// </summary>
		internal void  read_frame_data(int bytesize)
		{
			int numread = 0;
			
			readFully(frame_bytes, 0, bytesize);
			framesize = bytesize;
			wordpointer = - 1;
			bitindex = - 1;
		}
		
		/// <summary> Parses the data previously read with read_frame_data().
		/// </summary>
		internal void  parse_frame()
		{
			// Convert Bytes read to int
			int b = 0;
			sbyte[] byteread = frame_bytes;
			int bytesize = framesize;
			
			for (int k = 0; k < bytesize; k = k + 4)
			{
				int convert = 0;
				sbyte b0 = 0;
				sbyte b1 = 0;
				sbyte b2 = 0;
				sbyte b3 = 0;
				b0 = byteread[k];
				if (k + 1 < bytesize)
					b1 = byteread[k + 1];
				if (k + 2 < bytesize)
					b2 = byteread[k + 2];
				if (k + 3 < bytesize)
					b3 = byteread[k + 3];
				framebuffer[b++] = ((b0 << 24) & (int) SupportClass.Identity(0xFF000000)) | ((b1 << 16) & 0x00FF0000) | ((b2 << 8) & 0x0000FF00) | (b3 & 0x000000FF);
			}
			
			wordpointer = 0;
			bitindex = 0;
		}
		
		/// <summary> Read bits from buffer into the lower bits of an unsigned int.
		/// The LSB contains the latest read bit of the stream.
		/// (1 <= number_of_bits <= 16)
		/// </summary>
		public int get_bits(int number_of_bits)
		{
			
			int returnvalue = 0;
			int sum = bitindex + number_of_bits;
			
			// E.B
			// There is a problem here, wordpointer could be -1 ?!
			if (wordpointer < 0)
				wordpointer = 0;
			// E.B : End.
			
			if (sum <= 32)
			{
				// all bits contained in *wordpointer
				returnvalue = (SupportClass.URShift(framebuffer[wordpointer], (32 - sum))) & bitmask[number_of_bits];
				// returnvalue = (wordpointer[0] >> (32 - sum)) & bitmask[number_of_bits];
				if ((bitindex += number_of_bits) == 32)
				{
					bitindex = 0;
					wordpointer++; // added by me!
				}
				return returnvalue;
			}
			
			// Magouille a Voir
			//((short[])&returnvalue)[0] = ((short[])wordpointer + 1)[0];
			//wordpointer++; // Added by me!
			//((short[])&returnvalue + 1)[0] = ((short[])wordpointer)[0];
			int Right = (framebuffer[wordpointer] & 0x0000FFFF);
			wordpointer++;
			int Left = (framebuffer[wordpointer] & (int) SupportClass.Identity(0xFFFF0000));
			returnvalue = ((Right << 16) & (int) SupportClass.Identity(0xFFFF0000)) | ((SupportClass.URShift(Left, 16)) & 0x0000FFFF);
			
			returnvalue = SupportClass.URShift(returnvalue, 48 - sum); // returnvalue >>= 16 - (number_of_bits - (32 - bitindex))
			returnvalue &= bitmask[number_of_bits];
			bitindex = sum - 32;
			return returnvalue;
		}
		
		/// <summary> Set the word we want to sync the header to.
		/// In Big-Endian byte order
		/// </summary>
		internal void  set_syncword(int syncword0)
		{
			syncword = syncword0 & unchecked((int)0xFFFFFF3F);
			single_ch_mode = ((syncword0 & 0x000000C0) == 0x000000C0);
		}
		/// <summary> Reads the exact number of bytes from the source
		/// input stream into a byte array.
		/// *
		/// </summary>
		/// <param name="b		The">byte array to read the specified number
		/// of bytes into.
		/// </param>
		/// <param name="offs	The">index in the array where the first byte
		/// read should be stored.
		/// </param>
		/// <param name="len	the">number of bytes to read.
		/// *
		/// </param>
		/// <exception cref=""> BitstreamException is thrown if the specified
		/// number of bytes could not be read from the stream.
		/// 
		/// </exception>
		private void  readFully(sbyte[] b, int offs, int len)
		{
			try
			{
				while (len > 0)
				{
					int bytesread = source.Read(b, offs, len);
					if (bytesread == - 1)
					{
						while (len-- > 0)
						{
							b[offs++] = 0;
						}
						break;
						//throw newBitstreamException(UNEXPECTED_EOF, new EOFException());
					}
					
					offs += bytesread;
					len -= bytesread;
				}
			}
			catch (System.IO.IOException ex)
			{
				throw newBitstreamException(javazoom.jl.decoder.BitstreamErrors_Fields.STREAM_ERROR, ex);
			}
		}
		
		/// <summary> Simlar to readFully, but doesn't throw exception when
		/// EOF is reached.
		/// </summary>
		private int readBytes(sbyte[] b, int offs, int len)
		{
			int totalBytesRead = 0;
			try
			{
				while (len > 0)
				{
					int bytesread = source.Read(b, offs, len);
//					for (int i = 0; i < len; i++) b[i] = (sbyte)Temp[i];
					if (bytesread == - 1 || bytesread == 0)
					{
						break;
					}
					totalBytesRead += bytesread;
					offs += bytesread;
					len -= bytesread;
				}
			}
			catch (System.IO.IOException ex)
			{
				throw newBitstreamException(javazoom.jl.decoder.BitstreamErrors_Fields.STREAM_ERROR, ex);
			}
			return totalBytesRead;
		}
		
		/// <summary> Returns ID3v2 tags.
		/// </summary>
		/*public ByteArrayOutputStream getID3v2()
		{
		return _baos;
		}*/
	}
}