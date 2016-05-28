using Support;
/*
* 02/13/99 : Java Conversion by E.B , ebsp@iname.com
*
*---------------------------------------------------------------------------
* Declarations for MPEG header class
* A few layer III, MPEG-2 LSF, and seeking modifications made by Jeff Tsay.
* Last modified : 04/19/97
*
*  @(#) header.h 1.7, last edit: 6/15/94 16:55:33
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
*--------------------------------------------------------------------------
*/
namespace javazoom.jl.decoder
{
	using System;
	
	/// <summary> Class for extracting information from a frame header.
	/// *
	/// *
	/// </summary>
	// TODO: move strings into resources
	
	internal class Header
	{
		private void  InitBlock()
		{
			syncmode = Bitstream.INITIAL_SYNC;
		}
		/// <summary> Returns synchronized header.
		/// </summary>
		public virtual int SyncHeader
		{
			// E.B
			
			get
			{
				return _headerstring;
			}
			
		}
		public static readonly int[][] frequencies = {new int[]{22050, 24000, 16000, 1}, new int[]{44100, 48000, 32000, 1}, new int[]{11025, 12000, 8000, 1}}; // SZD: MPEG25
		
		/// <summary> Constant for MPEG-2 LSF version
		/// </summary>
		public const int MPEG2_LSF = 0;
		public const int MPEG25_LSF = 2; // SZD
		
		/// <summary> Constant for MPEG-1 version
		/// </summary>
		public const int MPEG1 = 1;
		
		public const int STEREO = 0;
		public const int JOINT_STEREO = 1;
		public const int DUAL_CHANNEL = 2;
		public const int SINGLE_CHANNEL = 3;
		public const int FOURTYFOUR_POINT_ONE = 0;
		public const int FOURTYEIGHT = 1;
		public const int THIRTYTWO = 2;
		
		private int h_layer, h_protection_bit, h_bitrate_index, h_padding_bit, h_mode_extension;
		private int h_version;
		private int h_mode;
		private int h_sample_frequency;
		private int h_number_of_subbands, h_intensity_stereo_bound;
		private bool h_copyright, h_original;
		//UPGRADE_NOTE: The initialization of  'syncmode' was moved to method 'InitBlock'. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1005"'
		private sbyte syncmode;
		private Crc16 crc;
		
		public short checksum;
		public int framesize;
		public int nSlots;
		
		private int _headerstring = - 1;
		// E.B
		
		internal Header()
		{
			InitBlock();
		}
		public override System.String ToString()
		{
			System.Text.StringBuilder buffer = new System.Text.StringBuilder(200);
			buffer.Append("Layer ");
			buffer.Append(layer_string());
			buffer.Append(" frame ");
			buffer.Append(mode_string());
			buffer.Append(' ');
			buffer.Append(version_string());
			if (!checksums())
				buffer.Append(" no");
			buffer.Append(" checksums");
			buffer.Append(' ');
			buffer.Append(sample_frequency_string());
			buffer.Append(',');
			buffer.Append(' ');
			buffer.Append(bitrate_string());
			
			System.String s = buffer.ToString();
			return s;
		}
		
