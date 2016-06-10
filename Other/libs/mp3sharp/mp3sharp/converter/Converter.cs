/*
* 12/12/99 Original verion. mdm@techie.com.
*/
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
namespace javazoom.jl.converter
{
	using System;
	using javazoom.jl.decoder;
	/// <summary> The <code>Converter</code> class implements the conversion of
	/// an MPEG audio file to a .WAV file. To convert an MPEG audio stream,
	/// just create an instance of this class and call the {@link convert() convert()}
	/// method, passing in the names of the input and output files. You can
	/// pass in optional <code>ProgressListener</code> and
	/// <code>Decoder.Params</code> objects also to customize the conversion.
	/// *
	/// </summary>
	/// <author> 	MDM		12/12/99
	/// @since	0.0.7
	/// *
	/// 
	/// </author>
	public class Converter
	{
		
		/// <summary> Creates a new converter instance.
		/// </summary>
		public Converter()
		{
		}
		
		//UPGRADE_NOTE: Synchronized keyword was removed from method 'convert'. Lock expression was added. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1027"'
		public virtual void  convert(System.String sourceName, System.String destName)
		{
			lock (this)
			{
				convert(sourceName, destName, null, null);
			}
		}
		
		//UPGRADE_NOTE: Synchronized keyword was removed from method 'convert'. Lock expression was added. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1027"'
		public virtual void  convert(System.String sourceName, System.String destName, ProgressListener progressListener)
		{
			lock (this)
			{
				convert(sourceName, destName, progressListener, null);
			}
		}
		
		
		public virtual void  convert(System.String sourceName, System.String destName, ProgressListener progressListener, Decoder.Params decoderParams)
		{
			if (destName.Length == 0)
				destName = null;
			try
			{
				System.IO.Stream in_Renamed = openInput(sourceName);
				convert(in_Renamed, destName, progressListener, decoderParams);
				in_Renamed.Close();
			}
			catch (System.IO.IOException ioe)
			{
				throw new JavaLayerException(ioe.Message, ioe);
			}
		}
		
