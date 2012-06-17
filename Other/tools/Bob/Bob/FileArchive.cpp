#include "stdafx.h"
#include "FileArchive.h"
#include <assert.h>
#include <iostream>
#include "Compress.h"
#include "FreeImage.h"

namespace Archives
{
	string Replace(string Str, string From, string To);

	FileArchive::FileArchive(string Path)
	{
		char Signature[8];
		char Version[4];
		char ManifestOffset[4];
		char NumFiles[4];

		m_ArchivePath = Path;

		ifstream Archive(Path.c_str(), ios::in | ios::binary);
		Archive.read(Signature, sizeof(FARSignature));

		string StrSignature(Signature, Signature + 8);

		assert(StrSignature == "FAR!byAZ");
		cout << "Signature: " << StrSignature << "\r\n";
		
		Archive.read(Version, 4);
		m_Version = *((int*)Version);

		assert(m_Version == 1 || 3);

		Archive.read(ManifestOffset, 4);
		m_ManifestOffset = *((unsigned int*)ManifestOffset);
		
		Archive.seekg(m_ManifestOffset, ios::beg);

		Archive.read(NumFiles, 4);
		m_NumFiles = *((unsigned int*)NumFiles);

		Archive.close();
	}

	void FileArchive::Process()
	{
		ifstream Archive(m_ArchivePath.c_str(), ios::in | ios::binary);

		char DecompressedDataSize[4];
		char CompressedDataSize[3];
		char DataType[1];
		char DataOffset[4];
		char Compressed[1];
		char AccessNumber[1];
		char FilenameLength[2];
		char TypeID[4];
		char FileID[4];
		char* Filename;
		
		//Seek past the number of files...
		Archive.seekg(m_ManifestOffset + 4, ios::beg);

		for(unsigned int i = 0; i < m_NumFiles; i++)
		{
			FAR3Entry Entry;
			
			Archive.read(DecompressedDataSize, sizeof(unsigned int));
			Entry.DecompressedDataSize = *((unsigned int*)DecompressedDataSize);

			Archive.read(CompressedDataSize, 3);
			Entry.CompressedDataSize = *((unsigned int*)CompressedDataSize);

			Archive.read(DataType, sizeof(unsigned char));
			Entry.DataType = *((unsigned char*)DataType);

			Archive.read(DataOffset, sizeof(unsigned int));
			Entry.DataOffset = *((unsigned int*)DataOffset);

			Archive.read(Compressed, sizeof(unsigned char));
			Entry.Compressed = *((unsigned char*)Compressed);

			Archive.read(AccessNumber, sizeof(unsigned char));
			Entry.AccessNumber = *((unsigned char*)AccessNumber);

			Archive.read(FilenameLength, sizeof(unsigned short));
			Entry.FilenameLength = *((unsigned short*)FilenameLength);

			Archive.read(TypeID, sizeof(unsigned int));
			Entry.TypeID = *((unsigned int*)TypeID);

			Archive.read(FileID, sizeof(unsigned int));
			Entry.FileID = *((unsigned int*)FileID);

			Filename = new char[Entry.FilenameLength];
			Archive.read(Filename, Entry.FilenameLength);
			memcpy(&Entry.Filename, Filename, Entry.FilenameLength);

			string StrFilename(Filename, Filename + Entry.FilenameLength);
			cout << "Filename: " << StrFilename << "\r\n";

			m_Entries.push_back(Entry);

			char* Buffer = new char[Entry.DecompressedDataSize];
			
			//Don't convert tgas...
			//if(StrFilename.find(".tga") == string::npos)
			//{
				FARExtractItemFromFileByName(m_ArchivePath.c_str(), StrFilename.c_str(), 
					(unsigned char**)&Buffer, 1);

				FREE_IMAGE_FORMAT Fif = FreeImage_GetFIFFromFilename(StrFilename.c_str());
				
				if(Fif != FIF_UNKNOWN)
				{
					FIBITMAP *Img = FreeImage_Load(Fif, StrFilename.c_str());
					
					/*FIBITMAP *Tmp = Img;
					Img = FreeImage_ConvertTo32Bits(Tmp);
					FreeImage_Unload(Tmp);*/

					//FreeImage_SetTransparent(Img, true);

					if(StrFilename.find(".tga") == string::npos)
						FreeImage_Save(FREE_IMAGE_FORMAT::FIF_PNG, Img, Replace(StrFilename, ".bmp", ".png").c_str());
					else
						FreeImage_Save(FREE_IMAGE_FORMAT::FIF_PNG, Img, Replace(StrFilename, ".tga", ".png").c_str());

					remove(StrFilename.c_str());
					FreeImage_Unload(Img);
				}
			//}

			delete Buffer;
		}

		RecreateArchive();
	}