		/// <summary> Read a 32-bit header from the bitstream.
		/// </summary>
		internal void  read_header(Bitstream stream, Crc16[] crcp)
		{
			int headerstring;
			int channel_bitrate;
			
			bool sync = false;
			
			do 
			{
				
				headerstring = stream.syncHeader(syncmode);
				_headerstring = headerstring; // E.B
				
				if (syncmode == Bitstream.INITIAL_SYNC)
				{
					h_version = ((SupportClass.URShift(headerstring, 19)) & 1);
					if (((SupportClass.URShift(headerstring, 20)) & 1) == 0)
					// SZD: MPEG2.5 detection
						if (h_version == MPEG2_LSF)
							h_version = MPEG25_LSF;
						else
							throw stream.newBitstreamException(javazoom.jl.decoder.BitstreamErrors_Fields.UNKNOWN_ERROR);
					
					
					if ((h_sample_frequency = ((SupportClass.URShift(headerstring, 10)) & 3)) == 3)
					{
						throw stream.newBitstreamException(javazoom.jl.decoder.BitstreamErrors_Fields.UNKNOWN_ERROR);
					}
				}
				
				h_layer = 4 - (SupportClass.URShift(headerstring, 17)) & 3;
				h_protection_bit = (SupportClass.URShift(headerstring, 16)) & 1;
				h_bitrate_index = (SupportClass.URShift(headerstring, 12)) & 0xF;
				h_padding_bit = (SupportClass.URShift(headerstring, 9)) & 1;
				h_mode = ((SupportClass.URShift(headerstring, 6)) & 3);
				h_mode_extension = (SupportClass.URShift(headerstring, 4)) & 3;
				if (h_mode == JOINT_STEREO)
					h_intensity_stereo_bound = (h_mode_extension << 2) + 4;
				else
					h_intensity_stereo_bound = 0;
				// should never be used
				if (((SupportClass.URShift(headerstring, 3)) & 1) == 1)
					h_copyright = true;
				if (((SupportClass.URShift(headerstring, 2)) & 1) == 1)
					h_original = true;
				
				
				// calculate number of subbands:
				if (h_layer == 1)
					h_number_of_subbands = 32;
				else
				{
					channel_bitrate = h_bitrate_index;
					// calculate bitrate per channel:
					if (h_mode != SINGLE_CHANNEL)
						if (channel_bitrate == 4)
							channel_bitrate = 1;
						else
							channel_bitrate -= 4;
					
					if ((channel_bitrate == 1) || (channel_bitrate == 2))
						if (h_sample_frequency == THIRTYTWO)
							h_number_of_subbands = 12;
						else
							h_number_of_subbands = 8;
					else if ((h_sample_frequency == FOURTYEIGHT) || ((channel_bitrate >= 3) && (channel_bitrate <= 5)))
						h_number_of_subbands = 27;
					else
						h_number_of_subbands = 30;
				}
				if (h_intensity_stereo_bound > h_number_of_subbands)
					h_intensity_stereo_bound = h_number_of_subbands;
				// calculate framesize and nSlots
				calculate_framesize();
				
				// read framedata:
				stream.read_frame_data(framesize);
				
				if (stream.isSyncCurrentPosition(syncmode))
				{
					if (syncmode == Bitstream.INITIAL_SYNC)
					{
						syncmode = Bitstream.STRICT_SYNC;
						stream.set_syncword(headerstring & unchecked((int)0xFFF80CC0));
					}
					sync = true;
				}
				else
				{
					stream.unreadFrame();
				}
			}
			while (!sync);
			
			stream.parse_frame();
			
			if (h_protection_bit == 0)
			{
				// frame contains a crc checksum
				checksum = (short) stream.get_bits(16);
				if (crc == null)
					crc = new Crc16();
				crc.add_bits(headerstring, 16);
				crcp[0] = crc;
			}
			else
				crcp[0] = null;
			if (h_sample_frequency == FOURTYFOUR_POINT_ONE)
			{
				/*
				if (offset == null)
				{
				int max = max_number_of_frames(stream);
				offset = new int[max];
				for(int i=0; i<max; i++) offset[i] = 0;
				}
				// Bizarre, y avait ici une acollade ouvrante
				int cf = stream.current_frame();
				int lf = stream.last_frame();
				if ((cf > 0) && (cf == lf))
				{
				offset[cf] = offset[cf-1] + h_padding_bit;
				}
				else
				{
				offset[0] = h_padding_bit;
				}
				*/
			}
		}
		
		// Functions to query header contents:
		/// <summary> Returns version.
		/// </summary>
		public int version()
		{
			return h_version;
		}
		
		/// <summary> Returns Layer ID.
		/// </summary>
		public int layer()
		{
			return h_layer;
		}
		
		/// <summary> Returns bitrate index.
		/// </summary>
		public int bitrate_index()
		{
			return h_bitrate_index;
		}
		
