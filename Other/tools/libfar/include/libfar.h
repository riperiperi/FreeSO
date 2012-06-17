/*
  This file is a part of libfar and is licensed under the X11 License.
*/
/*
  The following constants are recognized by libfar:
	* FAR_DEBUG		- Print verbose debugging messages
	* FAR_EMBEDDED	- Enable additional functionality for compatibility with 32-bit+ embedded devices
	* FAR_WIN32		- Enable additional functionality for use on Windows
	* FAR_POSIX		- Enable additional functionality for use on a Unix-like operating system
  Define your constants at the top of this header file [or any source (.c) files that include it] to use them.
*/
#define FAR_WIN32

#define LIBFAR_VERSION "0.3.1"
#define LIBFAR_VERSION_A 0
#define LIBFAR_VERSION_B 3
#define LIBFAR_VERSION_C 1

#ifdef FAR_DEBUG
#include <stdio.h>
#endif

#ifdef FAR_WIN32
#define WIN32_LEAN_AND_MEAN
#include "windows.h"
#endif

/* Used for Visual Studio */
#define _CRT_SECURE_NO_WARNINGS
#undef AFX_DATA
#define AFX_DATA AFX_EXT_DATA

enum FARTYPE {
	FAR_INVALID	= 0,
	FAR_FARV1	= 1,
	FAR_FARV3	= 2,
	FAR_DBPF	= 4
};

#define MINSIZE_FAR1FILE 20
#define MINSIZE_FAR3FILE 20
#define MINSIZE_DBPFFILE 56

#define MINSIZE_FAR1ENTRY 16
#define MINSIZE_FAR3ENTRY 24
#define MINSIZE_DBPFENTRY 20

#define MINSIZE_FAR			MINSIZE_FAR1FILE
#define MAXMINSIZE_FAR		MINSIZE_DBPFFILE
#define MINSIZE_FARENTRY	MINSIZE_FAR1ENTRY
#define MAXMINSIZE_FARENTRY	MINSIZE_FAR3ENTRY

#define MINSIZE_REFPACK 18

const unsigned char FAR1header[] = {'F','A','R','!','b','y','A','Z',0x01,0x00,0x00,0x00};
const unsigned char FAR3header[] = {'F','A','R','!','b','y','A','Z',0x03,0x00,0x00,0x00};
const unsigned char DBPFheader[] = {'D','B','P','F', 0x01,0x00,0x00,0x00, 0x00,0x00,0x00,0x00};

struct FAR1File {
	struct {
		unsigned int IndexOffset;
		unsigned int FilesCount;
	} FAR1Header;
	
	unsigned char * Index; /* Including the 4 bytes for FilesCount */
	unsigned int IndexSize;
};

struct FAR3File {
	struct {
		unsigned int IndexOffset;
		unsigned int FilesCount;
	} FAR3Header;
	
	unsigned char * Index; /* Including the 4 bytes for FilesCount */
	unsigned int IndexSize;
};

struct DBPFFile {
	struct {
		unsigned int DateCreated;
		unsigned int DateModified;
		unsigned int IndexMajorVersion; /* Normally is equal to 7 */
		unsigned int FilesCount;
		unsigned int IndexOffset;
		unsigned int IndexSize;
		unsigned int HoleCount;
		unsigned int HoleIndexOffset;
		unsigned int HoleIndexSize;
		unsigned int IndexMinorVersion; /* Normally is equal to 0 */
		unsigned int FirstEntryOffset;
	} DBPFHeader;
	
	unsigned char * Index;
};

struct FAR1Entry {
	unsigned int DecompressedDataSize;
	unsigned int CompressedDataSize; /* Note that FARv1 files NORMALLY aren't compressed, so this value should be the same as DecompressedDataSize */
	unsigned int DataOffset; /* Relative to the beginning of the FAR file */
	unsigned int FilenameLength; /* Note that the file name does not terminate with a null character */
	char Filename[256];
};
struct FAR3Entry {
	unsigned int DecompressedDataSize;
	unsigned int CompressedDataSize; /* Only 3 bytes large; refers to the total data's size including the RefPack header */
	unsigned char DataType; /* Normally equals 0x80 to denote that the data is in a RefPack container */
	unsigned int DataOffset; /* Relative to the beginning of the FAR file */
	unsigned char Compressed; /* Normally equals 0x01 */
	unsigned char AccessNumber; /* Refers to the number of times that the data at the specified offset has been used for other entries: Normally equals 0x00 (and works its way up) */
	unsigned short FilenameLength; /* Note that the file name does not terminate with a null character */
	unsigned int TypeID;
	unsigned int FileID;
	char Filename[256];
};

struct DBPFEntry {
	unsigned int TypeID;
	unsigned int GroupID;
	unsigned int FileID;
	unsigned int DataOffset;
	unsigned int DataSize;
};

struct RefPackStream {
	struct {
		unsigned char Compressed; /* Normally equals 0x01 */
		unsigned int DecompressedDataSize; /* Only 3 bytes large */
		unsigned char OoBChar; /* Out of Bounds character: Normally equals 0x00 */
		unsigned int StreamBodySize; /* Does not include the size of the RefPack header */
		unsigned int CompressedFileSize; /* Should be the same value as StreamBodySize */
		unsigned short CompressionMethod; /* Normally equals 0x10,0xFB to refer to the QFS compressor */
		unsigned int DecompressedFileSizeFlippedEndian; /* Only 3 bytes large. And I'll say it:
															why is the decompressed size 3 bytes large when the compressed size gets to be 4?
															EA does this kind of thing all the time. */
	} RefPackHeader;
};

#ifdef __cplusplus
extern "C" {
#endif

	enum FARTYPE libfarIdentify(const unsigned char * buffer, unsigned int filesize);
	int CreateFAR1File(struct FAR1File * File, const unsigned char * buffer, unsigned int filesize);
	int CreateFAR3File(struct FAR3File * File, const unsigned char * buffer, unsigned int filesize);
	int CreateDBPFFile(struct DBPFFile * File, const unsigned char * buffer, unsigned int filesize);
	int FARInitialize(void * File, enum FARTYPE ArchiveType, unsigned int FilesCount, unsigned char * Index, unsigned int IndexSize);
	int CreateFAR1Entry(struct FAR1Entry * Entry, const unsigned char * buffer);
	int CreateFAR3Entry(struct FAR3Entry * Entry, const unsigned char * buffer);
	int CreateDBPFEntry(struct DBPFEntry * Entry, const unsigned char * buffer);
	int CreateRefPackStream(struct RefPackStream * Stream, const unsigned char * buffer);
	int RefPackDecompress(const struct RefPackStream * Stream, const unsigned char * inbuffer, unsigned char * outbuffer);
	#if (defined FAR_WIN32 || defined FAR_POSIX)
	unsigned int FARExtractItemFromFileByID(const char * archivename, unsigned int FileID, unsigned char ** Destination, int Export);
	unsigned int FARExtractItemFromFileByName(const char * archivename, const char * EntryFilename, unsigned char ** Destination, int Export);
	#endif

#ifdef __cplusplus
}
#endif 

/* Used for Visual Studio */
#undef AFX_DATA
#define AFX_DATA