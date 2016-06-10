using Support;
/*
* 02/23/99 JavaConversion by E.B, JavaLayer
*/
/*===========================================================================

riff.h  -  Don Cross, April 1993.

RIFF file format classes.
See Chapter 8 of "Multimedia Programmer's Reference" in
the Microsoft Windows SDK.

See also:
..\source\riff.cpp
ddc.h

===========================================================================*/
namespace javazoom.jl.converter
{
	using System;
	
	/// <summary> Class allowing WaveFormat Access
	/// </summary>
	internal class WaveFile:RiffFile
	{
		public const int MAX_WAVE_CHANNELS = 2;
		
		//UPGRADE_NOTE: Field 'EnclosingInstance' was added to class 'WaveFormat_ChunkData' to access its enclosing instance. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1019"'
		internal class WaveFormat_ChunkData
		{
			private void  InitBlock(WaveFile enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private WaveFile enclosingInstance;
			public WaveFile Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			public short wFormatTag = 0; // Format category (PCM=1)
			public short nChannels = 0; // Number of channels (mono=1, stereo=2)
			public int nSamplesPerSec = 0; // Sampling rate [Hz]
			public int nAvgBytesPerSec = 0;
			public short nBlockAlign = 0;
			public short nBitsPerSample = 0;
			
			public WaveFormat_ChunkData(WaveFile enclosingInstance)
			{
				InitBlock(enclosingInstance);
				wFormatTag = 1; // PCM
				Config(44100, (short) 16, (short) 1);
			}
			
			public virtual void  Config(int NewSamplingRate, short NewBitsPerSample, short NewNumChannels)
			{
				nSamplesPerSec = NewSamplingRate;
				nChannels = NewNumChannels;
				nBitsPerSample = NewBitsPerSample;
				nAvgBytesPerSec = (nChannels * nSamplesPerSec * nBitsPerSample) / 8;
				nBlockAlign = (short) ((nChannels * nBitsPerSample) / 8);
			}
		}
		
		
		//UPGRADE_NOTE: Field 'EnclosingInstance' was added to class 'WaveFormat_Chunk' to access its enclosing instance. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1019"'
		internal class WaveFormat_Chunk
		{
			private void  InitBlock(WaveFile enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private WaveFile enclosingInstance;
			public WaveFile Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			public RiffChunkHeader header;
			public WaveFormat_ChunkData data;
			
			public WaveFormat_Chunk(WaveFile enclosingInstance)
			{
				InitBlock(enclosingInstance);
				header = new RiffChunkHeader(enclosingInstance);
				data = new WaveFormat_ChunkData(enclosingInstance);
				header.ckID = javazoom.jl.converter.RiffFile.FourCC("fmt ");
				header.ckSize = 16;
			}
			
			public virtual int VerifyValidity()
			{
				bool ret = header.ckID == javazoom.jl.converter.RiffFile.FourCC("fmt ") && (data.nChannels == 1 || data.nChannels == 2) && data.nAvgBytesPerSec == (data.nChannels * data.nSamplesPerSec * data.nBitsPerSample) / 8 && data.nBlockAlign == (data.nChannels * data.nBitsPerSample) / 8;
				if (ret == true)
					return 1;
				else
					return 0;
			}
		}
		
		//UPGRADE_NOTE: Field 'EnclosingInstance' was added to class 'WaveFileSample' to access its enclosing instance. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1019"'
		internal class WaveFileSample
		{
			private void  InitBlock(WaveFile enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private WaveFile enclosingInstance;
			public WaveFile Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			public short[] chan;
			
			public WaveFileSample(WaveFile enclosingInstance)
			{
				InitBlock(enclosingInstance);
				chan = new short[WaveFile.MAX_WAVE_CHANNELS];
			}
		}
		
		private WaveFormat_Chunk wave_format;
		private RiffChunkHeader pcm_data;
		private long pcm_data_offset = 0; // offset of 'pcm_data' in output file
		private int num_samples = 0;
		
		
		/// <summary> Constructs a new WaveFile instance. 
		/// </summary>
		public WaveFile()
		{
			pcm_data = new RiffChunkHeader(this);
			wave_format = new WaveFormat_Chunk(this);
			pcm_data.ckID = FourCC("data");
			pcm_data.ckSize = 0;
			num_samples = 0;
		}
		