		/// <summary> Returns Sample Frequency.
		/// </summary>
		public int sample_frequency()
		{
			return h_sample_frequency;
		}
		
		/// <summary> Returns Frequency.
		/// </summary>
		public int frequency()
		{
			return frequencies[h_version][h_sample_frequency];
		}
		
		/// <summary> Returns Mode.
		/// </summary>
		public int mode()
		{
			return h_mode;
		}
		
		/// <summary> Returns Protection bit.
		/// </summary>
		public bool checksums()
		{
			if (h_protection_bit == 0)
				return true;
			else
				return false;
		}
		
		/// <summary> Returns Copyright.
		/// </summary>
		public bool copyright()
		{
			return h_copyright;
		}
		
		/// <summary> Returns Original.
		/// </summary>
		public bool original()
		{
			return h_original;
		}
		
		/// <summary> Returns Checksum flag.
		/// Compares computed checksum with stream checksum.
		/// </summary>
		public bool checksum_ok()
		{
			return (checksum == crc.checksum());
		}
		
		// Seeking and layer III stuff
		/// <summary> Returns Layer III Padding bit.
		/// </summary>
		public bool padding()
		{
			if (h_padding_bit == 0)
				return false;
			else
				return true;
		}
		
		/// <summary> Returns Slots.
		/// </summary>
		public int slots()
		{
			return nSlots;
		}
		
		/// <summary> Returns Mode Extension.
		/// </summary>
		public int mode_extension()
		{
			return h_mode_extension;
		}
		
		// E.B -> private to public
		public static readonly int[][][] bitrates = {new int[][]{new int[]{0, 32000, 48000, 56000, 64000, 80000, 96000, 112000, 128000, 144000, 160000, 176000, 192000, 224000, 256000, 0}, new int[]{0, 8000, 16000, 24000, 32000, 40000, 48000, 56000, 64000, 80000, 96000, 112000, 128000, 144000, 160000, 0}, new int[]{0, 8000, 16000, 24000, 32000, 40000, 48000, 56000, 64000, 80000, 96000, 112000, 128000, 144000, 160000, 0}}, new int[][]{new int[]{0, 32000, 64000, 96000, 128000, 160000, 192000, 224000, 256000, 288000, 320000, 352000, 384000, 416000, 448000, 0}, new int[]{0, 32000, 48000, 56000, 64000, 80000, 96000, 112000, 128000, 160000, 192000, 224000, 256000, 320000, 384000, 0}, new int[]{0, 32000, 40000, 48000, 56000, 64000, 80000, 96000, 112000, 128000, 160000, 192000, 224000, 256000, 320000, 0}}, new int[][]{new int[]{0, 32000, 48000, 56000, 64000, 80000, 96000, 112000, 128000, 144000, 160000, 176000, 192000, 224000, 256000, 0}, new int[]{0, 8000, 16000, 24000, 32000, 40000, 48000, 56000, 64000, 80000, 96000, 112000, 128000, 144000, 160000, 0}, new int[]{0, 8000, 16000, 24000, 32000, 40000, 48000, 56000, 64000, 80000, 96000, 112000, 128000, 144000, 160000, 0}}};
		
		// E.B -> private to public
		/// <summary> Calculate Frame size.
		/// Calculates framesize in bytes excluding header size.
		/// </summary>
		public int calculate_framesize()
		{
			
			if (h_layer == 1)
			{
				framesize = (12 * bitrates[h_version][0][h_bitrate_index]) / frequencies[h_version][h_sample_frequency];
				if (h_padding_bit != 0)
					framesize++;
				framesize <<= 2; // one slot is 4 bytes long
				nSlots = 0;
			}
			else
			{
				framesize = (144 * bitrates[h_version][h_layer - 1][h_bitrate_index]) / frequencies[h_version][h_sample_frequency];
				if (h_version == MPEG2_LSF || h_version == MPEG25_LSF)
					framesize >>= 1;
				// SZD
				if (h_padding_bit != 0)
					framesize++;
				// Layer III slots
				if (h_layer == 3)
				{
					if (h_version == MPEG1)
					{
						nSlots = framesize - ((h_mode == SINGLE_CHANNEL)?17:32) - ((h_protection_bit != 0)?0:2) - 4; // header size
					}
					else
					{
						// MPEG-2 LSF, SZD: MPEG-2.5 LSF
						nSlots = framesize - ((h_mode == SINGLE_CHANNEL)?9:17) - ((h_protection_bit != 0)?0:2) - 4; // header size
					}
				}
				else
				{
					nSlots = 0;
				}
			}
			framesize -= 4; // subtract header size
			return framesize;
		}
		
