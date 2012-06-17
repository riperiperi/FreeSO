#ifndef __FILEARCHIVE_H__
#define __FILEARCHIVE_H__

#include <string>
#include <fstream>
#include <vector>
#include "libfar.h"
using namespace std;

namespace Archives
{
	const char FARSignature [] = { 'F', 'A', 'R', '!', 'b', 'y', 'A', 'Z' };

	class FileArchive
	{
	private:
		string m_ArchivePath;

		int m_Version;					//Version of archive.
		unsigned int m_ManifestOffset;	//Offset to the manifest.

		unsigned int m_NumFiles;		//Number of files in the archive.
		vector<FAR3Entry> m_Entries;

		void RecreateArchive();
	public:
		FileArchive(string Path);
		void Process();
		int ArchiveVersion() { return m_Version; }
	};
}

#endif