		/// <summary>*
		/// *
		/// public int OpenForRead (String Filename)
		/// {
		/// // Verify filename parameter as best we can...
		/// if (Filename == null)
		/// {
		/// return DDC_INVALID_CALL;
		/// }
		/// int retcode = Open ( Filename, RFM_READ );
		/// </summary>
		/// <summary>if ( retcode == DDC_SUCCESS )
		/// {
		/// retcode = Expect ( "WAVE", 4 );
		/// </summary>
		/// <summary>if ( retcode == DDC_SUCCESS )
		/// {
		/// retcode = Read(wave_format,24);
		/// </summary>
		/// <summary>if ( retcode == DDC_SUCCESS && !wave_format.VerifyValidity() )
		/// {
		/// // This isn't standard PCM, so we don't know what it is!
		/// retcode = DDC_FILE_ERROR;
		/// }
		/// </summary>
		/// <summary>if ( retcode == DDC_SUCCESS )
		/// {
		/// pcm_data_offset = CurrentFilePosition();
		/// </summary>
		/// <summary>// Figure out number of samples from
		/// // file size, current file position, and
		/// // WAVE header.
		/// retcode = Read (pcm_data, 8 );
		/// num_samples = filelength(fileno(file)) - CurrentFilePosition();
		/// num_samples /= NumChannels();
		/// num_samples /= (BitsPerSample() / 8);
		/// }
		/// }
		/// }
		/// return retcode;
		/// }
		/// </summary>
		

		/// <summary>
		/// Pass in either a FileName or a Stream.
		/// </summary>
		public virtual int OpenForWrite(System.String Filename, System.IO.Stream stream, int SamplingRate, short BitsPerSample, short NumChannels)
		{
			// Verify parameters...
			if ((BitsPerSample != 8 && BitsPerSample != 16) || NumChannels < 1 || NumChannels > 2)
			{
				return DDC_INVALID_CALL;
			}
			
			wave_format.data.Config(SamplingRate, BitsPerSample, NumChannels);
			
			int retcode = 0;
			if (stream != null)
				Open(stream, RFM_WRITE);
			else
				Open(Filename, RFM_WRITE);
			
			if (retcode == DDC_SUCCESS)
			{
				sbyte[] theWave = new sbyte[]{(sbyte) SupportClass.Identity('W'), (sbyte) SupportClass.Identity('A'), (sbyte) SupportClass.Identity('V'), (sbyte) SupportClass.Identity('E')};
				retcode = Write(theWave, 4);
				
				if (retcode == DDC_SUCCESS)
				{
					// Ecriture de wave_format
					retcode = Write(wave_format.header, 8);
					retcode = Write(wave_format.data.wFormatTag, 2);
					retcode = Write(wave_format.data.nChannels, 2);
					retcode = Write(wave_format.data.nSamplesPerSec, 4);
					retcode = Write(wave_format.data.nAvgBytesPerSec, 4);
					retcode = Write(wave_format.data.nBlockAlign, 2);
					retcode = Write(wave_format.data.nBitsPerSample, 2);
			
					if (retcode == DDC_SUCCESS)
					{
						pcm_data_offset = CurrentFilePosition();
						retcode = Write(pcm_data, 8);
					}
				}
			}
			
			return retcode;
		}
		
		/// <summary>*
		/// *
		/// public int ReadSample ( short[] Sample )
		/// {
		/// </summary>
		/// <summary>}
		/// </summary>
		
