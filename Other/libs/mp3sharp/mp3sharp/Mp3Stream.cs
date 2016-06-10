// $Id: Mp3Stream.cs,v 1.3 2004/08/03 16:20:37 tekhedd Exp $
//
// Fri Jul 30 20:39:30 EDT 2004
// Rewrote the buffer object to hold one frame at a time for 
// efficiency. Commented out some functions rather than taking
// the time to port them. --t/DD

// Rob, Sept 1:
// - Changed access for all classes in this project except Mp3Sharp and the Exceptions to internal 
// - Removed commenting from DecodeFrame method of Mp3Stream
// - Added GPL license to Mp3Sharp.cs
// - Changed version number to 1.4

/*
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


using System;
using System.Diagnostics;
using System.IO;
using System.Collections;

namespace Mp3Sharp
{

	/// <summary>
	/// Provides a view of the sequence of bytes that are produced during the conversion of an MP3 stream
	/// into a 16-bit PCM-encoded ("WAV" format) stream.
	/// </summary>
	public class Mp3Stream : Stream
	{
		/// <summary>
		/// Creates a new stream instance using the provided filename, and the default chunk size of 4096 bytes.
		/// </summary>
		public Mp3Stream(string fileName)
			:this(new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
		{ }
		/// <summary>
		/// Creates a new stream instance using the provided filename and chunk size.
		/// </summary>
		public Mp3Stream(string fileName, int chunkSize)
			:this(new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), chunkSize)
		{ }
		/// <summary>
		/// Creates a new stream instance using the provided stream as a source, and the default chunk size of 4096 bytes.
		/// </summary>
		public Mp3Stream(Stream sourceStream)
			: this(sourceStream, 4096) {}

		/// <summary>
		/// Creates a new stream instance using the provided stream as a source.
		///
		/// TODO: allow selecting stereo or mono in the constructor (note that
		///   this also requires "implementing" the stereo format).
		/// </summary>
		public Mp3Stream(Stream sourceStream, int chunkSize)
		{
			FormatRep = SoundFormat.Pcm16BitStereo;
			SourceStream = sourceStream;
			JZBitStream = new javazoom.jl.decoder.Bitstream(new javazoom.jl.decoder.BackStream(SourceStream, chunkSize));
			QueueOBuffer = new OBuffer16BitStereo();
                   
			JZDecoder.OutputBuffer = QueueOBuffer;
		}

		public int ChunkSize { get { return BackStreamByteCountRep; } }
		private int BackStreamByteCountRep;

		/// <summary>
		/// Used to interface with javaZoom.
		/// </summary>
		private javazoom.jl.decoder.Decoder JZDecoder = new javazoom.jl.decoder.Decoder(javazoom.jl.decoder.Decoder.DefaultParams);
		/// <summary>
		/// Used to interface with javaZoom.
		/// </summary>
		private javazoom.jl.decoder.Bitstream JZBitStream;


		private Stream SourceStream;

		public override bool CanRead { get { return SourceStream.CanRead; } }
		public override bool CanSeek { get { return SourceStream.CanSeek; } }
		public override bool CanWrite { get { return SourceStream.CanWrite; } }
		public override long Length { get { return SourceStream.Length; } }

		public override void Flush() { SourceStream.Flush(); }

		/// <summary>
		/// Gets or sets the position of the source stream.  This is relative to the number of bytes in the MP3 file, rather than
		/// the total number of PCM bytes (typically signicantly greater) contained in the Mp3Stream's output.
		/// </summary>
		public override long Position
		{
			get { return SourceStream.Position; }
			set { SourceStream.Position = value; }
		}
		/// <summary>
		/// Sets the position of the source stream.
		/// </summary>
		public override long Seek(long pos, SeekOrigin origin)
		{
			return SourceStream.Seek(pos, origin);
		}
		/// <summary>
		/// This method is not valid for an Mp3Stream.
		/// </summary>
		public override void SetLength(long len)
		{
			throw new InvalidOperationException();
		}
		/// <summary>
		/// This method is not valid for an Mp3Stream.
		/// </summary>
		public override void Write(byte[] buf, int ofs, int count)
		{
			throw new InvalidOperationException();
		}

		/// <summary>
		/// Gets the frequency of the audio being decoded.  
		/// Initially set to -1.  Initialized during the first call to either of the Read and DecodeFrames methods,
		/// and updated during every subsequent call to one of those methods to reflect the most recent header information
		/// from the MP3 stream.
		/// </summary>
		public int Frequency { get { return FrequencyRep; } }
		private int FrequencyRep = -1;

		/// <summary>
		/// Gets the number of channels available in the audio being decoded.
		/// Initially set to -1.  Initialized during the first call to either of the Read and DecodeFrames methods,
		/// and updated during every subsequent call to one of those methods to reflect the most recent header information
		/// from the MP3 stream.
		/// </summary>
		public short ChannelCount { get { return ChannelCountRep;  } }
		private short ChannelCountRep = -1;

		/// <summary>
		/// Gets or sets the PCM output format of this stream.
		/// </summary>
		public SoundFormat Format
		{
			get { return FormatRep; } 

			// Note: the buffers are stored in an optimized format--changing
			// the Format involves flushing the buffers and so on, so 
			// let's just not, OK?
			// set { FormatRep = value; } 
		}
		protected SoundFormat FormatRep = SoundFormat.Pcm16BitStereo;

		/// <summary>
		/// Decodes the requested number of frames from the MP3 stream 
		/// and caches their PCM-encoded bytes.  These can subsequently be obtained using the Read method.
		/// Returns the number of frames that were successfully decoded.
		/// </summary>
		public int DecodeFrames(int frameCount)
		{
		 	int framesDecoded = 0;
		 	bool aFrameWasRead = true;
		 	while (framesDecoded < frameCount && aFrameWasRead)
		 	{
		 		aFrameWasRead = ReadFrame();
		 		if (aFrameWasRead) framesDecoded++;
		 	}
		 	return framesDecoded;
		}

		/// <summary>
		/// Reads the MP3 stream as PCM-encoded bytes.  Decodes a portion of the stream if necessary.
		/// Returns the number of bytes read.
		/// </summary>
		public override int Read(byte[] buffer, int offset, int count)
		{
			// Copy from queue buffers, reading new ones as necessary,
			// until we can't read more or we have read "count" bytes
			int bytesRead = 0;
			while (true)
			{
				if (QueueOBuffer.bytesLeft <= 0)
				{
					if (!ReadFrame()) // out of frames or end of stream?
						break;
				}

				// Copy as much as we can from the current buffer:
				bytesRead += QueueOBuffer.Read( buffer, 
					offset + bytesRead, 
					count - bytesRead );

				if (bytesRead >= count)
					break;
			}
			return bytesRead;
		}
                      
                   
		// 			bool aFrameWasRead = true;
		// 			while (QueueOBuffer.QueuedByteCount < count && aFrameWasRead)
		// 			{
		// 				aFrameWasRead = ReadFrame();
		// 			}
		// 			int bytesToReturn = Math.Min(QueueOBuffer.QueuedByteCount, count);
		// 			int bytesRead = 0;
		// 			switch(Format)
		// 			{
		// 				case SoundFormat.Pcm16BitMono:
		// 					bytesRead = QueueOBuffer.DequeueAs16BitPcmMono(buffer, offset, bytesToReturn);
		// 					break;
		// 				case SoundFormat.Pcm16BitStereo:
		// 					bytesRead = QueueOBuffer.DequeueAs16BitPcmStereo(buffer, offset, bytesToReturn);
		// 					break;
		// 				default:
		// 					throw new ApplicationException("Unknown sound format in Mp3Stream Read call: " + Format);
		// 			}
		// 			return bytesRead;

		/// <summary>
		/// Reads a single byte of the PCM-encoded stream.
		/// </summary>
		// 		public override int ReadByte()
		// 		{
		// 			byte[] ret = new byte[1];
		// 			int result = Read(ret,0,1);
		// 			if (result == 0) return -1; else return ret[0];
		// 		}

		/// <summary>
		/// Closes the source stream and releases any associated resources.
		/// If you don't call this, you may be leaking file descriptors.
		/// </summary>
		public override void Close()
		{
			JZBitStream.close(); // This should close SourceStream as well.
			// SourceStream.Close();
		}

		private OBuffer16BitStereo QueueOBuffer;

		/// <summary>
		/// Reads a frame from the MP3 stream.  Returns whether the operation was successful.  If it wasn't, 
		/// the source stream is probably at its end.
		/// </summary>
		private bool ReadFrame()
		{
			// Read a frame from the bitstream.
			javazoom.jl.decoder.Header header = JZBitStream.readFrame();
			if (header == null) 
				return false;
                   
			try
			{
				// Set the channel count and frequency values for the stream.
				if (header.mode() == javazoom.jl.decoder.Header.SINGLE_CHANNEL)
					ChannelCountRep = (short)1;
				else
					ChannelCountRep = (short)2;

				FrequencyRep = header.frequency();
                      
				// Decode the frame.
				javazoom.jl.decoder.Obuffer decoderOutput = JZDecoder.decodeFrame(header, JZBitStream);
                      
				// Apparently, the way JavaZoom sets the output buffer 
				// on the decoder is a bit dodgy. Even though
				// this exception should never happen, we test to be sure.
				if (decoderOutput != QueueOBuffer)
					throw new System.ApplicationException("Output buffers are different.");
                      
				// And we're done.
			}
			finally
			{
				// No resource leaks please!
				JZBitStream.closeFrame();
			}
			return true;
		}

	}

	/// <summary>
	/// Describes sound formats that can be produced by the Mp3Stream class.
	/// </summary>
	public enum SoundFormat
	{
		/// <summary>
		/// PCM encoded, 16-bit Mono sound format.
		/// </summary>
		Pcm16BitMono,
		/// <summary>
		/// PCM encoded, 16-bit Stereo sound format.
		/// </summary>
		Pcm16BitStereo,
	}

	/// <summary>
	/// Internal class used to queue samples that are being obtained 
	/// from an Mp3 stream. This merges the old mp3stream OBuffer with
	/// the javazoom SampleBuffer code for the highest efficiency...
	/// well, not the highest possible. The highest I'm willing to sweat
	/// over. --trs
	/// 
	/// This class handles stereo 16-bit data! Switch it out if you want mono or something.
	/// </summary>
	internal class OBuffer16BitStereo 
		: javazoom.jl.decoder.Obuffer
	{
		// This is stereo!
		static readonly int CHANNELS = 2;

		// Read offset used to read from the stream, in bytes.
		int _offset;

		// end marker, one past end of array. Same as bufferp[0], but
		// without the array bounds check.
		int _end;

		// Write offset used in append_bytes
		byte [] buffer = new byte[OBUFFERSIZE * 2]; // all channels interleaved
		int [] bufferp = new int[MAXCHANNELS]; // offset in each channel not same!

		public OBuffer16BitStereo()
		{
			// Initialize the buffer pointers
			clear_buffer();
		}

		public int bytesLeft
		{
			get
			{
				// Note: should be Math.Max( bufferp[0], bufferp[1]-1 ). 
				// Heh.
				return _end - _offset;

				// This results in a measurable performance improvement, but
				// is actually incorrect. Is there a trick to optimize this?
				// return (OBUFFERSIZE * 2) - _offset;
			}
		}

		///
		/// Copies as much of this buffer as will fit into hte output
		/// buffer.
		///
		/// \return The amount of bytes copied.
		///
		public int Read(byte[] buffer_out, int offset, int count)
		{
			int remaining = bytesLeft;
			int copySize;
			if (count > remaining)
			{
				copySize = remaining;
				Debug.Assert( copySize % (2 * CHANNELS) == 0 );
			}
			else
			{
				// Copy an even number of sample frames
				int remainder = count % (2 * CHANNELS);
				copySize = count - remainder;
			}

			Array.Copy( buffer, _offset, buffer_out, offset, copySize );

			_offset += copySize;
			return copySize;
		}

		// Inefficiently write one sample value
		public override void append(int channel, short value)
		{
			buffer[bufferp[channel]]     = (byte)(value & 0xff);
			buffer[bufferp[channel] + 1] = (byte)(value >> 8);

			bufferp[channel] += CHANNELS * 2;
		}

		// efficiently write 32 samples
		public override void  appendSamples(int channel, float[] f)
		{
			// Always, 32 samples are appended
			int pos = bufferp[channel];
     		
			short s;
			float fs;
			for (int i = 0; i < 32; i++)
			{
				fs = f[i];
				if (fs > 32767.0f) // can this happen?
					fs = 32767.0f;
				else if (fs < - 32767.0f)
					fs = - 32767.0f;
                   
				int sample = (int) fs;
				buffer[pos]     = (byte)(sample & 0xff);
				buffer[pos + 1] = (byte)(sample >> 8);
                   
				pos += CHANNELS * 2;
			}
                
			bufferp[channel] = pos;
		}


		/// <summary>
		/// This implementation does not clear the buffer. 
		/// </summary>
		public override void  clear_buffer()  
		{ 
			_offset = 0;
			_end = 0;

			for (int i = 0; i < CHANNELS; i++)
				bufferp[i] = i * 2; // two bytes per channel
		}

		public override void  set_stop_flag() { }
		public override void  write_buffer(int val) 
		{ 
			_offset = 0;

			// speed optimization - save end marker, and avoid
			// array access at read time. Can you believe this saves
			// like 1-2% of the cpu on a PIII? I guess allocating
			// that temporary "new int(0)" is expensive, too.
			_end = bufferp[0]; 
		}
		public override void  close() {}	

	}

}
