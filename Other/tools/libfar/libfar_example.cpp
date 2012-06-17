/*
  This file is a part of libfar_example and is published directly into the public domain.
*/

#define _CRT_SECURE_NO_WARNINGS
#include <windows.h>
#include <stdio.h>
#include <string.h>
#include "include/libfar.h"
#include "errors.h"

int overwrite = 0;

char infile[256], outdirectory[256];
unsigned char readbuffer[256], writebuffer[256];
DWORD bytesread = 0, byteswritten = 0;
DWORD FARfilesize;

unsigned int indexposition;
unsigned int filescount;

int main(int argc, char *argv[]){
        int i;
        ///
        /// Check the arguments
        ///
        
        if(argc == 2 && argv[1][0] == '-' && (argv[1][1] == 'h' || strcmp(argv[1]+1, "-help") == 0)){
                printf("libfar version 1.0.0\n\n"
                        "Usage: libfar [-f] INFILE OUTDIRECTORY\n"
                        "  or:  libfar [-f] OUTDIRECTORY to read the input file from standard input\n"
                        "  or:  libfar [-f] to read the input file from standard input and\n"
                        "       write the output files to the current working directory\n\n"
                        "Use the -f option to force overwriting of files without confirmation.");
                return 0;
        }
        
        i = 1;
        if(argc > 1 && argv[1][0] == '-' && argv[1][1] == 'f' && argv[1][2] == 0x00){
                overwrite = 1;
                i++;
        }
        if(i == argc){ //Read from standard input
                printf("%sReading from standard input is not yet implemented.", "libfar: error: ");
                return ERROR_UNIMPLEMENTED;
        }
        for(; i<argc; i++){
                if(infile[0] == 0x00){
                        if(argc-overwrite > 2){ //We have an infile
                                strcpy(infile, argv[i]);
                        }else{ //Read from standard input
                                printf("%sReading from standard input is not yet implemented.", "libfar: error: ");
                                return ERROR_UNIMPLEMENTED;
                        }
                        continue;
                }
                strcpy(outdirectory, argv[i]);
                break;
        }
        
        ///
        /// Attempt to open the file
        ///
        HANDLE hFAR = CreateFile(infile, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
        if(hFAR == INVALID_HANDLE_VALUE){
                if(GetLastError() == ERROR_FILE_NOT_FOUND){
                        printf("%sThe specified input file does not exist.", "libfar: error: ");
                        return ERROR_FILENOTFOUND;
                }
                printf("%sThe input file could not be opened for reading.", "libfar: error: ");
                return ERROR_COULDNOTREAD;
        }
        FARfilesize = GetFileSize(hFAR, NULL);
        if(FARfilesize < 20){
                printf("%sNot a valid FAR archive.", "libfar: error: ");
                return ERROR_NOTVALIDFAR;
        }
        if(!ReadFile(hFAR, readbuffer, 16, &bytesread, NULL) || bytesread != 16){
                printf("%sThe input file could not be read.", "libfar: error: ");
                return ERROR_COULDNOTREAD;
        }
        
        struct FAR3File archive;
        if(!CreateFAR3File(&archive, readbuffer, FARfilesize)){
                printf("%sNot a valid FAR archive.", "libfar: error: ");
                return ERROR_NOTVALIDFAR;
        }
        //That's it as far as FAR archive validity checking goes.
        
        ///
        /// Jump to the file table of the FAR file
        ///
        SetFilePointer(hFAR, archive.FAR3Header.IndexOffset, NULL, FILE_BEGIN);
        ReadFile(hFAR, readbuffer, 4, &bytesread, NULL);
        indexposition = archive.FAR3Header.IndexOffset+4;
        filescount = (readbuffer[0]<<0) | (readbuffer[1]<<8) | (readbuffer[2]<<16) | (readbuffer[3]<<24);
        printf("This FARv3 archive contains X MiB (Y bytes) in %u files.\n\nExtracting\n", filescount);
        DWORD beginningtime = GetTickCount();
        
        unsigned int file, filesextracted = 0;
        
        ///
        /// Create the memory buffers to hold the compressed and decompressed data
        ///
        unsigned char * CompressedData   = (unsigned char *) malloc(16777216); //pow(2,24) bytes; 16 MiB
        unsigned char * DecompressedData = (unsigned char *) malloc(16777216);
        
        ///
        /// Extract each item
        ///
        for(file = 1; file <= filescount; file++){
                ///
                /// Read the file's attributes
                ///
                ReadFile(hFAR, readbuffer, 24, &bytesread, NULL);
                struct FAR3Entry entry;
                CreateFAR3Entry(&entry, readbuffer);
                ReadFile(hFAR, entry.Filename, entry.FilenameLength, &bytesread, NULL);
                entry.Filename[entry.FilenameLength] = 0x00; //Insert the null-terminating character
                indexposition += 4+2+1+1+4+2+2+4+4 + entry.FilenameLength; //Record our current location in the FAR file so later we can skip over to the file's data and be able to return
                
                ///
                /// Open the new file for writing
                ///
                char destination[256];
                strcpy(destination, outdirectory);
                strcat(destination, "/");
                strcat(destination, entry.Filename);
                HANDLE hFile;
                hFile = CreateFile(destination, GENERIC_WRITE, 0, NULL, CREATE_NEW+overwrite, FILE_ATTRIBUTE_NORMAL, NULL);
                if(hFile == INVALID_HANDLE_VALUE){
                        printf(" (%d/%u) Skipped (%s): %s\n", file, filescount, (!overwrite && GetLastError() == ERROR_FILE_EXISTS) ? "file exists" : "could not open", entry.Filename);
                        continue;
                }
                
                ///
                /// Jump to the file's contents
                ///
                SetFilePointer(hFAR, entry.DataOffset, NULL, FILE_BEGIN);
                ReadFile(hFAR, readbuffer, 18, &bytesread, NULL);
                
                ///
                /// Read the RefPack stream's attributes
                ///
                struct RefPackStream stream;
                CreateRefPackStream(&stream, readbuffer);
                
                ///
                /// Read in the RefPack stream
                ///
                ReadFile(hFAR, CompressedData, stream.RefPackHeader.CompressedFileSize, &bytesread, NULL);
                
                ///
                /// Begin the decompression
                ///
                printf(" (%u/%u) %s (%u bytes)\n", file, filescount, entry.Filename, entry.DecompressedDataSize);
                RefPackDecompress(&stream, CompressedData, DecompressedData);
                
                ///
                /// Write the result to the file
                ///
                BOOL result = WriteFile(hFile, DecompressedData, stream.RefPackHeader.DecompressedDataSize, &bytesread, NULL);
                
                /* Extract using RefPackBufferedDecompress
                ///
                /// Run the decompression loop
                ///
                ReadFile(hFAR, readbuffer, 512, &bytesread, NULL);
                int status;
                while(true){
                        status = RefPackBufferedDecompress(&stream, readbuffer, bytesread, writebuffer, 512);
                        if((status&0x0F) == 0) break; //Decompression is done
                        if(status == 0x00000001){ //We need to advance the in buffer
                                ReadFile(hFAR, readbuffer, 512, &bytesread, NULL);
                        }else if(status == 0x00000002){ //We need to write our data so we can advance the out buffer
                                WriteFile(hFile, writebuffer, stream.DataWritten, &byteswritten, NULL);
                        }else{ //We need to refer back to data that we have already written
                                unsigned int size = (status&0xFFF00000)>>20;
                                if(size > 512) size = 512;
                                SetFilePointer(hFile, -1*stream.OutOffset, NULL, FILE_END);
                                ReadFile(hFile, readbuffer, 512, &bytesread, NULL);
                                SetFilePointer(hFile, 0, NULL, FILE_END);
                                if((status&0xF00) == 0x200){ //Additionally, we need to advance the out buffer
                                        WriteFile(hFile, writebuffer, stream.DataWritten, &byteswritten, NULL);
                                }
                        }
                } */
                
                /* Extract without decompressing (to replace the "Run the compression loop" section)
                unsigned char * filedata = (unsigned char *) malloc(entry.CompressedFileSize);
                ReadFile(hFAR, filedata, entry.CompressedFileSize, &bytesread, NULL);
                
                BOOL result = WriteFile(hFile, filedata, entry.CompressedFileSize, &bytesread, NULL);
                CloseHandle(hFile);
                SetFilePointer(hFAR, indexposition, NULL, FILE_BEGIN);
                
                if(!result || byteswritten != entry.CompressedFileSize){
                        printf(" (%d/%u) Skipped (%s): %s\n", file, filescount, "could not write", entry.Filename);
                        continue;
                }
                */
                
                ///
                /// Close up
                ///
                CloseHandle(hFile);
                SetFilePointer(hFAR, indexposition, NULL, FILE_BEGIN);
                
                if(!result)
                        printf("Could not write the file.\n");
                else filesextracted++;
        }
        
        ///
        /// The extraction should be complete now
        ///
        CloseHandle(hFAR);
        printf("\nFinished extracting %d of %u files in %.2f seconds.", filesextracted, filescount, ((float) (GetTickCount() - beginningtime))/1000);

        return 0;
}