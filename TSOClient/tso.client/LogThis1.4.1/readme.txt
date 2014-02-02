#region Copyright © 2003 Dave Lewis [logthis@tpsd.com]
/*
 * 6/15/2003
 * This software is provided 'as-is', without any express or implied warranty.
 * In no event will the author(s) be held liable for any damages arising from
 * the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 *   1. The origin of this software must not be misrepresented; you must not
 *      claim that you wrote the original software. If you use this software
 *      in a product, an acknowledgment in the product documentation would be
 *      appreciated but is not required.
 * 
 *   2. Altered source versions must be plainly marked as such, and must not
 *      be misrepresented as being the original software.
 * 
 *   3. This notice may not be removed or altered from any source distribution.
 */ 
#endregion

LogThis is a C# class file intended to be simply dropped into any c# project. 
It is written so that it can be invoked from anywhere in the project without 
creating an instance of the object.  In other words, Log.LogThis doesn't require
you to create an instance of Log before using it. 
While an instance of Log is created (probably in Main()) it is only needed long
enough to run Log.Init and then the instance can be destroyed.  This is the
closest to having global methods as can be had in c#.  Enjoy.

The main functionality of the object is Log.LogThis(string logtext,eloglevel loglevel)
where you pass the log message and a log level. You also set the Log.LogLevel value.
The log levels are (error,warn,info and verbose).  
For instance, if you set Log.LogLevel = eloglevel.warn then Log.LogThis("test",eloglevel.info)
would not get logged.  A Log.LogLevel = warn means logs which have a value of warn or error
will get logged.   A Log.Loglevel = Info means logs which have a value of Info, Warn or error
will get logged.  This allows  you to pepper your application with log messages aimed at
a specific level of criticality and the the Log.LogLevel determines which will be actually
logged.  This is a great benefit when you want to turn debugging (verbose) on or off during
runtime. 


[Methods]:
Log.Init()
Log.Init(elogprofile logprofile)
Log.LogHeader(elogprofile logprofile, string sText,elogheaderlevel logheaderlevel)
Log.GetProfile(elogprofile logprofile)
Log.LogReset()
Log.LogReset(elogprofile logprofile)
Log.SetLogPath()
Log.SetLogPath(elogprofile logprofile)
Log.LogThis(string logtext,eloglevel loglevel)
Log.LogThis(string logtext,eloglevel loglevel, elogprefix logprefix)
Log.LogThis(elogprofile logprofile, string logtext,eloglevel loglevel)
Log.LogThis(elogprofile logprofile, string logtext,eloglevel loglevel, elogprefix logprefix)
Log.UseSensibleDefaults()
Log.UseSensibleDefaults(eloglevel logLevel)
Log.UseSensibleDefaults(string logfileName, string logLocation, eloglevel logLevel)

[Properties]:
Log.LogLevel
Log.LogName
Log.LogNameFormat
Log.LogPath
Log.LogPeriod
Log.LogQuotaFormat
Log.LogSizeMax
Log.LogWhere
Log.ProcessName
Log.Profile
Log.LogPrefix
Log.LogPath


*new* in this version:  I have created a new method Log.UseSensibleDefaults() 
and it now does mostly what you see below in the Example#2.  This is a very 
sensible configuration for most cases and what I use almost all the time. 

Example #1:

	Step 1:  Initialize the logging system by calling Log.UseSensibleDefaults(...).

	Log.UseSensibleDefaults(); 	Typical production settings. Log file 
					will be "<executing assemblyname>.log"
					and will be written in folder where 
					it's executing out of.  Errors, warnings
					and Log headers will be logged.
	** OR **
	Log.UseSensibleDefaults(logLevel.verbose);  //Typical dev/test setting.

	** OR **
	Log.UseSensibleDefaults(logFileName, logLocation, logLevel);  	sometimes I 
									override the 
									log filename.

	Step 2:  Sprinkle Log.LogThis(...) around your project as you see fit.

	//Typical logging for me.  Headers are optional, if you don't need 
	//them then don't use them.
	Log.LogHeader(Log.LogName + ": Starting up",elogheaderlevel.Level_1);
	...
	Log.LogThis("Main: " + e.Message,eloglevel.error);
	...
	Log.LogHeader(Log.LogName + ": Exiting",elogheaderlevel.Level_1);


Example #2:
	Using LogThis:

	static void Main(string[] args)
	{
		//create an instance of Log()
		Log Mylog = new Log();
		//init the profiles you intend to use 
		Mylog.Init();                    //primary profile
		Mylog.Init(elogprofile.system);  //a second profile if you need one.
		//get rid of the instance
		Mylog = null;
		
		//Set the properties for the primary log profile.
		Log.Profile = elogprofile.primary;
		Log.ProcessName = "IMAP-Agent";
		Log.LogLevel = eloglevel.info;
		Log.LogWhere = elogwhere.file;
		Log.LogName = "dave";
		Log.LogQuotaFormat = elogquotaformat.kbytes;
		Log.LogSizeMax = 100;
		Log.LogPeriod = elogperiod.week;
		Log.LogNameFormat = elognameformat.date_name;
		Log.SetLogPath();
		Console.WriteLine(Log.GetProfile(elogprofile.primary).LogPeriod);
		elogquotaformat t = Log.GetProfile(elogprofile.primary).LogQuotaFormat;
		
		Log.LogHeader("myHeader",elogheaderlevel.Level_1);
		Log.LogThis("this is a test",eloglevel.info);
		Log.Profile = elogprofile.system;
		Log.LogLevel = eloglevel.verbose;
		Log.LogWhere = elogwhere.file;
		Log.LogName = "dave2";
		Log.LogThis(elogprofile.primary,"this is a another test",eloglevel.info);
		Log.LogThis(elogprofile.system,"second test",eloglevel.verbose);