		/// <summary> Returns the maximum number of frames in the stream.
		/// </summary>
		public int max_number_of_frames(int streamsize)
		// E.B
		{
			if ((framesize + 4 - h_padding_bit) == 0)
				return 0;
			else
				return (streamsize / (framesize + 4 - h_padding_bit));
		}
		
		/// <summary> Returns the maximum number of frames in the stream.
		/// </summary>
		public int min_number_of_frames(int streamsize)
		// E.B
		{
			if ((framesize + 5 - h_padding_bit) == 0)
				return 0;
			else
				return (streamsize / (framesize + 5 - h_padding_bit));
		}
		
		
		/// <summary> Returns ms/frame.
		/// </summary>
		public float ms_per_frame()
		// E.B
		{
			float[][] ms_per_frame_array = {new float[]{8.707483f, 8.0f, 12.0f}, new float[]{26.12245f, 24.0f, 36.0f}, new float[]{26.12245f, 24.0f, 36.0f}};
			return (ms_per_frame_array[h_layer - 1][h_sample_frequency]);
		}
		
		/// <summary> Returns total ms.
		/// </summary>
		public float total_ms(int streamsize)
		// E.B
		{
			return (max_number_of_frames(streamsize) * ms_per_frame());
		}
		
		
		// functions which return header informations as strings:
		/// <summary> Return Layer version.
		/// </summary>
		public System.String layer_string()
		{
			switch (h_layer)
			{
				
				case 1: 
					return "I";
				
				case 2: 
					return "II";
				
				case 3: 
					return "III";
				}
			return null;
		}
		
		// E.B -> private to public
		public static readonly System.String[][][] bitrate_str = {new System.String[][]{new System.String[]{"free format", "32 kbit/s", "48 kbit/s", "56 kbit/s", "64 kbit/s", "80 kbit/s", "96 kbit/s", "112 kbit/s", "128 kbit/s", "144 kbit/s", "160 kbit/s", "176 kbit/s", "192 kbit/s", "224 kbit/s", "256 kbit/s", "forbidden"}, new System.String[]{"free format", "8 kbit/s", "16 kbit/s", "24 kbit/s", "32 kbit/s", "40 kbit/s", "48 kbit/s", "56 kbit/s", "64 kbit/s", "80 kbit/s", "96 kbit/s", "112 kbit/s", "128 kbit/s", "144 kbit/s", "160 kbit/s", "forbidden"}, new System.String[]{"free format", "8 kbit/s", "16 kbit/s", "24 kbit/s", "32 kbit/s", "40 kbit/s", "48 kbit/s", "56 kbit/s", "64 kbit/s", "80 kbit/s", "96 kbit/s", "112 kbit/s", "128 kbit/s", "144 kbit/s", "160 kbit/s", "forbidden"}}, new System.String[][]{new System.String[]{"free format", "32 kbit/s", "64 kbit/s", "96 kbit/s", "128 kbit/s", "160 kbit/s", "192 kbit/s", "224 kbit/s", "256 kbit/s", "288 kbit/s", "320 kbit/s", "352 kbit/s", "384 kbit/s", "416 kbit/s", "448 kbit/s", "forbidden"}, new System.String[]{"free format", "32 kbit/s", "48 kbit/s", "56 kbit/s", "64 kbit/s", "80 kbit/s", "96 kbit/s", "112 kbit/s", "128 kbit/s", "160 kbit/s", "192 kbit/s", "224 kbit/s", "256 kbit/s", "320 kbit/s", "384 kbit/s", "forbidden"}, new System.String[]{"free format", "32 kbit/s", "40 kbit/s", "48 kbit/s", "56 kbit/s", "64 kbit/s", "80 kbit/s", "96 kbit/s", "112 kbit/s", "128 kbit/s", "160 kbit/s", "192 kbit/s", "224 kbit/s", "256 kbit/s", "320 kbit/s", "forbidden"}}, new System.String[][]{new System.String[]{"free format", "32 kbit/s", "48 kbit/s", "56 kbit/s", "64 kbit/s", "80 kbit/s", "96 kbit/s", "112 kbit/s", "128 kbit/s", "144 kbit/s", "160 kbit/s", "176 kbit/s", "192 kbit/s", "224 kbit/s", "256 kbit/s", "forbidden"}, new System.String[]{"free format", "8 kbit/s", "16 kbit/s", "24 kbit/s", "32 kbit/s", "40 kbit/s", "48 kbit/s", "56 kbit/s", "64 kbit/s", "80 kbit/s", "96 kbit/s", "112 kbit/s", "128 kbit/s", "144 kbit/s", "160 kbit/s", "forbidden"}, new System.
			String[]{"free format", "8 kbit/s", "16 kbit/s", "24 kbit/s", "32 kbit/s", "40 kbit/s", "48 kbit/s", "56 kbit/s", "64 kbit/s", "80 kbit/s", "96 kbit/s", "112 kbit/s", "128 kbit/s", "144 kbit/s", "160 kbit/s", "forbidden"}}};
		
