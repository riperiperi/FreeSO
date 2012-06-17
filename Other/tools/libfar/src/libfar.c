/*
  This file is a part of libfar and is licensed under the X11 License.
*/

#include <string.h>
#include <stddef.h>
#include <stdio.h>
#include <stdlib.h>
#include "../include/libfar.h"

enum FARTYPE libfarIdentify(const unsigned char * buffer, unsigned int filesize){
	/* There can be a 0-file FAR1 archive. It consists of 12-byte Header + 4-byte Offset value + 4-byte file count value */
	/* The same goes for FAR3 archives. */
	
	/* A 0-file DBPF archive consists of the header alone. Therefore, it is 56 bytes in size. */
	
	if(!memcmp(buffer, FAR1header, 12)){
		if(filesize == 0 || filesize >= MINSIZE_FAR1FILE) return FAR_FARV1;
	}else if(!memcmp(buffer, FAR3header, 12)){
		if(filesize == 0 || filesize >= MINSIZE_FAR3FILE) return FAR_FARV3;
	}else if(!memcmp(buffer, DBPFheader, 12)){
		if(filesize == 0 || filesize >= MINSIZE_DBPFFILE) return FAR_DBPF;
	}
	return FAR_INVALID;
}

int CreateFAR1File(struct FAR1File * File, const unsigned char * buffer, unsigned int filesize){
	if(filesize != 0 && filesize < MINSIZE_FAR1FILE)
		return 0;
	
	/* Read the header to examine if the file opened is a valid FAR file */
	if(memcmp(buffer, FAR1header, 12) != 0) return 0;
	
	File->FAR1Header.IndexOffset = (buffer[12]<<0) | (buffer[13]<<8) | (buffer[14]<<16) | (buffer[15]<<24);
	if(File->FAR1Header.IndexOffset > 0xFFFFFFFF - (4-1)) return 0; /* 4 bytes are used for FilesCount */
	return (filesize) ? (filesize >= File->FAR1Header.IndexOffset+(4-1)) : 1;
}

int CreateFAR3File(struct FAR3File * File, const unsigned char * buffer, unsigned int filesize){
	if(filesize != 0 && filesize < MINSIZE_FAR3FILE)
		return 0;

	/* Read the header to examine if the file opened is a valid FAR file */
	if(memcmp(buffer, FAR3header, 12) != 0) return 0;
	
	File->FAR3Header.IndexOffset = (buffer[12]<<0) | (buffer[13]<<8) | (buffer[14]<<16) | (buffer[15]<<24);
	if(File->FAR3Header.IndexOffset > 0xFFFFFFFF - 3) return 0; /* 4 bytes are used for FilesCount */
	return (filesize) ? (filesize >= File->FAR3Header.IndexOffset+4) : 1;
}

int CreateDBPFFile(struct DBPFFile * File, const unsigned char * buffer, unsigned int filesize){
	if(filesize != 0 && filesize < MINSIZE_DBPFFILE)
		return 0;
	
	/* Read the header to examine if the file opened is a valid DBPF file */
	if(memcmp(buffer, DBPFheader, 12) != 0) return 0;
	
	File->DBPFHeader.DateCreated		= (buffer[0]<<0)  | (buffer[1]<<8)  | (buffer[2]<<16)  | (buffer[3]<<24);
	File->DBPFHeader.DateModified		= (buffer[4]<<0)  | (buffer[5]<<8)  | (buffer[6]<<16)  | (buffer[7]<<24);
	/* Skip the next 12 bytes */
	File->DBPFHeader.IndexMajorVersion	= (buffer[20]<<0) | (buffer[21]<<8) | (buffer[22]<<16) | (buffer[23]<<24);
	File->DBPFHeader.FilesCount			= (buffer[24]<<0) | (buffer[25]<<8) | (buffer[26]<<16) | (buffer[27]<<24);
	File->DBPFHeader.IndexOffset		= (buffer[28]<<0) | (buffer[29]<<8) | (buffer[30]<<16) | (buffer[31]<<24);
	File->DBPFHeader.IndexSize			= (buffer[32]<<0) | (buffer[33]<<8) | (buffer[34]<<16) | (buffer[35]<<24);
	File->DBPFHeader.HoleCount			= (buffer[36]<<0) | (buffer[37]<<8) | (buffer[38]<<16) | (buffer[39]<<24);
	File->DBPFHeader.HoleIndexOffset	= (buffer[40]<<0) | (buffer[41]<<8) | (buffer[42]<<16) | (buffer[43]<<24);
	File->DBPFHeader.HoleIndexSize		= (buffer[44]<<0) | (buffer[45]<<8) | (buffer[46]<<16) | (buffer[47]<<24);
	File->DBPFHeader.IndexMinorVersion	= (buffer[48]<<0) | (buffer[49]<<8) | (buffer[50]<<16) | (buffer[51]<<24);
	File->DBPFHeader.FirstEntryOffset	= (buffer[52]<<0) | (buffer[53]<<8) | (buffer[54]<<16) | (buffer[55]<<24);
	
	if(filesize && (filesize < File->DBPFHeader.IndexOffset || (filesize-File->DBPFHeader.IndexOffset) < File->DBPFHeader.IndexSize ||
		filesize < File->DBPFHeader.HoleIndexOffset || (filesize-File->DBPFHeader.HoleIndexOffset) < File->DBPFHeader.HoleIndexSize))
		return 0;
	return (File->DBPFHeader.IndexSize == File->DBPFHeader.FilesCount*20);
}