	void FileArchive::RecreateArchive()
	{
		unsigned int Version = 3;
		unsigned int ManifestOffset = 0x00;
		
		//Out Of Bounds - part of the RefPak header.
		unsigned char OOBChar = 0x00;

		ofstream Archive(Replace(m_ArchivePath, ".dat", ".tmp").c_str(), ios::binary);
		Archive << "FAR!byAZ";
		Archive.write((const char*)&Version, 4);
		Archive.write((const char*)&ManifestOffset, 4);
		Archive.flush();

		for(unsigned int i = 0; i < m_NumFiles; i++)
		{
			char* Filename = new char[m_Entries[i].FilenameLength];
			memcpy(Filename, &m_Entries[i].Filename, m_Entries[i].FilenameLength);
			string StrFilename(Filename, Filename + m_Entries[i].FilenameLength);

			if(StrFilename.find(".tga") == string::npos)
				StrFilename = Replace(StrFilename, ".bmp", ".png");
			else
				StrFilename = Replace(StrFilename, ".tga", ".png");
			
			//Replace the filename of the current entry.
			for(unsigned int j = 0; j < StrFilename.length(); j++)
				m_Entries[i].Filename[j] = StrFilename[j];

			ifstream CurrentFile(StrFilename.c_str(), ios::in | ios::binary | ios::ate);
			//CurrentFile.seekg(0, ios::end);
			ifstream::pos_type size = CurrentFile.tellg();

			unsigned char *MemBlock = new unsigned char[size];
			CurrentFile.seekg(0, ios::beg);
			CurrentFile.read((char*)MemBlock, size);

			unsigned char* Dst = mynew<unsigned char>((int)size - 1);
			unsigned char* DstEnd = compress((unsigned char*)MemBlock, (unsigned char*)MemBlock + size, 
				Dst, Dst + size - 1, false);
			
			cout << "Bob the builder, can he compress it? \r\n"; 

			//Write the compressed...
			if(DstEnd)
			{
				unsigned int DstLength = DstEnd - Dst;

				//TODO: Write FAR3 Compression header.
				cout << "Yes he can!\r\n\r\n";
				
				m_Entries[i].Compressed = 0x01;
				//Update the offset for the location of the data!
				m_Entries[i].DataOffset = Archive.tellp();
				//Update the compressed size!
				m_Entries[i].CompressedDataSize = DstLength;
				//Update the uncompressed size!
				m_Entries[i].DecompressedDataSize = size;
				
				Archive.write((const char*)&m_Entries[i].Compressed, 1);

				unsigned char Byte1 = m_Entries[i].DecompressedDataSize & 0xff;
				unsigned char Byte2 = (m_Entries[i].DecompressedDataSize >> 8) & 0xff;
				unsigned char Byte3 = (m_Entries[i].DecompressedDataSize >> 16) & 0xff;

				Archive.write((const char*)&Byte1, 1);
				Archive.write((const char*)&Byte2, 1);
				Archive.write((const char*)&Byte3, 1);

				Archive.write((const char*)&OOBChar, 1);
				//StreamBodySize - Does not include the size of the RefPak header.
				Archive.write((const char*)&DstLength - 18, 4);

				for(unsigned int i = 0; i < DstLength; i++)
					Archive.write((const char*)&Dst[i], 1);
			}
			else //... or uncompressed file.
			{
				cout << "No he can't!\r\n\r\n";
				
				m_Entries[i].Compressed = 0x00;
				//Update the offset for the location of the data!
				m_Entries[i].DataOffset = Archive.tellp();
				//Keep the compressed size same as the decompressed size since the entry isn't compressed.
				m_Entries[i].CompressedDataSize = size;
				//Update the uncompressed size!
				m_Entries[i].DecompressedDataSize = size;
				
				Archive.write((const char*)&m_Entries[i].Compressed, 1);

				unsigned char Byte1 = m_Entries[i].DecompressedDataSize & 0xff;
				unsigned char Byte2 = (m_Entries[i].DecompressedDataSize >> 8) & 0xff;
				unsigned char Byte3 = (m_Entries[i].DecompressedDataSize >> 16) & 0xff;

				Archive.write((const char*)&Byte1, 1);
				Archive.write((const char*)&Byte2, 1);
				Archive.write((const char*)&Byte3, 1);

				Archive.write((const char*)&OOBChar, 1);
				//StreamBodySize - Does not include the size of the RefPak header.
				Archive.write((const char*)&size - 9, 4);

				for(int i = 0; i < size; i++)
					Archive.write((const char*)&MemBlock[i], 1);
			}
			
			delete Dst;
			delete MemBlock;
			delete Filename;
			CurrentFile.close();

			remove(StrFilename.c_str());
		}

		ManifestOffset = Archive.tellp();
		Archive.seekp(12, ios::beg);
		Archive.write((const char*)&ManifestOffset, 4);
		Archive.seekp(ManifestOffset, ios::beg);
		
		unsigned int NumEntries = m_Entries.size();
		Archive.write((const char*)&NumEntries, 4);

		for(unsigned int i = 0; i < m_NumFiles; i++)
		{
			Archive.write((const char*)&m_Entries[i].DecompressedDataSize, 4);

			unsigned char Byte1 = m_Entries[i].CompressedDataSize & 0xff;
			unsigned char Byte2 = (m_Entries[i].CompressedDataSize >> 8) & 0xff;
			unsigned char Byte3 = (m_Entries[i].CompressedDataSize >> 16) & 0xff;

			Archive.write((const char*)&Byte1, 1);
			Archive.write((const char*)&Byte2, 1);
			Archive.write((const char*)&Byte3, 1);

			Archive.write((const char*)&m_Entries[i].DataType, 1);
			Archive.write((const char*)&m_Entries[i].DataOffset, 4);
			Archive.write((const char*)&m_Entries[i].Compressed, 1);
			Archive.write((const char*)&m_Entries[i].AccessNumber, 1);
			Archive.write((const char*)&m_Entries[i].FilenameLength, 2);
			Archive.write((const char*)&m_Entries[i].TypeID, 4);
			Archive.write((const char*)&m_Entries[i].FileID, 4);
			
			for(unsigned int j = 0; j < m_Entries[i].FilenameLength; j++)
				Archive << m_Entries[i].Filename[j];
		}

		Archive.close();
	}

	string Replace(string Str, string From, string To)
	{
		int Position = Str.find(From);

		while(Position != string::npos)
		{
			Str = Str.replace(Position, From.length(), To);
			Position = Str.find(From, Position + From.length());
		}

		return Str;
	}
}