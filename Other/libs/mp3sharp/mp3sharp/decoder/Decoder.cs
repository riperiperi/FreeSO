/*
* 1/12/99		Initial version.	mdm@techie.com
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
namespace javazoom.jl.decoder
{
	using System;
	/// <summary> The <code>Decoder</code> class encapsulates the details of
	/// decoding an MPEG audio frame. 
	/// 
	/// </summary>
	/// <author> 	MDM	
	/// </author>
	/// <version>  0.0.7 12/12/99
	/// @since	0.0.5
	/// 
	/// </version>
	
	internal class Decoder : DecoderErrors
	{
		private void  InitBlock()
		{
			equalizer = new Equalizer();
		}
		static public Params DefaultParams
		{
			get
			{
				return (Params) DEFAULT_PARAMS.Clone();  // MemberwiseClone();
			}
			
		}
		virtual public Equalizer Equalizer
		{
			set
			{
				if (value == null)
					value = decoder.Equalizer.PASS_THRU_EQ;
				
				equalizer.FromEqualizer = value;
				
				float[] factors = equalizer.BandFactors;
				if (filter1 != null)
					filter1.EQ = factors;
				
				if (filter2 != null)
					filter2.EQ = factors;
			}
			
		}
		/// <summary> Changes the output buffer. This will take effect the next time
		/// decodeFrame() is called. 
		/// </summary>
		virtual public Obuffer OutputBuffer
		{
			set
			{
				output = value;
			}
			
		}
		/// <summary> Retrieves the sample frequency of the PCM samples output
		/// by this decoder. This typically corresponds to the sample
		/// rate encoded in the MPEG audio stream.
		/// 
		/// </summary>
		/// <param name="the">sample rate (in Hz) of the samples written to the
		/// output buffer when decoding. 
		/// 
		/// </param>
		virtual public int OutputFrequency
		{
			get
			{
				return outputFrequency;
			}
			
		}
		/// <summary> Retrieves the number of channels of PCM samples output by
		/// this decoder. This usually corresponds to the number of
		/// channels in the MPEG audio stream, although it may differ.
		/// 
		/// </summary>
		/// <returns> The number of output channels in the decoded samples: 1 
		/// for mono, or 2 for stereo.
		/// 
		/// 
		/// </returns>
		virtual public int OutputChannels
		{
			get
			{
				return outputChannels;
			}
			
		}
		/// <summary> Retrieves the maximum number of samples that will be written to
		/// the output buffer when one frame is decoded. This can be used to
		/// help calculate the size of other buffers whose size is based upon 
		/// the number of samples written to the output buffer. NB: this is
		/// an upper bound and fewer samples may actually be written, depending
		/// upon the sample rate and number of channels.
		/// 
		/// </summary>
		/// <returns> The maximum number of samples that are written to the 
		/// output buffer when decoding a single frame of MPEG audio.
		/// 
		/// </returns>
		virtual public int OutputBlockSize
		{
			get
			{
				return javazoom.jl.decoder.Obuffer.OBUFFERSIZE;
			}
			
		}
		//UPGRADE_NOTE: Final was removed from the declaration of 'DEFAULT_PARAMS '. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1003"'
		private static readonly Params DEFAULT_PARAMS = new Params();
		
		/// <summary> The Bistream from which the MPEG audio frames are read.
		/// </summary>
		//private Bitstream				stream;
		
		/// <summary> The Obuffer instance that will receive the decoded
		/// PCM samples.
		/// </summary>
		private Obuffer output;
		
		/// <summary> Synthesis filter for the left channel.
		/// </summary>
		private SynthesisFilter filter1;
		
		/// <summary> Sythesis filter for the right channel.
		/// </summary>
		private SynthesisFilter filter2;
		
		/// <summary> The decoder used to decode layer III frames.
		/// </summary>
		private LayerIIIDecoder l3decoder;
		private LayerIIDecoder l2decoder;
		private LayerIDecoder l1decoder;
		
		private int outputFrequency;
		private int outputChannels;
		
		//UPGRADE_NOTE: The initialization of  'equalizer' was moved to method 'InitBlock'. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1005"'
		private Equalizer equalizer;
		
		private Params params_Renamed;
		
		private bool initialized;
		
		
		/// <summary> Creates a new <code>Decoder</code> instance with default 
		/// parameters.
		/// </summary>
		
		public Decoder():this(null)
		{
			InitBlock();
		}
		
		/// <summary> Creates a new <code>Decoder</code> instance with default 
		/// parameters.
		/// 
		/// </summary>
		/// <param name="params	The"><code>Params</code> instance that describes
		/// the customizable aspects of the decoder.  
		/// 
		/// </param>
		public Decoder(Params params0)
		{
			InitBlock();
			if (params0 == null)
				params0 = DEFAULT_PARAMS;
			
			params_Renamed = params0;
			
			Equalizer eq = params_Renamed.InitialEqualizerSettings;
			if (eq != null)
			{
				equalizer.FromEqualizer = eq;
			}
		}
		
		
		
		/// <summary> Decodes one frame from an MPEG audio bitstream.
		/// 
		/// </summary>
		/// <param name="header		The">header describing the frame to decode.
		/// </param>
		/// <param name="bitstream		The">bistream that provides the bits for te body of the frame. 
		/// 
		/// </param>
		/// <returns> A SampleBuffer containing the decoded samples.
		/// 
		/// </returns>
		public virtual Obuffer decodeFrame(Header header, Bitstream stream)
		{
			if (!initialized)
			{
				initialize(header);
			}
			
			int layer = header.layer();
			
			output.clear_buffer();
			
			FrameDecoder decoder = retrieveDecoder(header, stream, layer);
			
			decoder.decodeFrame();
			
			output.write_buffer(1);
			
			return output;
		}
		
		
		
		
		
		
		protected internal virtual DecoderException newDecoderException(int errorcode)
		{
			return new DecoderException(errorcode, null);
		}
		
		//UPGRADE_NOTE: Exception 'java.lang.Throwable' was converted to 'System.Exception' which has different behavior. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1100"'
		protected internal virtual DecoderException newDecoderException(int errorcode, System.Exception throwable)
		{
			return new DecoderException(errorcode, throwable);
		}
		
		protected internal virtual FrameDecoder retrieveDecoder(Header header, Bitstream stream, int layer)
		{
			FrameDecoder decoder = null;
			
			// REVIEW: allow channel output selection type
			// (LEFT, RIGHT, BOTH, DOWNMIX)
			switch (layer)
			{
				
				case 3: 
					if (l3decoder == null)
					{
						l3decoder = new LayerIIIDecoder(stream, header, filter1, filter2, output, (int)OutputChannelsEnum.BOTH_CHANNELS);
					}
					
					decoder = l3decoder;
					break;
				
				case 2: 
					if (l2decoder == null)
					{
						l2decoder = new LayerIIDecoder();
						l2decoder.create(stream, header, filter1, filter2, output, (int)OutputChannelsEnum.BOTH_CHANNELS);
					}
					decoder = l2decoder;
					break;
				
				case 1: 
					if (l1decoder == null)
					{
						l1decoder = new LayerIDecoder();
						l1decoder.create(stream, header, filter1, filter2, output, (int)OutputChannelsEnum.BOTH_CHANNELS);
					}
					decoder = l1decoder;
					break;
				}
			
			if (decoder == null)
			{
				throw newDecoderException(javazoom.jl.decoder.DecoderErrors_Fields.UNSUPPORTED_LAYER, null);
			}
			
			return decoder;
		}
		
		private void  initialize(Header header)
		{
			
			// REVIEW: allow customizable scale factor
			float scalefactor = 32700.0f;
			
			int mode = header.mode();
			int layer = header.layer();
			int channels = mode == Header.SINGLE_CHANNEL?1:2;
			
			
			// set up output buffer if not set up by client.
			if (output == null)
				output = new SampleBuffer(header.frequency(), channels);
			
			float[] factors = equalizer.BandFactors;
			//Console.WriteLine("NOT CREATING SYNTHESIS FILTERS");
			filter1 = new SynthesisFilter(0, scalefactor, factors);
			
			// REVIEW: allow mono output for stereo
			if (channels == 2)
				filter2 = new SynthesisFilter(1, scalefactor, factors);
			
			outputChannels = channels;
			outputFrequency = header.frequency();
			
			initialized = true;
		}
		
		/// <summary> The <code>Params</code> class presents the customizable
		/// aspects of the decoder. 
		/// <p>
		/// Instances of this class are not thread safe. 
		/// </summary>
		internal class Params : System.ICloneable
		{
			private void  InitBlock()
			{
				outputChannels = OutputChannels.BOTH;
				equalizer = new Equalizer();
			}
			virtual public OutputChannels OutputChannels
			{
				get
				{
					return outputChannels;
				}
				
				set
				{
					if (value == null)
						throw new System.NullReferenceException("out");
					
					outputChannels = value;
				}
				
			}
			/// <summary> Retrieves the equalizer settings that the decoder's equalizer
			/// will be initialized from.
			/// <p>
			/// The <code>Equalizer</code> instance returned 
			/// cannot be changed in real time to affect the 
			/// decoder output as it is used only to initialize the decoders
			/// EQ settings. To affect the decoder's output in realtime,
			/// use the Equalizer returned from the getEqualizer() method on
			/// the decoder. 
			/// 
			/// </summary>
			/// <returns>	The <code>Equalizer</code> used to initialize the
			/// EQ settings of the decoder. 
			/// 
			/// </returns>
			virtual public Equalizer InitialEqualizerSettings
			{
				get
				{
					return equalizer;
				}
				
			}
			//UPGRADE_NOTE: The initialization of  'outputChannels' was moved to method 'InitBlock'. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1005"'
			private OutputChannels outputChannels;
			
			//UPGRADE_NOTE: The initialization of  'equalizer' was moved to method 'InitBlock'. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1005"'
			private Equalizer equalizer;
			
			public Params()
			{
			}
			
			//UPGRADE_TODO: The equivalent of method 'java.lang.Object.clone' is not an override method. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1143"'
			public System.Object Clone()
			{
				//UPGRADE_NOTE: Exception 'java.lang.CloneNotSupportedException' was converted to 'System.Exception' which has different behavior. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1100"'
				try
				{
					return base.MemberwiseClone();
				}
				catch (System.Exception ex)
				{
					throw new System.ApplicationException(this + ": " + ex);
				}
			}
			
			
			
		}
		
	}
}