int FARInitialize(void * File, enum FARTYPE ArchiveType, unsigned int FilesCount, unsigned char * Index, unsigned int IndexSize){
	/* In FAR v1 and FAR v3, IndexSize must be >=4 */
	if(ArchiveType == FAR_FARV1){
		((struct FAR1File *) File)->FAR1Header.FilesCount	= FilesCount;
		if(Index != NULL)
			((struct FAR1File *) File)->Index				= Index;
		if(IndexSize)
			((struct FAR1File *) File)->IndexSize			= IndexSize;
	}else if(ArchiveType == FAR_FARV3){
		((struct FAR3File *) File)->FAR3Header.FilesCount	= FilesCount;
		if(Index != NULL)
			((struct FAR3File *) File)->Index				= Index;
		if(IndexSize)
			((struct FAR3File *) File)->IndexSize			= IndexSize;
	}else if(ArchiveType == FAR_DBPF){
		if(Index != NULL)
			((struct DBPFFile *) File)->Index				= Index;
	}else return 0;
	
	return 1;
}

int CreateFAR1Entry(struct FAR1Entry * Entry, const unsigned char * buffer){
	Entry->DecompressedDataSize	= (buffer[0]<<0)  | (buffer[1]<<8)  | (buffer[2]<<16)  | (buffer[3]<<24);
	Entry->CompressedDataSize	= (buffer[4]<<0)  | (buffer[5]<<8)  | (buffer[6]<<16)  | (buffer[7]<<24);
	Entry->DataOffset			= (buffer[8]<<0)  | (buffer[9]<<8)  | (buffer[10]<<16) | (buffer[11]<<24);
	Entry->FilenameLength		= (buffer[12]<<0) | (buffer[13]<<8) | (buffer[14]<<16) | (buffer[15]<<24);
	
	return 1;
}

int CreateFAR3Entry(struct FAR3Entry * Entry, const unsigned char * buffer){
	Entry->DecompressedDataSize	= (buffer[0]<<0)  | (buffer[1]<<8)  | (buffer[2]<<16)  | (buffer[3]<<24);
	Entry->CompressedDataSize	= (buffer[4]<<0)  | (buffer[5]<<8)  | (buffer[6]<<16);
	Entry->DataType				= (buffer[7]<<0);
	Entry->DataOffset			= (buffer[8]<<0)  | (buffer[9]<<8)  | (buffer[10]<<16) | (buffer[11]<<24);
	Entry->Compressed			= (buffer[12]<<0);
	Entry->AccessNumber			= (buffer[13]<<0);
	Entry->FilenameLength		= (buffer[14]<<0) | (buffer[15]<<8);
	Entry->TypeID				= (buffer[16]<<0) | (buffer[17]<<8) | (buffer[18]<<16) | (buffer[19]<<24);
	Entry->FileID				= (buffer[20]<<0) | (buffer[21]<<8) | (buffer[22]<<16) | (buffer[23]<<24);

	return (Entry->Compressed <= 1);
}