		/// <summary>*
		/// *
		/// public int WriteSample( short[] Sample )
		/// {
		/// int retcode = DDC_SUCCESS;
		/// switch ( wave_format.data.nChannels )
		/// {
		/// case 1:
		/// switch ( wave_format.data.nBitsPerSample )
		/// {
		/// case 8:
		/// pcm_data.ckSize += 1;
		/// retcode = Write ( Sample, 1 );
		/// break;
		/// </summary>
		/// <summary>case 16:
		/// pcm_data.ckSize += 2;
		/// retcode = Write ( Sample, 2 );
		/// break;
		/// </summary>
		/// <summary>default:
		/// retcode = DDC_INVALID_CALL;
		/// }
		/// break;
		/// </summary>
		/// <summary>case 2:
		/// switch ( wave_format.data.nBitsPerSample )
		/// {
		/// case 8:
		/// retcode = Write ( Sample, 1 );
		/// if ( retcode == DDC_SUCCESS )
		/// {
		/// // &Sample[1]
		/// retcode = Write (Sample, 1 );
		/// if ( retcode == DDC_SUCCESS )
		/// {
		/// pcm_data.ckSize += 2;
		/// }
		/// }
		/// break;
		/// </summary>
		/// <summary>case 16:
		/// retcode = Write ( Sample, 2 );
		/// if ( retcode == DDC_SUCCESS )
		/// {
		/// // &Sample[1]
		/// retcode = Write (Sample, 2 );
		/// if ( retcode == DDC_SUCCESS )
		/// {
		/// pcm_data.ckSize += 4;
		/// }
		/// }
		/// break;
		/// </summary>
		/// <summary>default:
		/// retcode = DDC_INVALID_CALL;
		/// }
		/// break;
		/// </summary>
		/// <summary>default:
		/// retcode = DDC_INVALID_CALL;
		/// }
		/// </summary>
		/// <summary>return retcode;
		/// }
		/// </summary>
		
		/// <summary>*
		/// *
		/// public int SeekToSample ( long SampleIndex )
		/// {
		/// if ( SampleIndex >= NumSamples() )
		/// {
		/// return DDC_INVALID_CALL;
		/// }
		/// int SampleSize = (BitsPerSample() + 7) / 8;
		/// int rc = Seek ( pcm_data_offset + 8 +
		/// SampleSize * NumChannels() * SampleIndex );
		/// return rc;
		/// }
		/// </summary>
		
		/// <summary> Write 16-bit audio
		/// </summary>
		public virtual int WriteData(short[] data, int numData)
		{
			int extraBytes = numData * 2;
			pcm_data.ckSize += extraBytes;
			return base.Write(data, extraBytes);
		}
		
		/// <summary> Read 16-bit audio.
		/// *
		/// public int ReadData  (short[] data, int numData)
		/// {return super.Read ( data, numData * 2);} 
		/// </summary>
		
		/// <summary> Write 8-bit audio.
		/// *
		/// public int WriteData ( byte[] data, int numData )
		/// {
		/// pcm_data.ckSize += numData;
		/// return super.Write ( data, numData );
		/// }
		/// </summary>
		
		/// <summary> Read 8-bit audio.
		/// *
		/// public int ReadData ( byte[] data, int numData )
		/// {return super.Read ( data, numData );} 
		/// </summary>
		
		
		/// <summary>*
		/// *
		/// public int ReadSamples  (int num, int [] WaveFileSample)
		/// {
		/// </summary>
		/// <summary>}
		/// </summary>
		
		/// <summary>*
		/// *
		/// public int WriteMonoSample ( short[] SampleData )
		/// {
		/// switch ( wave_format.data.nBitsPerSample )
		/// {
		/// case 8:
		/// pcm_data.ckSize += 1;
		/// return Write ( SampleData, 1 );
		/// </summary>
		/// <summary>case 16:
		/// pcm_data.ckSize += 2;
		/// return Write ( SampleData, 2 );
		/// }
		/// return DDC_INVALID_CALL;
		/// }
		/// </summary>
		
