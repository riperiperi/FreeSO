// Bob.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <string>
#include <iostream>
#include "FileArchive.h"
#include "FreeImage.h"
//#include "libfar.h"

using namespace std;
using namespace Archives;

int _tmain(int argc, _TCHAR* argv[])
{
	//Only call when linking statically!
	FreeImage_Initialise();

	FileArchive Archive("personselection.dat");
	Archive.Process();

	cin.get();

	FreeImage_DeInitialise();

	return 0;
}