int CreateDBPFEntry(struct DBPFEntry * Entry, const unsigned char * buffer){
	Entry->TypeID		= (buffer[0]<<0)  | (buffer[1]<<8)  | (buffer[2]<<16)  | (buffer[3]<<24);
	Entry->GroupID		= (buffer[4]<<0)  | (buffer[5]<<8)  | (buffer[6]<<16)  | (buffer[7]<<24);
	Entry->FileID		= (buffer[8]<<0)  | (buffer[9]<<8)  | (buffer[10]<<16) | (buffer[11]<<24);
	Entry->DataOffset	= (buffer[12]<<0) | (buffer[13]<<8) | (buffer[14]<<16) | (buffer[15]<<24);
	Entry->DataSize		= (buffer[16]<<0) | (buffer[17]<<8) | (buffer[18]<<16) | (buffer[19]<<24);
	
	return 1;
}

int CreateRefPackStream(struct RefPackStream * Stream, const unsigned char * buffer){
	Stream->RefPackHeader.Compressed						= (buffer[0]<<0);
	Stream->RefPackHeader.DecompressedDataSize				= (buffer[1]<<0)  | (buffer[2]<<8)  | (buffer[3]<<16);
	Stream->RefPackHeader.OoBChar							= (buffer[4]<<0);
	Stream->RefPackHeader.StreamBodySize					= (buffer[5]<<0)  | (buffer[6]<<8)  | (buffer[7]<<16)  | (buffer[8]<<24);
	Stream->RefPackHeader.CompressedFileSize				= (buffer[9]<<0)  | (buffer[10]<<8) | (buffer[11]<<16) | (buffer[12]<<24);
	Stream->RefPackHeader.CompressionMethod					= (buffer[13]<<0) | (buffer[14]<<8);
	Stream->RefPackHeader.DecompressedFileSizeFlippedEndian	= (buffer[15]<<0) | (buffer[16]<<8) | (buffer[17]<<16);
	
	return 0;
}

