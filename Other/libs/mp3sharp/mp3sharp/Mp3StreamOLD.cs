using System;
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
			:this(new FileStream(fileName, FileMode.Open))
		{ }
		/// <summary>
		/// Creates a new stream instance using the provided filename and chunk size.
		/// </summary>
		public Mp3Stream(string fileName, int chunkSize)
			:this(new FileStream(fileName, FileMode.Open), chunkSize)
		{ }
		/// <summary>
		/// Creates a new stream instance using the provided stream as a source, and the default chunk size of 4096 bytes.
		/// </summary>
		public Mp3Stream(Stream sourceStream)
			: this(sourceStream, 4096) {}

		/// <summary>
		/// Creates a new stream instance using the provided stream as a source.
		/// </summary>
		public Mp3Stream(Stream sourceStream, int chunkSize)
		{
			SourceStream = sourceStream;
			JZBitStream = new javazoom.jl.decoder.Bitstream(new javazoom.jl.decoder.BackStream(SourceStream, chunkSize));
			QueueOBuffer = new QueueOBuffer();

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
		/// the Mp3Stream's output.
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
			get { return FormatRep; } set { FormatRep = value; } 
		}
		public SoundFormat FormatRep = SoundFormat.Pcm16BitStereo;

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
		/// </summary>
		public override int Read(byte[] buffer, int offset, int count)
		{
			bool aFrameWasRead = true;
			while (QueueOBuffer.QueuedByteCount < count && aFrameWasRead)
			{
				aFrameWasRead = ReadFrame();
			}
			int bytesToReturn = Math.Min(QueueOBuffer.QueuedByteCount, count);
			int bytesRead = 0;
			switch(Format)
			{
				case SoundFormat.Pcm16BitMono:
					bytesRead = QueueOBuffer.DequeueAs16BitPcmMono(buffer, offset, bytesToReturn);
					break;
				case SoundFormat.Pcm16BitStereo:
					bytesRead = QueueOBuffer.DequeueAs16BitPcmStereo(buffer, offset, bytesToReturn);
					break;
				default:
					throw new ApplicationException("Unknown sound format in Mp3Stream Read call: " + Format);
			}
			return bytesRead;
		}
		/// <summary>
		/// Reads a single byte of the PCM-encoded stream.
		/// </summary>
		public override int ReadByte()
		{
			byte[] ret = new byte[1];
			int result = Read(ret,0,1);
			if (result == 0) return -1; else return ret[0];
		}

		/// <summary>
		/// Closes the source stream and releases any associated resources.
		/// </summary>
		public override void Close()
		{
			SourceStream.Close();
		}

		private QueueOBuffer QueueOBuffer;

		/// <summary>
		/// Reads a frame from the MP3 stream.  Returns whether the operation was successful.  If it wasn't, 
		/// the source stream is probably at its end.
		/// </summary>
		private bool ReadFrame()
		{
			// Read a frame from the bitstream.
			javazoom.jl.decoder.Header header = JZBitStream.readFrame();
			if (header == null) return false;

			// Set the channel count and frequency values for the stream.
			ChannelCountRep = (header.mode() == javazoom.jl.decoder.Header.SINGLE_CHANNEL)?(short)1:(short)2;
			FrequencyRep = header.frequency();

			// Decode the frame.
			javazoom.jl.decoder.Obuffer decoderOutput = JZDecoder.decodeFrame(header, JZBitStream);
							
			// Apparently, the way JavaZoom sets the output buffer 
			// on the decoder is a bit dodgy. Even though
			// this exception should never happen, we test to be sure.
			if (decoderOutput != QueueOBuffer)
				throw new System.ApplicationException("Output buffers are different.");

			// And we're done.
			JZBitStream.closeFrame();
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
	/// Internal class used to queue samples that are being obtained from an Mp3 stream.
	/// </summary>
	internal class QueueOBuffer :javazoom.jl.decoder.Obuffer
	{
		private static int MaxChannels = 2;
		public QueueOBuffer()
		{
			ChannelQueue = new Queue[MaxChannels];
			for (int i = 0; i < ChannelQueue.Length;i++) ChannelQueue[i] = new Queue();
		}

		/// <summary>
		/// TODO in C# 2.0: Convert this to Generic Queues of shorts.
		/// </summary>
		private Queue[] ChannelQueue = new Queue[0];

		public Queue GetChannelQueue(int channelNumber)
		{
			return ChannelQueue[channelNumber];
		}

		/// <summary>
		/// Gets the total number of bytes queued in the buffer.
		/// </summary>
		public int QueuedByteCount
		{
			get
			{
				int total = 0;
				for (int i = 0; i < ChannelQueue.Length; i++) total += 2*ChannelQueue[i].Count;
				return total;
			}
		}

		/// <summary>
		/// Dequeues bytes out of the buffer in 16-it stereo PCM format (16-bit values with alternating channels)
		/// </summary>
		public int DequeueAs16BitPcmStereo(byte[] buffer, int offset, int count)
		{
			System.ComponentModel.ByteConverter bc =  new System.ComponentModel.ByteConverter();
			if (count %2 == 1) count--;
			int firstOffset = offset;
			int lastOffset = count + offset;
			int channelNumber = -1;
			while (offset < lastOffset)
			{
				channelNumber++; channelNumber %= ChannelQueue.Length;
				short sample = (short)ChannelQueue[channelNumber].Dequeue();
				byte[] bytes = BitConverter.GetBytes(sample);
				buffer[offset+0] = bytes[0];
				buffer[offset+1] = bytes[1];
				offset += 2;
			}
			return offset - firstOffset;
		}

		/// <summary>
		/// Dequeues bytes out of the buffer in PCM format (16-bit values with alternating channels)
		/// </summary>
		public int DequeueAs16BitPcmMono(byte[] buffer, int offset, int count)
		{
			throw new ApplicationException("MP3Sharp Mono output not implemented.");
			/// TODO
			return 0;
		}


		public override void append(int channel, short value)
		{
			ChannelQueue[channel].Enqueue(value);			
		}

		/// <summary>
		/// This implementation does not clear the buffer. 
		/// </summary>
		public override void  clear_buffer()  { }	
		public override void  set_stop_flag() { }
		public override void  write_buffer(int val) { }
		public override void  close() {}	

	}

}