		//UPGRADE_NOTE: Synchronized keyword was removed from method 'convert'. Lock expression was added. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1027"'
		public virtual void  convert(System.IO.Stream sourceStream, System.String destName, ProgressListener progressListener, Decoder.Params decoderParams)
		{
			lock (this)
			{
				if (progressListener == null)
					progressListener = PrintWriterProgressListener.newStdOut(PrintWriterProgressListener.NO_DETAIL);
				try
				{
					if (!(sourceStream is System.IO.BufferedStream))
						sourceStream = new System.IO.BufferedStream(sourceStream);
					int frameCount = - 1;
					//UPGRADE_ISSUE: Method 'java.io.InputStream.markSupported' was not converted. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1000_javaioInputStreammarkSupported"'
					if (sourceStream.markSupported())
					{
						//UPGRADE_ISSUE: Method 'java.io.InputStream.mark' was not converted. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1000_javaioInputStreammark_int"'
						sourceStream.mark(- 1);
						frameCount = countFrames(sourceStream);
						//UPGRADE_ISSUE: Method 'java.io.InputStream.reset' was not converted. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1000_javaioInputStreamreset"'
						sourceStream.reset();
					}
					progressListener.converterUpdate(javazoom.jl.converter.Converter.ProgressListener_Fields.UPDATE_FRAME_COUNT, frameCount, 0);
					
					
					Obuffer output = null;
					Decoder decoder = new Decoder(decoderParams);
					Bitstream stream = new Bitstream(sourceStream);
					
					if (frameCount == - 1)
						frameCount = System.Int32.MaxValue;
					
					int frame = 0;
					long startTime = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
					
					try
					{
						for (; frame < frameCount; frame++)
						{
							try
							{
								Header header = stream.readFrame();
								if (header == null)
									break;
								
								progressListener.readFrame(frame, header);
								
								if (output == null)
								{
									// REVIEW: Incorrect functionality.
									// the decoder should provide decoded
									// frequency and channels output as it may differ from
									// the source (e.g. when downmixing stereo to mono.)
									int channels = (header.mode() == Header.SINGLE_CHANNEL)?1:2;
									int freq = header.frequency();
									output = new WaveFileObuffer(channels, freq, destName);
									decoder.OutputBuffer = output;
								}
								
								Obuffer decoderOutput = decoder.decodeFrame(header, stream);
								
								// REVIEW: the way the output buffer is set
								// on the decoder is a bit dodgy. Even though
								// this exception should never happen, we test to be sure.
								if (decoderOutput != output)
									throw new System.ApplicationException("Output buffers are different.");
								
								
								progressListener.decodedFrame(frame, header, output);
								
								stream.closeFrame();
							}
							catch (System.Exception ex)
							{
								bool stop = !progressListener.converterException(ex);
								
								if (stop)
								{
									throw new JavaLayerException(ex.Message, ex);
								}
							}
						}
					}
					finally
					{
						
						if (output != null)
							output.close();
					}
					
					int time = (int) ((System.DateTime.Now.Ticks - 621355968000000000) / 10000 - startTime);
					progressListener.converterUpdate(javazoom.jl.converter.Converter.ProgressListener_Fields.UPDATE_CONVERT_COMPLETE, time, frame);
				}
				catch (System.IO.IOException ex)
				{
					throw new JavaLayerException(ex.Message, ex);
				}
			}
		}
		
		
		protected internal virtual int countFrames(System.IO.Stream in_Renamed)
		{
			return - 1;
		}
		
		
		protected internal virtual System.IO.Stream openInput(System.String fileName)
		{
			// ensure name is abstract path name
			System.IO.FileInfo file = new System.IO.FileInfo(fileName);
			System.IO.Stream fileIn = new System.IO.FileStream(file.FullName, System.IO.FileMode.Open, System.IO.FileAccess.Read);
			System.IO.BufferedStream bufIn = new System.IO.BufferedStream(fileIn);
			
			return bufIn;
		}
		
		
		/// <summary> This interface is used by the Converter to provide
		/// notification of tasks being carried out by the converter,
		/// and to provide new information as it becomes available.
		/// </summary>
		public enum ProgressListener_FieldsEnum
		{
			UPDATE_FRAME_COUNT = 1,
			UPDATE_CONVERT_COMPLETE = 2
		}

		
		public struct ProgressListener_Fields
		{
			public readonly static int UPDATE_FRAME_COUNT = 1;
			public readonly static int UPDATE_CONVERT_COMPLETE = 2;
		}
		public interface ProgressListener
			{
				//UPGRADE_NOTE: Members of interface 'ProgressListener' were extracted into structure 'ProgressListener_Fields'. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1045"'
				/// <summary> Conversion is complete. Param1 contains the time
				/// to convert in milliseconds. Param2 contains the number
				/// of MPEG audio frames converted.
				/// </summary>
				/// <summary> Notifies the listener that new information is available.
				/// *
				/// </summary>
				/// <param name="updateID	Code">indicating the information that has been
				/// updated.
				/// *
				/// </param>
				/// <param name="param1	Parameter">whose value depends upon the update code.
				/// </param>
				/// <param name="param2	Parameter">whose value depends upon the update code.
				/// *
				/// The <code>updateID</code> parameter can take these values:
				/// *
				/// UPDATE_FRAME_COUNT: param1 is the frame count, or -1 if not known.
				/// UPDATE_CONVERT_COMPLETE: param1 is the conversion time, param2
				/// is the number of frames converted.
				/// 
				/// </param>
				void  converterUpdate(int updateID, int param1, int param2);
				/// <summary> If the converter wishes to make a first pass over the
				/// audio frames, this is called as each frame is parsed.
				/// </summary>
				void  parsedFrame(int frameNo, Header header);
				/// <summary> This method is called after each frame has been read,
				/// but before it has been decoded.
				/// *
				/// </summary>
				/// <param name="frameNo	The">0-based sequence number of the frame.
				/// </param>
				/// <param name="header	The">Header rerpesenting the frame just read.
				/// 
				/// </param>
				void  readFrame(int frameNo, Header header);
				/// <summary> This method is called after a frame has been decoded.
				/// *
				/// </summary>
				/// <param name="frameNo	The">0-based sequence number of the frame.
				/// </param>
				/// <param name="header	The">Header rerpesenting the frame just read.
				/// </param>
				/// <param name="o			The">Obuffer the deocded data was written to.
				/// 
				/// </param>
				void  decodedFrame(int frameNo, Header header, Obuffer o);
				//UPGRADE_NOTE: Exception 'java.lang.Throwable' was converted to 'System.Exception' which has different behavior. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1100"'
				/// <summary> Called when an exception is thrown during while converting
				/// a frame.
				/// *
				/// </summary>
				/// <param name="t	The"><code>Throwable</code> instance that
				/// was thrown.
				/// *
				/// </param>
				/// <returns> <code>true</code> to continue processing, or false
				/// to abort conversion.
				/// *
				/// If this method returns <code>false</code>, the exception
				/// is propagated to the caller of the convert() method. If
				/// <code>true</code> is returned, the exception is silently
				/// ignored and the converter moves onto the next frame.
				/// 
				/// </returns>
				bool converterException(System.Exception t);
			}
		
		
		/// <summary> Implementation of <code>ProgressListener</code> that writes
		/// notification text to a <code>PrintWriter</code>.
		/// </summary>
		// REVIEW: i18n of text and order required.
		public class PrintWriterProgressListener : ProgressListener
		{
			public const int NO_DETAIL = 0;
			