		/// <summary>*
		/// *
		/// public int WriteStereoSample  ( short[] LeftSample, short[] RightSample )
		/// {
		/// int retcode = DDC_SUCCESS;
		/// switch ( wave_format.data.nBitsPerSample )
		/// {
		/// case 8:
		/// retcode = Write ( LeftSample, 1 );
		/// if ( retcode == DDC_SUCCESS )
		/// {
		/// retcode = Write ( RightSample, 1 );
		/// if ( retcode == DDC_SUCCESS )
		/// {
		/// pcm_data.ckSize += 2;
		/// }
		/// }
		/// break;
		/// </summary>
		/// <summary>case 16:
		/// retcode = Write ( LeftSample, 2 );
		/// if ( retcode == DDC_SUCCESS )
		/// {
		/// retcode = Write ( RightSample, 2 );
		/// if ( retcode == DDC_SUCCESS )
		/// {
		/// pcm_data.ckSize += 4;
		/// }
		/// }
		/// break;
		/// </summary>
		/// <summary>default:
		/// retcode = DDC_INVALID_CALL;
		/// }   
		/// return retcode;
		/// }
		/// </summary>
		
		/// <summary>*
		/// *
		/// public int ReadMonoSample ( short[] Sample )
		/// {
		/// int retcode = DDC_SUCCESS;
		/// switch ( wave_format.data.nBitsPerSample )
		/// {
		/// case 8:
		/// byte[] x = {0};
		/// retcode = Read ( x, 1 );
		/// Sample[0] = (short)(x[0]);
		/// break;
		/// </summary>
		/// <summary>case 16:
		/// retcode = Read ( Sample, 2 );
		/// break;
		/// </summary>
		/// <summary>default:
		/// retcode = DDC_INVALID_CALL;
		/// }
		/// return retcode;
		/// }
		/// </summary>
		
		/// <summary>*
		/// *
		/// public int ReadStereoSample ( short[] LeftSampleData, short[] RightSampleData )
		/// {
		/// int retcode = DDC_SUCCESS;
		/// byte[] x = new byte[2];
		/// short[] y = new short[2];
		/// switch ( wave_format.data.nBitsPerSample )
		/// {
		/// case 8:
		/// retcode = Read ( x, 2 );
		/// L[0] = (short) ( x[0] );
		/// R[0] = (short) ( x[1] );
		/// break;
		/// </summary>
		/// <summary>case 16:
		/// retcode = Read ( y, 4 );
		/// L[0] = (short) ( y[0] );
		/// R[0] = (short) ( y[1] );
		/// break;
		/// </summary>
		/// <summary>default:
		/// retcode = DDC_INVALID_CALL;
		/// }
		/// return retcode;
		/// }
		/// </summary>
		
		
		/// <summary>*
		/// </summary>
		public override int Close()
		{
			int rc = DDC_SUCCESS;
			
			if (fmode == RFM_WRITE)
				rc = Backpatch(pcm_data_offset, pcm_data, 8);
			if (!JustWriteLengthBytes)
			{
				if (rc == DDC_SUCCESS)
					rc = base.Close();
			}
			return rc;
		}
		public int Close(bool justWriteLengthBytes)
		{
			JustWriteLengthBytes = justWriteLengthBytes;
			int ret = Close();
			JustWriteLengthBytes = false;
			return ret;
		}
		bool JustWriteLengthBytes = false;

		

		// [Hz]
		public virtual int SamplingRate()
		{
			return wave_format.data.nSamplesPerSec;
		}
		
		public virtual short BitsPerSample()
		{
			return wave_format.data.nBitsPerSample;
		}
		
		public virtual short NumChannels()
		{
			return wave_format.data.nChannels;
		}
		
		public virtual int NumSamples()
		{
			return num_samples;
		}
		
		
		/// <summary> Open for write using another wave file's parameters...
		/// </summary>
		public virtual int OpenForWrite(System.String Filename, WaveFile OtherWave)
		{
			return OpenForWrite(Filename, null, OtherWave.SamplingRate(), OtherWave.BitsPerSample(), OtherWave.NumChannels());
		}
		
		/// <summary>*
		/// </summary>
		public override long CurrentFilePosition()
		{
			return base.CurrentFilePosition();
		}
		
		/* public int FourCC(String ChunkName)
		{
		byte[] p = {0x20,0x20,0x20,0x20};
		ChunkName.getBytes(0,4,p,0);
		int ret = (((p[0] << 24)& 0xFF000000) | ((p[1] << 16)&0x00FF0000) | ((p[2] << 8)&0x0000FF00) | (p[3]&0x000000FF));
		return ret;
		}*/
	}
}