		/// <summary> Returns Bitrate.
		/// </summary>
		public System.String bitrate_string()
		{
			return bitrate_str[h_version][h_layer - 1][h_bitrate_index];
		}
		
		/// <summary> Returns Frequency
		/// </summary>
		public System.String sample_frequency_string()
		{
			switch (h_sample_frequency)
			{
				
				case THIRTYTWO: 
					if (h_version == MPEG1)
						return "32 kHz";
					else if (h_version == MPEG2_LSF)
						return "16 kHz";
					// SZD
					else
						return "8 kHz";
					goto case FOURTYFOUR_POINT_ONE;
				
				case FOURTYFOUR_POINT_ONE: 
					if (h_version == MPEG1)
						return "44.1 kHz";
					else if (h_version == MPEG2_LSF)
						return "22.05 kHz";
					// SZD
					else
						return "11.025 kHz";
					goto case FOURTYEIGHT;
				
				case FOURTYEIGHT: 
					if (h_version == MPEG1)
						return "48 kHz";
					else if (h_version == MPEG2_LSF)
						return "24 kHz";
					// SZD
					else
						return "12 kHz";
					break;
				}
			return (null);
		}
		
		/// <summary> Returns Mode.
		/// </summary>
		public System.String mode_string()
		{
			switch (h_mode)
			{
				
				case STEREO: 
					return "Stereo";
				
				case JOINT_STEREO: 
					return "Joint stereo";
				
				case DUAL_CHANNEL: 
					return "Dual channel";
				
				case SINGLE_CHANNEL: 
					return "Single channel";
				}
			return null;
		}
		
		/// <summary> Returns Version.
		/// </summary>
		public System.String version_string()
		{
			switch (h_version)
			{
				
				case MPEG1: 
					return "MPEG-1";
				
				case MPEG2_LSF: 
					return "MPEG-2 LSF";
				
				case MPEG25_LSF: 
					return "MPEG-2.5 LSF";
				}
			return (null);
		}
		
		/// <summary> Returns the number of subbands in the current frame.
		/// </summary>
		public int number_of_subbands()
		{
			return h_number_of_subbands;
		}
		
		/// <summary> Returns Intensity Stereo.
		/// Layer II joint stereo only).
		/// Returns the number of subbands which are in stereo mode,
		/// subbands above that limit are in intensity stereo mode.
		/// </summary>
		public int intensity_stereo_bound()
		{
			return h_intensity_stereo_bound;
		}
	}
}