			/// <summary> Level of detail typically expected of expert
			/// users.
			/// </summary>
			public const int EXPERT_DETAIL = 1;
			
			/// <summary> Verbose detail.
			/// </summary>
			public const int VERBOSE_DETAIL = 2;
			
			/// <summary> Debug detail. All frame read notifications are shown.
			/// </summary>
			public const int DEBUG_DETAIL = 7;
			
			public const int MAX_DETAIL = 10;
			
			private System.IO.StreamWriter pw;
			
			private int detailLevel;
			
			static public PrintWriterProgressListener newStdOut(int detail)
			{
				System.IO.StreamWriter temp_writer;
				//UPGRADE_ISSUE: 'java.lang.System.out' was converted to 'System.Console.Out' which is not valid in this expression. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1109"'
				temp_writer = new System.IO.StreamWriter(System.Console.Out);
				temp_writer.AutoFlush = true;
				return new PrintWriterProgressListener(temp_writer, detail);
			}
			
			public PrintWriterProgressListener(System.IO.StreamWriter writer, int detailLevel)
			{
				this.pw = writer;
				this.detailLevel = detailLevel;
			}
			
			
			public virtual bool isDetail(int detail)
			{
				return (this.detailLevel >= detail);
			}
			
			public virtual void  converterUpdate(int updateID, int param1, int param2)
			{
				if (isDetail(VERBOSE_DETAIL))
				{
					switch (updateID)
					{
						
						case (int)javazoom.jl.converter.Converter.ProgressListener_FieldsEnum.UPDATE_CONVERT_COMPLETE: 
							if (param2 == 0)
								param2 = 1;
							
							pw.WriteLine();
							pw.WriteLine("Converted " + param2 + " frames in " + param1 + " ms (" + (param1 / param2) + " ms per frame.)");
							break;
						}
				}
			}
			
			public virtual void  parsedFrame(int frameNo, Header header)
			{
				if ((frameNo == 0) && isDetail(VERBOSE_DETAIL))
				{
					//UPGRADE_TODO: The equivalent in .NET for method 'java.Object.toString' may return a different value. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1043"'
					System.String headerString = header.ToString();
					pw.WriteLine("File is a " + headerString);
				}
				else if (isDetail(MAX_DETAIL))
				{
					//UPGRADE_TODO: The equivalent in .NET for method 'java.Object.toString' may return a different value. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1043"'
					System.String headerString = header.ToString();
					pw.WriteLine("Prased frame " + frameNo + ": " + headerString);
				}
			}
			
			public virtual void  readFrame(int frameNo, Header header)
			{
				if ((frameNo == 0) && isDetail(VERBOSE_DETAIL))
				{
					//UPGRADE_TODO: The equivalent in .NET for method 'java.Object.toString' may return a different value. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1043"'
					System.String headerString = header.ToString();
					pw.WriteLine("File is a " + headerString);
				}
				else if (isDetail(MAX_DETAIL))
				{
					//UPGRADE_TODO: The equivalent in .NET for method 'java.Object.toString' may return a different value. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1043"'
					System.String headerString = header.ToString();
					pw.WriteLine("Read frame " + frameNo + ": " + headerString);
				}
			}
			
			public virtual void  decodedFrame(int frameNo, Header header, Obuffer o)
			{
				if (isDetail(MAX_DETAIL))
				{
					//UPGRADE_TODO: The equivalent in .NET for method 'java.Object.toString' may return a different value. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1043"'
					System.String headerString = header.ToString();
					pw.WriteLine("Decoded frame " + frameNo + ": " + headerString);
					pw.WriteLine("Output: " + o);
				}
				else if (isDetail(VERBOSE_DETAIL))
				{
					if (frameNo == 0)
					{
						pw.Write("Converting.");
						pw.Flush();
					}
					
					if ((frameNo % 10) == 0)
					{
						pw.Write('.');
						pw.Flush();
					}
				}
			}
			
			//UPGRADE_NOTE: Exception 'java.lang.Throwable' was converted to 'System.Exception' which has different behavior. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1100"'
			public virtual bool converterException(System.Exception t)
			{
				if (this.detailLevel > NO_DETAIL)
				{
					SupportClass.WriteStackTrace(t, pw);
					pw.Flush();
				}
				return false;
			}
		}
	}
}