int RefPackDecompress(const struct RefPackStream * Stream, const unsigned char * inbuffer, unsigned char * outbuffer){
	/* Returns 0x00 on success,
		0x01 if a parameter is invalid,
		0xF1 if the last opcode did not finish,
		0xF3 if some data still had yet to be decompressed when the end of the compressed stream was reached,
		or 0xF5 if more data in the compressed stream existed when all required data was decompressed. */

	unsigned int StreamSize = Stream->RefPackHeader.CompressedFileSize, Decompresseddataleft = Stream->RefPackHeader.DecompressedDataSize;
	unsigned int offset = 0; /* Offset relative to the input stream; it cannot determine the data written in the output stream, so we must keep a dedicated variable for that */
	unsigned int datawritten = 0;
	int stopflag = 0;
	
	/* Leave if a parameter is invalid */
	if(Stream == NULL || inbuffer == NULL || outbuffer == NULL)
		return 0x01;
		
	while(offset < StreamSize && !stopflag){
		unsigned char currentbyte;
		unsigned int ProceedingDataLength, ReferencedDataLength, ReferencedDataOffset;
		
		/****
		  Fetch the opcode
		*/
		
		/* The first byte determines the size of the entire opcode.
		   In some cases, that one byte is it, but the size in other cases can be up to 4 bytes. */

		currentbyte = *(inbuffer++), offset++;
		if(currentbyte <= 0x7F){ /* 2 bytes */
			if(Decompresseddataleft < 1) return 0xF1;
			
			/* First byte */
			ProceedingDataLength = currentbyte & 0x03;
			ReferencedDataLength = ((currentbyte & 0x1C) >> 2) + 3;
			ReferencedDataOffset = (currentbyte & 0x60) << 3;
			
			/* Second byte */
			currentbyte = *(inbuffer++);
			ReferencedDataOffset += currentbyte;
			
			offset++;
		}else if(currentbyte <= 0xBF){ /* 3 bytes */
			if(Decompresseddataleft < 2) return 0xF1;
			
			/* First byte */
			ReferencedDataLength = (currentbyte & 0x3F) + 4;
			
			/* Second byte */
			currentbyte = *(inbuffer++);
			ProceedingDataLength = (currentbyte & 0xC0) >> 6;
			ReferencedDataOffset = (currentbyte & 0x3F) << 8;
			
			/* Third byte */
			currentbyte = *(inbuffer++);
			ReferencedDataOffset += currentbyte;
			
			offset += 2;
		}else if(currentbyte <= 0xDF){ /* 4 bytes */
			if(Decompresseddataleft < 1) return 0xF1;
			
			/* First byte */
			ProceedingDataLength = currentbyte & 0x03;
			ReferencedDataLength = ((currentbyte & 0x0C) << 6) + 5;
			ReferencedDataOffset = (currentbyte & 0x10) << 12;
			
			/* Second byte */
			currentbyte = *(inbuffer++);
			ReferencedDataOffset += currentbyte << 8;
			
			/* Third byte */
			currentbyte = *(inbuffer++);
			ReferencedDataOffset += currentbyte;
			
			/* Fourth byte */
			currentbyte = *(inbuffer++);
			ReferencedDataLength += currentbyte;
			
			offset += 3;
		}else{ /* 1 byte: Two different opcode types fall into this category */
			if(currentbyte <= 0xFB){
				ProceedingDataLength = ((currentbyte & 0x1F) << 2) + 4;
			}else{
				ProceedingDataLength = currentbyte & 0x03;
				stopflag++;
			}
			ReferencedDataLength = 0;
			ReferencedDataOffset = 0;
		}
		
		/****
		  Copy proceeding data
		*/
		
		if(ProceedingDataLength != 0){
			if(ProceedingDataLength > Decompresseddataleft){
				if(!Decompresseddataleft) return 0xF3;
				ProceedingDataLength = Decompresseddataleft;
			}
			
			memcpy(outbuffer, inbuffer, ProceedingDataLength);
			Decompresseddataleft	-= ProceedingDataLength;
			datawritten				+= ProceedingDataLength;
			offset		+= ProceedingDataLength;
			inbuffer	+= ProceedingDataLength;
			outbuffer	+= ProceedingDataLength;
		}

		/****
		  Copy referenced data
		*/
		
		if(ReferencedDataLength != 0){
			unsigned int copylength, nulllength = 0;
			ReferencedDataOffset++; /* An offset of 0 would mean to refer to the (uninitialized?) spot that you're writing at, which is not supposed to contain any data. */
			if(ReferencedDataLength > Decompresseddataleft){
				if(!Decompresseddataleft) return 0xF5;
				ReferencedDataLength = Decompresseddataleft;
			}
			
			copylength = (ReferencedDataLength > ReferencedDataOffset) ? ReferencedDataOffset : ReferencedDataLength;
 
			/* We need to check for the instance that the given offset is behind our write buffer.
			   When this occurs, the decoder is to treat the data as if it is all null. */
			if(ReferencedDataOffset > datawritten){
				nulllength = ReferencedDataOffset - datawritten; /* This is the number of null bytes that the offset extends into */
				#ifdef FAR_DEBUG
				printf("Null copy: Specified offset is: %lu, yet data written so far is: %lu", ReferencedDataOffset, datawritten);
				#endif
				if(nulllength > ReferencedDataLength) nulllength = ReferencedDataLength; /* But it doesn't mean that we will use all of it. */

				memset(outbuffer, 0x00, nulllength); 
				outbuffer += nulllength; /* If we still have more to copy over, outbuffer-ReferencedDataOffset points to the first real byte in the reference buffer, now. */
				if(copylength > nulllength) copylength -= nulllength; else copylength = 0;
			}

			/* It is possible that the offset specified by the stream does not provide for a large enough buffer to copy from.
			   This event would be caused by the reference data offset (relative from the end of the out buffer) being set smaller than can add with the proceeding data length to meet the reference data length.
			   When this occurs, the decoder is to repeatedly copy the referenced data (from the beginning again) until the reference copy's length is satisfied. */

			/* We will do this in a way so that we call memcpy ceil(log2(N)) times instead of N times.
			   The performance will only increase by the amount we reduce argument pushing and memcpy startup, but whatever */


			if(copylength){
				/* Copying from the source at ReferencedDataOffset */
				unsigned int datacopied = nulllength;
				unsigned char * copysource = outbuffer-nulllength;
				memcpy(outbuffer, outbuffer-ReferencedDataOffset, copylength);
				outbuffer += copylength;
				datacopied += copylength;

				for(copylength = copylength+nulllength; copylength; copylength <<= 1){
					/* Copying from what we have in the out buffer, repeatedly until all the data has been copied */

					if(copylength > ReferencedDataLength - datacopied){
						copylength = ReferencedDataLength - datacopied;
						if(!copylength) break;
					}

					memcpy(outbuffer, copysource, copylength);
					outbuffer += copylength;
					datacopied += copylength;
				}
			}

			Decompresseddataleft    -= ReferencedDataLength;
			datawritten             += ReferencedDataLength;
		}
	}
	return (Decompresseddataleft) ? 0xF3 : 0;
}

