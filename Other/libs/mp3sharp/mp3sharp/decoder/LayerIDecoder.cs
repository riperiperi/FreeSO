/*
* 12/12/99		Initial version. Adapted from javalayer.java
*				and Subband*.java. mdm@techie.com
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
	
	/// <summary> Implements decoding of MPEG Audio Layer I frames. 
	/// </summary>
	
	class LayerIDecoder : FrameDecoder
	{
		protected internal Bitstream stream;
		protected internal Header header;
		protected internal SynthesisFilter filter1, filter2;
		protected internal Obuffer buffer;
		protected internal int which_channels;
		protected internal int mode;
		
		protected internal int num_subbands;
		protected internal Subband[] subbands;
		protected internal Crc16 crc = null;
		// new Crc16[1] to enable CRC checking.
		
		public LayerIDecoder()
		{
			crc = new Crc16();
		}
		
		public virtual void  create(Bitstream stream0, Header header0, SynthesisFilter filtera, SynthesisFilter filterb, Obuffer buffer0, int which_ch0)
		{
			stream = stream0;
			header = header0;
			filter1 = filtera;
			filter2 = filterb;
			buffer = buffer0;
			which_channels = which_ch0;
		}
		
		
		
		public virtual void  decodeFrame()
		{
			
			num_subbands = header.number_of_subbands();
			subbands = new Subband[32];
			mode = header.mode();
			
			createSubbands();
			
			readAllocation();
			readScaleFactorSelection();
			
			if ((crc != null) || header.checksum_ok())
			{
				readScaleFactors();
				
				readSampleData();
			}
		}
		
		protected internal virtual void  createSubbands()
		{
			int i;
			if (mode == Header.SINGLE_CHANNEL)
				for (i = 0; i < num_subbands; ++i)
					subbands[i] = new SubbandLayer1(i);
			else if (mode == Header.JOINT_STEREO)
			{
				for (i = 0; i < header.intensity_stereo_bound(); ++i)
					subbands[i] = new SubbandLayer1Stereo(i);
				for (; i < num_subbands; ++i)
					subbands[i] = new SubbandLayer1IntensityStereo(i);
			}
			else
			{
				for (i = 0; i < num_subbands; ++i)
					subbands[i] = new SubbandLayer1Stereo(i);
			}
		}
		
		protected internal virtual void  readAllocation()
		{
			// start to read audio data:
			for (int i = 0; i < num_subbands; ++i)
				subbands[i].read_allocation(stream, header, crc);
		}
		
		protected internal virtual void  readScaleFactorSelection()
		{
			// scale factor selection not present for layer I. 
		}
		
		protected internal virtual void  readScaleFactors()
		{
			for (int i = 0; i < num_subbands; ++i)
				subbands[i].read_scalefactor(stream, header);
		}
		
		protected internal virtual void  readSampleData()
		{
			bool read_ready = false;
			bool write_ready = false;
			int mode = header.mode();
			int i;
			do 
			{
				for (i = 0; i < num_subbands; ++i)
					read_ready = subbands[i].read_sampledata(stream);
				do 
				{
					for (i = 0; i < num_subbands; ++i)
						write_ready = subbands[i].put_next_sample(which_channels, filter1, filter2);
					
					filter1.calculate_pcm_samples(buffer);
					if ((which_channels == OutputChannels.BOTH_CHANNELS) && (mode != Header.SINGLE_CHANNEL))
						filter2.calculate_pcm_samples(buffer);
				}
				while (!write_ready);
			}
			while (!read_ready);
		}
		
		/// <summary> Abstract base class for subband classes of layer I and II
		/// </summary>
		public abstract class Subband
		{
			/*
			*  Changes from version 1.1 to 1.2:
			*    - array size increased by one, although a scalefactor with index 63
			*      is illegal (to prevent segmentation faults)
			*/
			// Scalefactors for layer I and II, Annex 3-B.1 in ISO/IEC DIS 11172:
			public static readonly float[] scalefactors = new float[]{2.00000000000000f, 1.58740105196820f, 1.25992104989487f, 1.00000000000000f, 0.79370052598410f, 0.62996052494744f, 0.50000000000000f, 0.39685026299205f, 0.31498026247372f, 0.25000000000000f, 0.19842513149602f, 0.15749013123686f, 0.12500000000000f, 0.09921256574801f, 0.07874506561843f, 0.06250000000000f, 0.04960628287401f, 0.03937253280921f, 0.03125000000000f, 0.02480314143700f, 0.01968626640461f, 0.01562500000000f, 0.01240157071850f, 0.00984313320230f, 0.00781250000000f, 0.00620078535925f, 0.00492156660115f, 0.00390625000000f, 0.00310039267963f, 0.00246078330058f, 0.00195312500000f, 0.00155019633981f, 0.00123039165029f, 0.00097656250000f, 0.00077509816991f, 0.00061519582514f, 0.00048828125000f, 0.00038754908495f, 0.00030759791257f, 0.00024414062500f, 0.00019377454248f, 0.00015379895629f, 0.00012207031250f, 0.00009688727124f, 0.00007689947814f, 0.00006103515625f, 0.00004844363562f, 0.00003844973907f, 0.00003051757813f, 0.00002422181781f, 0.00001922486954f, 0.00001525878906f, 0.00001211090890f, 0.00000961243477f, 0.00000762939453f, 0.00000605545445f, 0.00000480621738f, 0.00000381469727f, 0.00000302772723f, 0.00000240310869f, 0.00000190734863f, 0.00000151386361f, 0.00000120155435f, 0.00000000000000f};
			
			public abstract void  read_allocation(Bitstream stream, Header header, Crc16 crc);
			public abstract void  read_scalefactor(Bitstream stream, Header header);
			public abstract bool read_sampledata(Bitstream stream);
			public abstract bool put_next_sample(int channels, SynthesisFilter filter1, SynthesisFilter filter2);
		}
		
		
		/// <summary> Class for layer I subbands in single channel mode.
		/// Used for single channel mode
		/// and in derived class for intensity stereo mode
		/// </summary>
		internal class SubbandLayer1:Subband
		{
			
			// Factors and offsets for sample requantization
			public static readonly float[] table_factor = new float[]{0.0f, (1.0f / 2.0f) * (4.0f / 3.0f), (1.0f / 4.0f) * (8.0f / 7.0f), (1.0f / 8.0f) * (16.0f / 15.0f), (1.0f / 16.0f) * (32.0f / 31.0f), (1.0f / 32.0f) * (64.0f / 63.0f), (1.0f / 64.0f) * (128.0f / 127.0f), (1.0f / 128.0f) * (256.0f / 255.0f), (1.0f / 256.0f) * (512.0f / 511.0f), (1.0f / 512.0f) * (1024.0f / 1023.0f), (1.0f / 1024.0f) * (2048.0f / 2047.0f), (1.0f / 2048.0f) * (4096.0f / 4095.0f), (1.0f / 4096.0f) * (8192.0f / 8191.0f), (1.0f / 8192.0f) * (16384.0f / 16383.0f), (1.0f / 16384.0f) * (32768.0f / 32767.0f)};
			
			public static readonly float[] table_offset = new float[]{0.0f, ((1.0f / 2.0f) - 1.0f) * (4.0f / 3.0f), ((1.0f / 4.0f) - 1.0f) * (8.0f / 7.0f), ((1.0f / 8.0f) - 1.0f) * (16.0f / 15.0f), ((1.0f / 16.0f) - 1.0f) * (32.0f / 31.0f), ((1.0f / 32.0f) - 1.0f) * (64.0f / 63.0f), ((1.0f / 64.0f) - 1.0f) * (128.0f / 127.0f), ((1.0f / 128.0f) - 1.0f) * (256.0f / 255.0f), ((1.0f / 256.0f) - 1.0f) * (512.0f / 511.0f), ((1.0f / 512.0f) - 1.0f) * (1024.0f / 1023.0f), ((1.0f / 1024.0f) - 1.0f) * (2048.0f / 2047.0f), ((1.0f / 2048.0f) - 1.0f) * (4096.0f / 4095.0f), ((1.0f / 4096.0f) - 1.0f) * (8192.0f / 8191.0f), ((1.0f / 8192.0f) - 1.0f) * (16384.0f / 16383.0f), ((1.0f / 16384.0f) - 1.0f) * (32768.0f / 32767.0f)};
			
			protected internal int subbandnumber;
			protected internal int samplenumber;
			protected internal int allocation;
			protected internal float scalefactor;
			protected internal int samplelength;
			protected internal float sample;
			protected internal float factor, offset;
			
			/// <summary> Construtor.
			/// </summary>
			public SubbandLayer1(int subbandnumber):base()
			{
				this.subbandnumber = subbandnumber;
				samplenumber = 0;
			}
			
			/// <summary>*
			/// </summary>
			public override void  read_allocation(Bitstream stream, Header header, Crc16 crc)
			{
				if ((allocation = stream.get_bits(4)) == 15)
				{
				}
				//	 cerr << "WARNING: stream contains an illegal allocation!\n";
				// MPEG-stream is corrupted!
				if (crc != null)
					crc.add_bits(allocation, 4);
				if (allocation != 0)
				{
					samplelength = allocation + 1;
					factor = table_factor[allocation];
					offset = table_offset[allocation];
				}
			}
			
			/// <summary>*
			/// </summary>
			public override void  read_scalefactor(Bitstream stream, Header header)
			{
				if (allocation != 0)
					scalefactor = scalefactors[stream.get_bits(6)];
			}
			
			/// <summary>*
			/// </summary>
			public override bool read_sampledata(Bitstream stream)
			{
				if (allocation != 0)
				{
					sample = (float) (stream.get_bits(samplelength));
				}
				if (++samplenumber == 12)
				{
					samplenumber = 0;
					return true;
				}
				return false;
			}
			
			/// <summary>*
			/// </summary>
			public override bool put_next_sample(int channels, SynthesisFilter filter1, SynthesisFilter filter2)
			{
				if ((allocation != 0) && (channels != OutputChannels.RIGHT_CHANNEL))
				{
					float scaled_sample = (sample * factor + offset) * scalefactor;
					filter1.input_sample(scaled_sample, subbandnumber);
				}
				return true;
			}
		}
		
		
		/// <summary> Class for layer I subbands in joint stereo mode.
		/// </summary>
		internal class SubbandLayer1IntensityStereo:SubbandLayer1
		{
			protected internal float channel2_scalefactor;
			
			/// <summary> Constructor
			/// </summary>
			public SubbandLayer1IntensityStereo(int subbandnumber):base(subbandnumber)
			{
			}
			
			/// <summary>*
			/// </summary>
			public override void  read_allocation(Bitstream stream, Header header, Crc16 crc)
			{
				base.read_allocation(stream, header, crc);
			}
			
			/// <summary>*
			/// </summary>
			public override void  read_scalefactor(Bitstream stream, Header header)
			{
				if (allocation != 0)
				{
					scalefactor = scalefactors[stream.get_bits(6)];
					channel2_scalefactor = scalefactors[stream.get_bits(6)];
				}
			}
			
			/// <summary>*
			/// </summary>
			public override bool read_sampledata(Bitstream stream)
			{
				return base.read_sampledata(stream);
			}
			
			/// <summary>*
			/// </summary>
			public override bool put_next_sample(int channels, SynthesisFilter filter1, SynthesisFilter filter2)
			{
				if (allocation != 0)
				{
					sample = sample * factor + offset; // requantization
					if (channels == OutputChannels.BOTH_CHANNELS)
					{
						float sample1 = sample * scalefactor, sample2 = sample * channel2_scalefactor;
						filter1.input_sample(sample1, subbandnumber);
						filter2.input_sample(sample2, subbandnumber);
					}
					else if (channels == OutputChannels.LEFT_CHANNEL)
					{
						float sample1 = sample * scalefactor;
						filter1.input_sample(sample1, subbandnumber);
					}
					else
					{
						float sample2 = sample * channel2_scalefactor;
						filter1.input_sample(sample2, subbandnumber);
					}
				}
				return true;
			}
		}
		
		
		/// <summary> Class for layer I subbands in stereo mode.
		/// </summary>
		internal class SubbandLayer1Stereo:SubbandLayer1
		{
			protected internal int channel2_allocation;
			protected internal float channel2_scalefactor;
			protected internal int channel2_samplelength;
			protected internal float channel2_sample;
			protected internal float channel2_factor, channel2_offset;
			
			
			/// <summary> Constructor
			/// </summary>
			public SubbandLayer1Stereo(int subbandnumber):base(subbandnumber)
			{
			}
			
			/// <summary>*
			/// </summary>
			public override void  read_allocation(Bitstream stream, Header header, Crc16 crc)
			{
				allocation = stream.get_bits(4);
				channel2_allocation = stream.get_bits(4);
				if (crc != null)
				{
					crc.add_bits(allocation, 4);
					crc.add_bits(channel2_allocation, 4);
				}
				if (allocation != 0)
				{
					samplelength = allocation + 1;
					factor = table_factor[allocation];
					offset = table_offset[allocation];
				}
				if (channel2_allocation != 0)
				{
					channel2_samplelength = channel2_allocation + 1;
					channel2_factor = table_factor[channel2_allocation];
					channel2_offset = table_offset[channel2_allocation];
				}
			}
			
			/// <summary>*
			/// </summary>
			public override void  read_scalefactor(Bitstream stream, Header header)
			{
				if (allocation != 0)
					scalefactor = scalefactors[stream.get_bits(6)];
				if (channel2_allocation != 0)
					channel2_scalefactor = scalefactors[stream.get_bits(6)];
			}
			
			/// <summary>*
			/// </summary>
			public override bool read_sampledata(Bitstream stream)
			{
				bool returnvalue = base.read_sampledata(stream);
				if (channel2_allocation != 0)
				{
					channel2_sample = (float) (stream.get_bits(channel2_samplelength));
				}
				return (returnvalue);
			}
			
			/// <summary>*
			/// </summary>
			public override bool put_next_sample(int channels, SynthesisFilter filter1, SynthesisFilter filter2)
			{
				base.put_next_sample(channels, filter1, filter2);
				if ((channel2_allocation != 0) && (channels != OutputChannels.LEFT_CHANNEL))
				{
					float sample2 = (channel2_sample * channel2_factor + channel2_offset) * channel2_scalefactor;
					if (channels == OutputChannels.BOTH_CHANNELS)
						filter2.input_sample(sample2, subbandnumber);
					else
						filter1.input_sample(sample2, subbandnumber);
				}
				return true;
			}
		}
		
	}
}