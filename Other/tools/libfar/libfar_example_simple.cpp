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

unsigned long indexposition;
unsigned long filescount;

int main(int argc, char *argv[]){
	int i;
	///
	/// Check the arguments
	///
	/* Nevermind, I'll keep it even simpler! No arguments, just hardcoded decompress options */
	
	///
	/// Attempt to open the file
	///
	const char * destination_prefix = "output/";
	printf("%u", FARExtractItemFromFileByName("cpanel.dat", "actionmad.bmp", (unsigned char **) destination_prefix, 1));

	return 0;
}