#ifdef FAR_WIN32

#define FAREXTRACT_INVALIDPARAMETER 0x00000101
#define FAREXTRACT_BADARCHIVENAME	0x00000103
#define FAREXTRACT_BADINHANDLE		0x00000105
#define FAREXTRACT_BADARCHIVE		0x00000301
#define FAREXTRACT_READERROR		0x00000303

#define FAREXTRACT_MEMINPUT			1
#define FAREXTRACT_FILENAMEINPUT	2
#define FAREXTRACT_FILEHANDLEINPUT	3

int FARExtract(const unsigned char * InputData, const char * ArchiveName, HANDLE hFile, unsigned int FileSize, int MatchParameter, const char * EntryName, unsigned int EntryID, struct FARINFO * Info, unsigned int * EntrySize, void * Destination, int Export){
	unsigned char * FARData;
	int InputMethod = 0;
	DWORD BytesTransferred;
	
	/****
	** Parameter checking
	*/
	
	if(Destination == NULL)
		return FAREXTRACT_INVALIDPARAMETER;
	
	/* Check for exactly one of: InputData, ArchiveName, hFile */
	if(InputData != NULL){
		if(FileSize < MINSIZE_FAR)
			return FAREXTRACT_INVALIDPARAMETER;
		InputMethod = FAREXTRACT_MEMINPUT;
	}
	if(ArchiveName != NULL){
		if(InputMethod || (ArchiveName[0] == '\0'))
			return FAREXTRACT_INVALIDPARAMETER;
		InputMethod = FAREXTRACT_FILENAMEINPUT;
	}
	if(hFile != NULL){
		if(InputMethod)
			return FAREXTRACT_INVALIDPARAMETER;
		InputMethod = FAREXTRACT_FILEHANDLEINPUT;
	}
	if(InputMethod == 0)
		return FAREXTRACT_INVALIDPARAMETER;
	
	if(MatchParameter&FAREXTRACT_MATCHNAME && (EntryName == NULL || EntryName[0] == '\0'))
		return FAREXTRACT_INVALIDPARAMETER;
	
	if(InputMethod == FAREXTRACT_FILENAMEINPUT){
		hFile = CreateFile(archivename, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
		if(hFile == INVALID_HANDLE_VALUE)
			return FAREXTRACT_BADARCHIVENAME;
	}else if(InputMethod == FAREXTRACT_FILEHANDLEINPUT){
		if(hFile == INVALID_HANDLE_VALUE)
			return FAREXTRACT_BADINHANDLE;
	}
	
	if(FileSize == 0){
		FileSize = GetFileSize(hFile, NULL);
		if(GetLastError() != ERROR_SUCCESS){
			CloseHandle(hFile);
			return (InputMethod == FAREXTRACT_FILEHANDLEINPUT) ? FAREXTRACT_BADINHANDLE : FAREXTRACT_BADARCHIVENAME;
		}
	}
	
	if(filesize < MINSIZE_FAR){
		CloseHandle(hFile);
		return FAREXTRACT_BADARCHIVE;
	}
	
	/****
	** Input file checking
	*/
	
	if(Info == NULL){
		if(InputMethod == FAREXTRACT_MEMINPUT)
			FARData = (unsigned char *) InputData;
		else{
			FARData = (unsigned char *) malloc(
			if(ReadFile(hFile, InputData, 
		}
	}
}

unsigned int FARExtractItemFromFileByID(const char * archivename, unsigned int FileID, unsigned char ** Destination, int Export){
	/* Valid for: FARv3, DBPF */
	
	/* If the last bit of Export is 1, the contents are exported to a file. Then,
	   If the second to last bit is also 1, the file will overwrite any existing files. */
	
	/* The function will return the size of the data if everything goes correctly or 0 if something fails. */
	
	HANDLE hFile;
	BOOL result;
	DWORD filesize, bytestransferred;
	int type;
	LARGE_INTEGER FilePointer;
	unsigned int IndexSize;
	unsigned char header[MINSIZE_FAR], * SearchOffset, * Data, * DecompressedData;
	char * DestinationFilename;
	unsigned int SearchSize;
	unsigned int item = 0;
	int found = 0;
	if(archivename == NULL || archivename[0] == 0x00 || Destination == NULL) return 0;
	
	hFile = CreateFile(archivename, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
	if(hFile == INVALID_HANDLE_VALUE) return 0;
	
	filesize = GetFileSize(hFile, NULL);
	
	if(GetLastError() != ERROR_SUCCESS || filesize < MINSIZE_FAR || ReadFile(hFile, header, 16, &bytestransferred, NULL) == FALSE || bytestransferred != 16){
		CloseHandle(hFile); return 0;
	}
	
	type = libfarIdentify(header, filesize);
	
	switch(type){
	case FAR_FARV3: {
		struct FAR3File file;
		struct FAR3Entry entry;
		struct RefPackStream rps;
		if(!CreateFAR3File(&file, header, filesize)){
			CloseHandle(hFile); return 0;
		}
		FilePointer.u.LowPart = file.FAR3Header.IndexOffset;
		SetFilePointerEx(hFile, FilePointer, (PLARGE_INTEGER) NULL, FILE_BEGIN);
		ReadFile(hFile, header, 4, &bytestransferred, NULL);
		
		IndexSize = filesize - file.FAR3Header.IndexOffset;
		Data = malloc(IndexSize);
		ReadFile(hFile, Data, IndexSize, &bytestransferred, NULL);
		FARInitialize(&file, FAR_FARV3, (Data[0]<<0) | (Data[1]<<8) | (Data[2]<<16) | (Data[3]<<24), Data, IndexSize);
		
		SearchOffset = file.Index+4;
		SearchSize = file.IndexSize-4;
		
		while(SearchSize>=MINSIZE_FAR3ENTRY && item<file.FAR3Header.FilesCount){
			CreateFAR3Entry(&entry, SearchOffset);
			if(entry.FileID == FileID){
				found++; break;
			}
			item++;
			SearchOffset	+= MINSIZE_FAR3ENTRY + entry.FilenameLength;
			SearchSize		-= MINSIZE_FAR3ENTRY + entry.FilenameLength;
		}
		free(Data);
		if(!found){
			CloseHandle(hFile); return 0;
		}
		
		Data = malloc(entry.CompressedDataSize), DecompressedData = malloc(entry.DecompressedDataSize);
		ReadFile(hFile, Data, entry.CompressedDataSize, &bytestransferred, NULL);
		CloseHandle(hFile);
		
		CreateRefPackStream(&rps, Data);
		if(RefPackDecompress(&rps, Data+MINSIZE_REFPACK, DecompressedData))
			return 0;
		
		free(Data);
		
		if(!(Export&1)){
			*Destination = DecompressedData;
			return entry.DecompressedDataSize;
		}
		
		strcpy(DestinationFilename, (const char *) *Destination);
		strcat(DestinationFilename, entry.Filename);
		hFile = CreateFile(DestinationFilename, GENERIC_WRITE, 0, NULL, (Export&3) ? CREATE_ALWAYS : CREATE_NEW, FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, NULL);
		if(hFile == INVALID_HANDLE_VALUE) return 0;
		
		result = WriteFile(hFile, DecompressedData, entry.DecompressedDataSize, &bytestransferred, NULL);
		CloseHandle(hFile);
		return (result == TRUE && bytestransferred == entry.DecompressedDataSize) ? entry.DecompressedDataSize : 0;
		} break;
	case FAR_DBPF: {
		struct DBPFFile file;
		struct DBPFEntry entry;
		if(!CreateDBPFFile(&file, header, filesize)){
			CloseHandle(hFile); return 0;
		}
		FilePointer.u.LowPart = file.DBPFHeader.IndexOffset;
		SetFilePointerEx(hFile, FilePointer, (PLARGE_INTEGER) NULL, FILE_BEGIN);
		
		IndexSize = filesize - file.DBPFHeader.IndexOffset;
		Data = malloc(IndexSize);
		ReadFile(hFile, Data, IndexSize, &bytestransferred, NULL);
		
		SearchOffset = Data+4;
		SearchSize = file.DBPFHeader.IndexSize-4;
		
		while(SearchSize>=MINSIZE_DBPFENTRY && item<file.DBPFHeader.FilesCount){
			CreateDBPFEntry(&entry, SearchOffset);
			if(entry.FileID == FileID){
				found++; break;
			}
			SearchOffset	+= MINSIZE_DBPFENTRY;
			SearchSize		-= MINSIZE_DBPFENTRY;
		}
		free(Data);
		if(!found){
			CloseHandle(hFile); return 0;
		}
		
		Data = malloc(entry.DataSize);
		ReadFile(hFile, Data, entry.DataSize, &bytestransferred, NULL);
		CloseHandle(hFile);
		
		if(!(Export&1)){
			*Destination = Data;
			return entry.DataSize;
		}
		
		hFile = CreateFile((const char *) *Destination, GENERIC_WRITE, 0, NULL, (Export&3) ? CREATE_ALWAYS : CREATE_NEW, FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, NULL);
		if(hFile == INVALID_HANDLE_VALUE) return 0;
		
		result = WriteFile(hFile, Data, entry.DataSize, &bytestransferred, NULL);
		CloseHandle(hFile);
		return (result == TRUE && bytestransferred == entry.DataSize) ? entry.DataSize : 0;
		} break;
	default: /* FAR v1 does not make use of File IDs */
		return 0;
	}
}

unsigned int FARExtractItemFromFileByName(const char * archivename, const char * EntryFilename, unsigned char ** Destination, int Export){
	/* Valid for: FARv1, FARv3 */
	
	/* If the last bit of Export is 1, the contents are exported to a file. Then,
	   If the second to last bit is also 1, the file will overwrite any existing files. */
	
	/* The function will return the size of the data if everything goes correctly or 0 if something fails. */
	
	HANDLE hFile;
	BOOL result;
	DWORD filesize, bytestransferred;
	int type;
	LARGE_INTEGER FilePointer;
	unsigned int IndexSize;
	unsigned char header[MINSIZE_FAR], * SearchOffset, * Data, * DecompressedData;
	char DestinationFilename[256];
	unsigned int SearchSize;
	unsigned int item = 0;
	int found = 0;
	if(archivename == NULL || archivename[0] == 0x00 || Destination == NULL){ printf("Bad parameter\n"); return 0; }
	
	hFile = CreateFile(archivename, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
	if(hFile == INVALID_HANDLE_VALUE){ printf("Could not open file for reading\n"); return 0; }
	
	filesize = GetFileSize(hFile, NULL);
	
	if(GetLastError() != ERROR_SUCCESS || filesize < MINSIZE_FAR || ReadFile(hFile, header, 16, &bytestransferred, NULL) == FALSE || bytestransferred != 16){
		CloseHandle(hFile); printf("Could not probe file\n"); return 0;
	}
	
	type = libfarIdentify(header, filesize);
	
	switch(type){
	case FAR_FARV3: {
		struct FAR3File file;
		struct FAR3Entry entry;
		struct RefPackStream rps;
		LONG highbits = 0;
		if(!CreateFAR3File(&file, header, filesize)){
			CloseHandle(hFile); printf("Could not create FAR3 file\n"); return 0;
		}
		SetFilePointer(hFile, file.FAR3Header.IndexOffset, &highbits, FILE_BEGIN);
		
		IndexSize = filesize - file.FAR3Header.IndexOffset;
		Data = (unsigned char *) malloc(IndexSize);
		ReadFile(hFile, Data, IndexSize, &bytestransferred, NULL);
		FARInitialize(&file, FAR_FARV3, (Data[0]<<0) | (Data[1]<<8) | (Data[2]<<16) | (Data[3]<<24), Data, IndexSize);
		printf("Files: %u\n", (Data[0]<<0) | (Data[1]<<8) | (Data[2]<<16) | (Data[3]<<24));
		
		SearchOffset = Data+4;
		SearchSize = file.IndexSize-4;
		
		while(SearchSize>=MINSIZE_FAR3ENTRY && item<file.FAR3Header.FilesCount){
			CreateFAR3Entry(&entry, SearchOffset);
			if(SearchSize < entry.FilenameLength) break;
			SearchOffset	+= MINSIZE_FAR3ENTRY;
			memcpy(entry.Filename, SearchOffset, entry.FilenameLength);
			entry.Filename[entry.FilenameLength] = 0x00;
			if(!strcmp(entry.Filename, EntryFilename)){
				printf("Found it\n");
				found++; break;
			}else{
				printf("Checked %s and looking for %s\n", entry.Filename, EntryFilename);
			}
			item++;
			SearchOffset	+= entry.FilenameLength;
			SearchSize		-= MINSIZE_FAR3ENTRY + entry.FilenameLength;
		}
		free(Data);
		if(!found){
			CloseHandle(hFile); printf("FAR3 entry not found\n"); return 0;
		}
		
		Data = (unsigned char *) malloc(entry.CompressedDataSize), DecompressedData = (unsigned char *) malloc(entry.DecompressedDataSize);
		SetFilePointer(hFile, entry.DataOffset, &highbits, FILE_BEGIN);
		ReadFile(hFile, Data, entry.CompressedDataSize, &bytestransferred, NULL);
		CloseHandle(hFile);
		
		printf("Going to decompress stream\n");
		CreateRefPackStream(&rps, Data);
		RefPackDecompress(&rps, Data+MINSIZE_REFPACK, DecompressedData);
		printf("Decompressed stream\n");
		
		free(Data);
		
		if(!(Export&1)){
			*Destination = DecompressedData;
			return entry.DecompressedDataSize;
		}

		strcpy(DestinationFilename, (const char *) Destination);
		strcat(DestinationFilename, (const char *) EntryFilename);
		hFile = CreateFile(DestinationFilename, GENERIC_WRITE, 0, NULL, (Export&3) ? CREATE_ALWAYS : CREATE_NEW, FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, NULL);
		if(hFile == INVALID_HANDLE_VALUE){ printf("Could not open file for writing\n"); return 0; }
		
		printf("Going to write\n");
		result = WriteFile(hFile, DecompressedData, entry.DecompressedDataSize, &bytestransferred, NULL);
		printf("Written. Going to close\n");
		CloseHandle(hFile);
		return (result == TRUE && bytestransferred == entry.DecompressedDataSize) ? entry.DecompressedDataSize : 0;
		} break;
	case FAR_FARV1: {
		struct FAR1File file;
		struct FAR1Entry entry;
		if(!CreateFAR1File(&file, header, filesize)){
			CloseHandle(hFile); printf("Could not create FAR1 file"); return 0;
		}
		FilePointer.u.LowPart = file.FAR1Header.IndexOffset;
		SetFilePointerEx(hFile, FilePointer, (PLARGE_INTEGER) NULL, FILE_BEGIN);
		ReadFile(hFile, header, 4, &bytestransferred, NULL);
		
		IndexSize = filesize - file.FAR1Header.IndexOffset;
		Data = malloc(IndexSize);
		ReadFile(hFile, Data, IndexSize, &bytestransferred, NULL);
		FARInitialize(&file, FAR_FARV1, (Data[0]<<0) | (Data[1]<<8) | (Data[2]<<16) | (Data[3]<<24), Data, IndexSize);
		
		SearchOffset = file.Index+4;
		SearchSize = file.IndexSize-4;
		
		while(SearchSize>=MINSIZE_FAR1ENTRY && item<file.FAR1Header.FilesCount){
			CreateFAR1Entry(&entry, SearchOffset);
			SearchOffset	+= MINSIZE_FAR1ENTRY;
			if(SearchSize < entry.FilenameLength) break;
			memcpy(entry.Filename, SearchOffset, entry.FilenameLength);
			if(!strcmp(entry.Filename, EntryFilename)){
				found++; break;
			}
			item++;
			SearchOffset	+= entry.FilenameLength;
			SearchSize		-= MINSIZE_FAR1ENTRY + entry.FilenameLength;
		}
		free(Data);
		if(!found){
			CloseHandle(hFile); printf("FAR1 entry not found"); return 0;
		}
		
		Data = malloc(entry.CompressedDataSize);
		ReadFile(hFile, Data, entry.CompressedDataSize, &bytestransferred, NULL);
		CloseHandle(hFile);
		
		if(!(Export&1)){
			*Destination = Data;
			return entry.CompressedDataSize;
		}
		
		strcpy(DestinationFilename, (const char *) Destination);
		strcat(DestinationFilename, (const char *) EntryFilename);
		hFile = CreateFile(DestinationFilename, GENERIC_WRITE, 0, NULL, (Export&3) ? CREATE_ALWAYS : CREATE_NEW, FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, NULL);
		if(hFile == INVALID_HANDLE_VALUE){ printf("Could not open file for writing"); return 0; }
		
		result = WriteFile(hFile, Data, entry.CompressedDataSize, &bytestransferred, NULL);
		CloseHandle(hFile);
		return (result == TRUE && bytestransferred == entry.CompressedDataSize) ? entry.CompressedDataSize : 0;
		} break;
	default: /* DBPF does not make use of filenames */
		return 0;
	}
}
#endif