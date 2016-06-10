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
	/// <summary> Class to manage RIFF files
	/// </summary>
	internal class RiffFile
	{
		//UPGRADE_NOTE: Field 'EnclosingInstance' was added to class 'RiffChunkHeader' to access its enclosing instance. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1019"'
		internal class RiffChunkHeader
		{
			private void  InitBlock(RiffFile enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private RiffFile enclosingInstance;
			public RiffFile Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			public int ckID = 0; // Four-character chunk ID
			public int ckSize = 0;
			// Length of data in chunk
			public RiffChunkHeader(RiffFile enclosingInstance)
			{
				InitBlock(enclosingInstance);
			}
		}
		
		
		// DDCRET
		public const int DDC_SUCCESS = 0; // The operation succeded
		public const int DDC_FAILURE = 1; // The operation failed for unspecified reasons
		public const int DDC_OUT_OF_MEMORY = 2; // Operation failed due to running out of memory
		public const int DDC_FILE_ERROR = 3; // Operation encountered file I/O error
		public const int DDC_INVALID_CALL = 4; // Operation was called with invalid parameters
		public const int DDC_USER_ABORT = 5; // Operation was aborted by the user
		public const int DDC_INVALID_FILE = 6; // File format does not match
		
		// RiffFileMode
		public const int RFM_UNKNOWN = 0; // undefined type (can use to mean "N/A" or "not open")
		public const int RFM_WRITE = 1; // open for write
		public const int RFM_READ = 2; // open for read
		
		private RiffChunkHeader riff_header; // header for whole file
		protected internal int fmode; // current file I/O mode
		//protected internal System.IO.FileStream file; // I/O stream to use
		protected internal System.IO.Stream file; // I/O stream to use
		
		/// <summary> Dummy Constructor
		/// </summary>
		public RiffFile()
		{
			file = null;
			fmode = RFM_UNKNOWN;
			riff_header = new RiffChunkHeader(this);
			
			riff_header.ckID = FourCC("RIFF");
			riff_header.ckSize = 0;
		}
		
		/// <summary> Return File Mode.
		/// </summary>
		public virtual int CurrentFileMode()
		{
			return fmode;
		}
		
		/// <summary> Open a RIFF file.
		/// </summary>
		public virtual int Open(System.String Filename, int NewMode)
		{
			int retcode = DDC_SUCCESS;
			
			if (fmode != RFM_UNKNOWN)
			{
				retcode = Close();
			}
			
			if (retcode == DDC_SUCCESS)
			{
				switch (NewMode)
				{
					
					case RFM_WRITE: 
						try
						{
							file = SupportClass.RandomAccessFileSupport.CreateRandomAccessFile(Filename, "rw");
							
							try
							{
								// Write the RIFF header...
								// We will have to come back later and patch it!
								sbyte[] br = new sbyte[8];
								br[0] = (sbyte) ((SupportClass.URShift(riff_header.ckID, 24)) & 0x000000FF);
								br[1] = (sbyte) ((SupportClass.URShift(riff_header.ckID, 16)) & 0x000000FF);
								br[2] = (sbyte) ((SupportClass.URShift(riff_header.ckID, 8)) & 0x000000FF);
								br[3] = (sbyte) (riff_header.ckID & 0x000000FF);
								
								sbyte br4 = (sbyte) ((SupportClass.URShift(riff_header.ckSize, 24)) & 0x000000FF);
								sbyte br5 = (sbyte) ((SupportClass.URShift(riff_header.ckSize, 16)) & 0x000000FF);
								sbyte br6 = (sbyte) ((SupportClass.URShift(riff_header.ckSize, 8)) & 0x000000FF);
								sbyte br7 = (sbyte) (riff_header.ckSize & 0x000000FF);
								
								br[4] = br7;
								br[5] = br6;
								br[6] = br5;
								br[7] = br4;
								
								file.Write(SupportClass.ToByteArray(br), 0, 8);
								fmode = RFM_WRITE;
							}
							catch (System.IO.IOException ioe)
							{
								file.Close();
								fmode = RFM_UNKNOWN;
							}
						}
						catch (System.IO.IOException ioe)
						{
							fmode = RFM_UNKNOWN;
							retcode = DDC_FILE_ERROR;
						}
						break;
					
					
					case RFM_READ: 
						try
						{
							file = SupportClass.RandomAccessFileSupport.CreateRandomAccessFile(Filename, "r");
							try
							{
								// Try to read the RIFF header...   				   
								sbyte[] br = new sbyte[8];
								SupportClass.ReadInput(file, ref br, 0, 8);
								fmode = RFM_READ;
								riff_header.ckID = ((br[0] << 24) & (int) SupportClass.Identity(0xFF000000)) | ((br[1] << 16) & 0x00FF0000) | ((br[2] << 8) & 0x0000FF00) | (br[3] & 0x000000FF);
								riff_header.ckSize = ((br[4] << 24) & (int) SupportClass.Identity(0xFF000000)) | ((br[5] << 16) & 0x00FF0000) | ((br[6] << 8) & 0x0000FF00) | (br[7] & 0x000000FF);
							}
							catch (System.IO.IOException ioe)
							{
								file.Close();
								fmode = RFM_UNKNOWN;
							}
						}
						catch (System.IO.IOException ioe)
						{
							fmode = RFM_UNKNOWN;
							retcode = DDC_FILE_ERROR;
						}
						break;
					
					default: 
						retcode = DDC_INVALID_CALL;
						break;
					
				}
			}
			return retcode;
		}
		

		/// <summary> Open a RIFF STREAM.
		/// </summary>
		public virtual int Open(System.IO.Stream stream, int NewMode)
		{
			int retcode = DDC_SUCCESS;
			
			if (fmode != RFM_UNKNOWN)
			{
				retcode = Close();
			}
			
			if (retcode == DDC_SUCCESS)
			{
				switch (NewMode)
				{
					
					case RFM_WRITE: 
						try
						{
							//file = SupportClass.RandomAccessFileSupport.CreateRandomAccessFile(Filename, "rw");
							file = stream;
							
							try
							{
								// Write the RIFF header...
								// We will have to come back later and patch it!
								sbyte[] br = new sbyte[8];
								br[0] = (sbyte) ((SupportClass.URShift(riff_header.ckID, 24)) & 0x000000FF);
								br[1] = (sbyte) ((SupportClass.URShift(riff_header.ckID, 16)) & 0x000000FF);
								br[2] = (sbyte) ((SupportClass.URShift(riff_header.ckID, 8)) & 0x000000FF);
								br[3] = (sbyte) (riff_header.ckID & 0x000000FF);
								
								sbyte br4 = (sbyte) ((SupportClass.URShift(riff_header.ckSize, 24)) & 0x000000FF);
								sbyte br5 = (sbyte) ((SupportClass.URShift(riff_header.ckSize, 16)) & 0x000000FF);
								sbyte br6 = (sbyte) ((SupportClass.URShift(riff_header.ckSize, 8)) & 0x000000FF);
								sbyte br7 = (sbyte) (riff_header.ckSize & 0x000000FF);
								
								br[4] = br7;
								br[5] = br6;
								br[6] = br5;
								br[7] = br4;
								
								file.Write(SupportClass.ToByteArray(br), 0, 8);
								fmode = RFM_WRITE;
							}
							catch (System.IO.IOException ioe)
							{
								file.Close();
								fmode = RFM_UNKNOWN;
							}
						}
						catch (System.IO.IOException ioe)
						{
							fmode = RFM_UNKNOWN;
							retcode = DDC_FILE_ERROR;
						}
						break;
					
					
					case RFM_READ: 
						try
						{
							file = stream;
							//file = SupportClass.RandomAccessFileSupport.CreateRandomAccessFile(Filename, "r");
							try
							{
								// Try to read the RIFF header...   				   
								sbyte[] br = new sbyte[8];
								SupportClass.ReadInput(file, ref br, 0, 8);
								fmode = RFM_READ;
								riff_header.ckID = ((br[0] << 24) & (int) SupportClass.Identity(0xFF000000)) | ((br[1] << 16) & 0x00FF0000) | ((br[2] << 8) & 0x0000FF00) | (br[3] & 0x000000FF);
								riff_header.ckSize = ((br[4] << 24) & (int) SupportClass.Identity(0xFF000000)) | ((br[5] << 16) & 0x00FF0000) | ((br[6] << 8) & 0x0000FF00) | (br[7] & 0x000000FF);
							}
							catch (System.IO.IOException ioe)
							{
								file.Close();
								fmode = RFM_UNKNOWN;
							}
						}
						catch (System.IO.IOException ioe)
						{
							fmode = RFM_UNKNOWN;
							retcode = DDC_FILE_ERROR;
						}
						break;
					
					default: 
						retcode = DDC_INVALID_CALL;
						break;
					
				}
			}
			return retcode;
		}
		

		
		/// <summary> Write NumBytes data.
		/// </summary>
		public virtual int Write(sbyte[] Data, int NumBytes)
		{
			if (fmode != RFM_WRITE)
			{
				return DDC_INVALID_CALL;
			}
			try
			{
				file.Write(SupportClass.ToByteArray(Data), 0, NumBytes);
				fmode = RFM_WRITE;
			}
			catch (System.IO.IOException ioe)
			{
				return DDC_FILE_ERROR;
			}
			riff_header.ckSize += NumBytes;
			return DDC_SUCCESS;
		}
		
		
		
		/// <summary> Write NumBytes data.
		/// </summary>
		public virtual int Write(short[] Data, int NumBytes)
		{
			sbyte[] theData = new sbyte[NumBytes];
			int yc = 0;
			for (int y = 0; y < NumBytes; y = y + 2)
			{
				theData[y] = (sbyte) (Data[yc] & 0x00FF);
				theData[y + 1] = (sbyte) ((SupportClass.URShift(Data[yc++], 8)) & 0x00FF);
			}
			if (fmode != RFM_WRITE)
			{
				return DDC_INVALID_CALL;
			}
			try
			{
				file.Write(SupportClass.ToByteArray(theData), 0, NumBytes);
				fmode = RFM_WRITE;
			}
			catch (System.IO.IOException ioe)
			{
				return DDC_FILE_ERROR;
			}
			riff_header.ckSize += NumBytes;
			return DDC_SUCCESS;
		}
		
		/// <summary> Write NumBytes data.
		/// </summary>
		public virtual int Write(RiffChunkHeader Triff_header, int NumBytes)
		{
			sbyte[] br = new sbyte[8];
			br[0] = (sbyte) ((SupportClass.URShift(Triff_header.ckID, 24)) & 0x000000FF);
			br[1] = (sbyte) ((SupportClass.URShift(Triff_header.ckID, 16)) & 0x000000FF);
			br[2] = (sbyte) ((SupportClass.URShift(Triff_header.ckID, 8)) & 0x000000FF);
			br[3] = (sbyte) (Triff_header.ckID & 0x000000FF);
			
			sbyte br4 = (sbyte) ((SupportClass.URShift(Triff_header.ckSize, 24)) & 0x000000FF);
			sbyte br5 = (sbyte) ((SupportClass.URShift(Triff_header.ckSize, 16)) & 0x000000FF);
			sbyte br6 = (sbyte) ((SupportClass.URShift(Triff_header.ckSize, 8)) & 0x000000FF);
			sbyte br7 = (sbyte) (Triff_header.ckSize & 0x000000FF);
			
			br[4] = br7;
			br[5] = br6;
			br[6] = br5;
			br[7] = br4;
			
			if (fmode != RFM_WRITE)
			{
				return DDC_INVALID_CALL;
			}
			try
			{
				file.Write(SupportClass.ToByteArray(br), 0, NumBytes);
				fmode = RFM_WRITE;
			}
			catch (System.IO.IOException ioe)
			{
				return DDC_FILE_ERROR;
			}
			riff_header.ckSize += NumBytes;
			return DDC_SUCCESS;
		}
		
		/// <summary> Write NumBytes data.
		/// </summary>
		public virtual int Write(short Data, int NumBytes)
		{
			short theData = Data;//(short) (((SupportClass.URShift(Data, 8)) & 0x00FF) | ((Data << 8) & 0xFF00));
			if (fmode != RFM_WRITE)
			{
				return DDC_INVALID_CALL;
			}
			try
			{
				System.IO.BinaryWriter temp_BinaryWriter;
				temp_BinaryWriter = new System.IO.BinaryWriter(file);
				temp_BinaryWriter.Write((System.Int16) theData);
				fmode = RFM_WRITE;
			}
			catch (System.IO.IOException ioe)
			{
				return DDC_FILE_ERROR;
			}
			riff_header.ckSize += NumBytes;
			return DDC_SUCCESS;
		}
		/// <summary> Write NumBytes data.
		/// </summary>
		public virtual int Write(int Data, int NumBytes)
		{
			short theDataL = (short) ((SupportClass.URShift(Data, 16)) & 0x0000FFFF);
			short theDataR = (short) (Data & 0x0000FFFF);
			short theDataLI = (short) (((SupportClass.URShift(theDataL, 8)) & 0x00FF) | ((theDataL << 8) & 0xFF00));
			short theDataRI = (short) (((SupportClass.URShift(theDataR, 8)) & 0x00FF) | ((theDataR << 8) & 0xFF00));
			int theData = Data;//((theDataRI << 16) & (int) SupportClass.Identity(0xFFFF0000)) | (theDataLI & 0x0000FFFF);
			if (fmode != RFM_WRITE)
			{
				return DDC_INVALID_CALL;
			}
			try
			{
				System.IO.BinaryWriter temp_BinaryWriter;
				temp_BinaryWriter = new System.IO.BinaryWriter(file);
				temp_BinaryWriter.Write((System.Int32) theData);
				fmode = RFM_WRITE;
			}
			catch (System.IO.IOException ioe)
			{
				return DDC_FILE_ERROR;
			}
			riff_header.ckSize += NumBytes;
			return DDC_SUCCESS;
		}
		
		
		
		/// <summary> Read NumBytes data.
		/// </summary>
		public virtual int Read(sbyte[] Data, int NumBytes)
		{
			int retcode = DDC_SUCCESS;
			try
			{
				SupportClass.ReadInput(file, ref Data, 0, NumBytes);
			}
			catch (System.IO.IOException ioe)
			{
				retcode = DDC_FILE_ERROR;
			}
			return retcode;
		}
		
		/// <summary> Expect NumBytes data.
		/// </summary>
		public virtual int Expect(System.String Data, int NumBytes)
		{
			sbyte target = 0;
			int cnt = 0;
			try
			{
				while ((NumBytes--) != 0)
				{
					target = (sbyte) file.ReadByte();
					if (target != Data[cnt++])
						return DDC_FILE_ERROR;
				}
			}
			catch (System.IO.IOException ioe)
			{
				return DDC_FILE_ERROR;
			}
			return DDC_SUCCESS;
		}
		
		/// <summary> Close Riff File.
		/// Length is written too.
		/// </summary>
		public virtual int Close()
		{
			int retcode = DDC_SUCCESS;
			
			switch (fmode)
			{
				
				case RFM_WRITE: 
					try
					{
						file.Seek(0, System.IO.SeekOrigin.Begin);
						try
						{
							sbyte[] br = new sbyte[8];
							br[0] = (sbyte) ((SupportClass.URShift(riff_header.ckID, 24)) & 0x000000FF);
							br[1] = (sbyte) ((SupportClass.URShift(riff_header.ckID, 16)) & 0x000000FF);
							br[2] = (sbyte) ((SupportClass.URShift(riff_header.ckID, 8)) & 0x000000FF);
							br[3] = (sbyte) (riff_header.ckID & 0x000000FF);
							
							br[7] = (sbyte) ((SupportClass.URShift(riff_header.ckSize, 24)) & 0x000000FF);
							br[6] = (sbyte) ((SupportClass.URShift(riff_header.ckSize, 16)) & 0x000000FF);
							br[5] = (sbyte) ((SupportClass.URShift(riff_header.ckSize, 8)) & 0x000000FF);
							br[4] = (sbyte) (riff_header.ckSize & 0x000000FF);
							file.Write(SupportClass.ToByteArray(br), 0, 8);
							file.Close();
						}
						catch (System.IO.IOException ioe)
						{
							retcode = DDC_FILE_ERROR;
						}
					}
					catch (System.IO.IOException ioe)
					{
						retcode = DDC_FILE_ERROR;
					}
					break;
				
				
				case RFM_READ: 
					try
					{
						file.Close();
					}
					catch (System.IO.IOException ioe)
					{
						retcode = DDC_FILE_ERROR;
					}
					break;
				}
			file = null;
			fmode = RFM_UNKNOWN;
			return retcode;
		}
		
		/// <summary> Return File Position.
		/// </summary>
		public virtual long CurrentFilePosition()
		{
			long position;
			try
			{
				position = file.Position;
			}
			catch (System.IO.IOException ioe)
			{
				position = - 1;
			}
			return position;
		}
		
		/// <summary> Write Data to specified offset.
		/// </summary>
		public virtual int Backpatch(long FileOffset, RiffChunkHeader Data, int NumBytes)
		{
			if (file == null)
			{
				return DDC_INVALID_CALL;
			}
			try
			{
				file.Seek(FileOffset, System.IO.SeekOrigin.Begin);
			}
			catch (System.IO.IOException ioe)
			{
				return DDC_FILE_ERROR;
			}
			return Write(Data, NumBytes);
		}
		
		public virtual int Backpatch(long FileOffset, sbyte[] Data, int NumBytes)
		{
			if (file == null)
			{
				return DDC_INVALID_CALL;
			}
			try
			{
				file.Seek(FileOffset, System.IO.SeekOrigin.Begin);
			}
			catch (System.IO.IOException ioe)
			{
				return DDC_FILE_ERROR;
			}
			return Write(Data, NumBytes);
		}
		
		
		/// <summary> Seek in the File.
		/// </summary>
		protected internal virtual int Seek(long offset)
		{
			int rc;
			try
			{
				file.Seek(offset, System.IO.SeekOrigin.Begin);
				rc = DDC_SUCCESS;
			}
			catch (System.IO.IOException ioe)
			{
				rc = DDC_FILE_ERROR;
			}
			return rc;
		}
		
		/// <summary> Error Messages.
		/// </summary>
		private System.String DDCRET_String(int retcode)
		{
			switch (retcode)
			{
				
				case DDC_SUCCESS:  return "DDC_SUCCESS";
				
				case DDC_FAILURE:  return "DDC_FAILURE";
				
				case DDC_OUT_OF_MEMORY:  return "DDC_OUT_OF_MEMORY";
				
				case DDC_FILE_ERROR:  return "DDC_FILE_ERROR";
				
				case DDC_INVALID_CALL:  return "DDC_INVALID_CALL";
				
				case DDC_USER_ABORT:  return "DDC_USER_ABORT";
				
				case DDC_INVALID_FILE:  return "DDC_INVALID_FILE";
				}
			return "Unknown Error";
		}
		
		/// <summary> Fill the header.
		/// </summary>
		public static int FourCC(System.String ChunkName)
		{
			sbyte[] p = new sbyte[]{(sbyte) (0x20), (sbyte) (0x20), (sbyte) (0x20), (sbyte) (0x20)};
			SupportClass.GetSBytesFromString(ChunkName, 0, 4, ref p, 0);
			int ret = (((p[0] << 24) & (int) SupportClass.Identity(0xFF000000)) | ((p[1] << 16) & 0x00FF0000) | ((p[2] << 8) & 0x0000FF00) | (p[3] & 0x000000FF));
			return ret;
		}
	}
}