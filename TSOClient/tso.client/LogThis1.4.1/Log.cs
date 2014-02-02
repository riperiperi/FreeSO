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

using System;
using System.IO;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
// LogThis version 1.4.1

namespace LogThis
{
	/// <summary>
	/// Notes to any developer who uses this class:
	/// The elogprofile enum is the only part of this
	/// class that will require modification for your
	/// particular applications use.  
	/// The LogThis logger class supports log profiles
	/// allowing multiple logging strategies when desired.
	/// One example:
	///		A primary log file for customer consumption and
	///		then a second log file for when you need to do
	///		debugging.  Some developers prefer to have debugging
	///		information go to a second file for any number of
	///		reasons.  In that case you would add some other enum
	///		value to the elogprofile enum.  You would have any
	///		normal logging use the primary log profile and any debugging
	///		logging use the other profile.  
	///		This is just a simple example.  You can have any number of
	///		logging profiles based on your requirements just by adding
	///		them.  See the tutorial on using multiple profiles for more
	///		information.
	/// </summary>
	public enum elogprofile
	{
		primary = 1
			,system
	}
	public enum eloglevel
	{
		error=1
		,warn
		,info
		,verbose
	}
	public enum elogwhere
	{
		eventlog
		,file
		,eventlog_and_file
	}
	public enum elogprefix
	{
		none
		,dt
		,loglevel
		,dt_loglevel
	}
	public enum elogheaderlevel
	{
		Level_1
		,Level_2
		,Level_3
	}
	public enum elogperiod
	{
		none
		,day
		,week
		,month
	}
	public enum elognameformat
	{
		name_date
		,date_name
	}
	public enum elogquotaformat
	{
		no_restriction
		,kbytes
		,rows
	}

	
	public class Log
	{
		/*
		 * Some methods are overloaded, allowing the caller to specify the log profile.
		 * None of the properties support the caller specifying the log profile.  
		 * In the case of properties, it is necessary to set Log.Profile to a valid
		 * log profile and then address the properties.  
		 * Properties:
		 *				LogLevel:
		 *				LogWhere:
		 *				LogName:
		 *				Profile: This property contains the currently chosen log profile.
		 * 
		 * DefaultToPrimaryProfile:
		 *			True:	When true and when the log profile isn't explicitly specified
		 *					then the primary log profile will be implied.
		 *					Note: The value of True is only supported when just a single
		 *					log profile exists.  Creating additional log profiles will 
		 *					automatically set this to false.  A value of True makes it
		 *					simplier to work with when using a single log profile because
		 *					all methods and properties default to the primary profile.
		 *			False:	The profile to be used will be the most recently
		 *					chosen one that was set using Log.Profile = elogprofile.YourProfile
		 * 
		 */
		private static ListDictionary m_Logs;
		private static LogMethods m_LogMethods;
		private static elogprofile m_logprofile;
		private static bool m_DefaultToPrimaryProfile = true;
		public static void LogThis(string logtext,eloglevel loglevel)
		{
			if (m_DefaultToPrimaryProfile)
			{
				LogThis(elogprofile.primary,logtext,loglevel);
			}
			else //log to current profile
			{
				m_LogMethods.LogThis(logtext,loglevel);
			}

		}
		public static void UseSensibleDefaults()
		{
			/* 
			 * eloglevel.info allows LogHeaders, warnings and errors to show up in log.
			*/
			UseSensibleDefaults(eloglevel.info);
		}
		public static void UseSensibleDefaults(eloglevel logLevel)
		{
			/*
			 * Uses default values for the log filename and path.
			 * 
			*/
			string[] logFile = DefaultLogFileAndLocation();
			UseSensibleDefaults(logFile[1],logFile[0],logLevel);
		}
		public static void UseSensibleDefaults(string logFileName, string logLocation, eloglevel logLevel)
		{
			//create an instance of Log()
			Log Mylog = new Log();
			//init the profiles you intend to use. 
			/*
				The profile concept is the only complicated thing about LogThis and unless
				you have some need to log in multiple different ways *in the same project*
				then just default to the primary profile.  All the settings you see below are 
				configuring the primary profile.  To have multiple profiles you would 
				call Mylog.Init(elogprofile.your_made_up_profile) and then set these same
				settings for that profile also. 
	 
				One possible scenerio for using multiple profiles might be having different 
				parts of the code log to different files with different quota settings 
				or one part to a file and the other part to the event log.
				It's a lame example but you get the gist hopefully.
			*/
			Mylog.Init();  //This is the same as Mylog.Init(elogprofile.primary) .
			/*	You only need the Mylog instance just long enough to run Init.  Now
				you can just use the static methods to perform all logging actions,
				which is the most of the reason I wrote LogThis.
			*/
			Mylog = null;
			/*
				Set the properties for the primary log profile.
				Log.Profile sets the *active* profile, and it remains the active profile
				unless you set it explicitly.  Any calls to Log.LogThis or any other Log
				methods will use the active profile.
			*/
			Log.Profile = elogprofile.primary;
			//I prefer a simple text file log but you can change this 
			//to use the event log also.
			Log.LogWhere = elogwhere.file;
			
			if (Convert.ToInt32(Log.LogLevel) < 1)
			{
				Log.LogLevel = eloglevel.error; //errors show at the minimum.
			}
			else
			{
				Log.LogLevel = logLevel;
			}
			string[] fileDefaults = DefaultLogFileAndLocation();
			if (logFileName == string.Empty)
				Log.LogName = fileDefaults[1];
			else 
				Log.LogName = logFileName;

			if (logLocation == string.Empty)
				Log.LogBasePath = fileDefaults[0];
			else
				Log.LogBasePath = logLocation;

			//evaluate the quota size as meaning kbytes.
			Log.LogQuotaFormat = elogquotaformat.kbytes;
			//So, max logfile size = (Log.LogQuotaFormat * Log.LogSizeMax)
			Log.LogSizeMax = 100;
			//Every Log.LogPeriod the log file will roll over to a new file.
			Log.LogPeriod = elogperiod.month;
			//The log filename will be formatted this way.
			Log.LogNameFormat = elognameformat.date_name;
			Log.SetLogPath(); 
			/*
				ok, all things required as init configuration have now
				been set.  Simply call Log.LogThis or Log.LogHeader 
				anywhere in your code.

							Example: 
				
				Log.LogHeader(Log.LogName + ": Starting up",elogheaderlevel.Level_1);
				...
				Log.LogThis("Main: " + e.Message,eloglevel.error);
				...
				Log.LogHeader(Log.LogName + ": Exiting",elogheaderlevel.Level_1);
			*/


		}
		private static string[] DefaultLogFileAndLocation()
		{
			return GetBasename((string) System.Reflection.Assembly.GetExecutingAssembly().Location);
		}
		public static string[] GetBasename(string filePath)
		{
			/*
			 * Example:  filePath = "c:\temp\file1.exe"
			 * string[0] is "c:\temp"
			 * string[1] is "file1.exe"
			 * string[2] is "exe"
			*/
			string[] a = filePath.Split('\\');
			string[] baseInfo = new string[3];
			baseInfo[1] = a[a.Length-1]; 
			a = baseInfo[1].Split('.');
			if (a.Length > 1)
			{
				try
				{
					baseInfo[2] = a[a.Length-1]; 
					baseInfo[1] = baseInfo[1].Remove(baseInfo[1].Length-baseInfo[2].Length-1,baseInfo[2].Length+1);
				}
				catch(Exception ex)
				{
					Debug.Write(ex.Message);
				}
			}
			//locate base path without the last (slash + filename + extension)
			for (int i=0;i<filePath.Length - ((baseInfo[1].Length) + (baseInfo[2].Length+1))-1	;i++) 
			{
				baseInfo[0]+= filePath[i];
			}
				
			return baseInfo;
			
		}

		public static void LogThis(elogprofile logprofile, string logtext,eloglevel loglevel)
		{
			((LogMethods)m_Logs[Convert.ToString(logprofile)]).LogThis(logtext,loglevel);
		}
		public static void LogThis(elogprofile logprofile, string logtext,eloglevel loglevel, elogprefix logprefix)
		{
			((LogMethods)m_Logs[Convert.ToString(logprofile)]).LogThis(logtext,loglevel,logprefix);
		}
		public static void LogThis(string logtext,eloglevel loglevel, elogprefix logprefix)
		{
			if (m_DefaultToPrimaryProfile)
			{
				LogThis(elogprofile.primary,logtext,loglevel,logprefix);
			}
			else //log to current profile
			{
				m_LogMethods.LogThis(logtext,loglevel, logprefix);
			}

		}
		public static elogprofile Profile
		{
			get
			{
				return m_logprofile;
			}
			set
			{
				m_logprofile = (elogprofile)value;
				m_LogMethods = (LogMethods)m_Logs[Convert.ToString(m_logprofile)];
			}
		}

		public static bool DefaultToPrimaryProfile
		{
			get
			{
				return m_DefaultToPrimaryProfile;
			}
		}
		
		public static eloglevel LogLevel
		{
			get
			{
				if (m_DefaultToPrimaryProfile)
				{
					return ((LogMethods)m_Logs[Convert.ToString(elogprofile.primary)]).LogLevel;
				}
				else //default to current
				{
					return m_LogMethods.LogLevel;
				}
			}
			set
			{
				if (m_DefaultToPrimaryProfile)
				{
					((LogMethods)m_Logs[Convert.ToString(elogprofile.primary)]).LogLevel = (eloglevel)value;
				}
				else //default to current
				{
					m_LogMethods.LogLevel = (eloglevel)value;
				}
			}
		}
		public static elogwhere LogWhere
		{
			get
			{
				if (m_DefaultToPrimaryProfile)
				{
					return ((LogMethods)m_Logs[Convert.ToString(elogprofile.primary)]).LogWhere;
				}
				else //default to current
				{
					return m_LogMethods.LogWhere;
				}
			}
			set
			{
				if (m_DefaultToPrimaryProfile)
				{
					((LogMethods)m_Logs[Convert.ToString(elogprofile.primary)]).LogWhere = (elogwhere)value;
				}
				else //default to current
				{
					m_LogMethods.LogWhere = value;
				}
			}
		}
		public static string LogName
		{
			get
			{
				if (m_DefaultToPrimaryProfile)
				{
					return ((LogMethods)m_Logs[Convert.ToString(elogprofile.primary)]).LogName;
				}
				else //default to current
				{
					return m_LogMethods.LogName;
				}
			}
			set
			{
				if (m_DefaultToPrimaryProfile)
				{
					((LogMethods)m_Logs[Convert.ToString(elogprofile.primary)]).LogName = value;
				}
				else //default to current
				{
					m_LogMethods.LogName = value;
				}
			}
		}
		public static string LogBasePath
		{
			//example:  LogBasePath("c:\temp")
			//Default value: LogThis will place log in folder where exe is running.
			get
			{
				if (m_DefaultToPrimaryProfile)
				{
					return ((LogMethods)m_Logs[Convert.ToString(elogprofile.primary)]).LogBasePath;
				}
				else //default to current
				{
					return m_LogMethods.LogBasePath;
				}
			}
			set
			{
				if (m_DefaultToPrimaryProfile)
				{
					((LogMethods)m_Logs[Convert.ToString(elogprofile.primary)]).LogBasePath = value;
				}
				else //default to current
				{
					m_LogMethods.LogBasePath = value;
				}
			}
		}
		public static string LogPath
		{
			//This value is set in SetLogPath().  It will be LogBasePath + LogName + the chosen datestamp 
			get
			{
				if (m_DefaultToPrimaryProfile)
				{
					return ((LogMethods)m_Logs[Convert.ToString(elogprofile.primary)]).LogPath;
				}
				else //default to current
				{
					return m_LogMethods.LogPath;
				}
			}
			set
			{
				//This is not intended to be set by user code.  SetLogPath() will override.
				if (m_DefaultToPrimaryProfile)
				{
					((LogMethods)m_Logs[Convert.ToString(elogprofile.primary)]).LogPath = value;
				}
				else //default to current.
				{
					m_LogMethods.LogPath = value;
				}
			}
		}
		public static string ProcessName
		{
			get
			{
				if (m_DefaultToPrimaryProfile)
				{
					return ((LogMethods)m_Logs[Convert.ToString(elogprofile.primary)]).ProcessName;
				}
				else //default to current
				{
					return m_LogMethods.ProcessName;
				}
			}
			set
			{
				if (m_DefaultToPrimaryProfile)
				{
					((LogMethods)m_Logs[Convert.ToString(elogprofile.primary)]).ProcessName = value;
				}
				else //default to current
				{
					m_LogMethods.ProcessName = value;
				}
			}
		}
		public static elogperiod LogPeriod
		{
			get
			{
				if (m_DefaultToPrimaryProfile)
				{
					return ((LogMethods)m_Logs[Convert.ToString(elogprofile.primary)]).LogPeriod;
				}
				else //default to current
				{
					return m_LogMethods.LogPeriod;
				}
			}
			set
			{
				if (m_DefaultToPrimaryProfile)
				{
					((LogMethods)m_Logs[Convert.ToString(elogprofile.primary)]).LogPeriod = (elogperiod)value;
				}
				else //default to current
				{
					m_LogMethods.LogPeriod = value;
				}
			}
		}
		public static elognameformat LogNameFormat
		{
			get
			{
				if (m_DefaultToPrimaryProfile)
				{
					return ((LogMethods)m_Logs[Convert.ToString(elogprofile.primary)]).LogNameFormat;
				}
				else //default to current
				{
					return m_LogMethods.LogNameFormat;
				}
			}
			set
			{
				if (m_DefaultToPrimaryProfile)
				{
					((LogMethods)m_Logs[Convert.ToString(elogprofile.primary)]).LogNameFormat = (elognameformat)value;
				}
				else //default to current
				{
					m_LogMethods.LogNameFormat = (elognameformat)value;
				}
			}
		}
		public static elogquotaformat LogQuotaFormat
		{
			get
			{
				if (m_DefaultToPrimaryProfile)
				{
					return ((LogMethods)m_Logs[Convert.ToString(elogprofile.primary)]).LogQuotaFormat;
				}
				else //default to current
				{
					return m_LogMethods.LogQuotaFormat;
				}
			}
			set
			{
				if (m_DefaultToPrimaryProfile)
				{
					((LogMethods)m_Logs[Convert.ToString(elogprofile.primary)]).LogQuotaFormat = (elogquotaformat)value;
				}
				else //default to current
				{
					m_LogMethods.LogQuotaFormat = (elogquotaformat)value;
				}
			}
		}
		public static elogprefix LogPrefix
		{
			get
			{
				if (m_DefaultToPrimaryProfile)
				{
					return ((LogMethods)m_Logs[Convert.ToString(elogprofile.primary)]).LogPrefix;
				}
				else //default to current
				{
					return m_LogMethods.LogPrefix;
				}
			}
			set
			{
				if (m_DefaultToPrimaryProfile)
				{
					((LogMethods)m_Logs[Convert.ToString(elogprofile.primary)]).LogPrefix = (elogprefix)value;
				}
				else //default to current
				{
					m_LogMethods.LogPrefix = (elogprefix)value;
				}
			}
		}
		public static int LogSizeMax
		{
			get
			{
				if (m_DefaultToPrimaryProfile)
				{
					return ((LogMethods)m_Logs[Convert.ToString(elogprofile.primary)]).LogSizeMax;
				}
				else //default to current
				{
					return m_LogMethods.LogSizeMax;
				}
			}
			set
			{
				if (m_DefaultToPrimaryProfile)
				{
					((LogMethods)m_Logs[Convert.ToString(elogprofile.primary)]).LogSizeMax = value;
				}
				else //default to current
				{
					m_LogMethods.LogSizeMax = value;
				}
			}
		}
		public static void SetLogPath()
		{
			if (m_DefaultToPrimaryProfile)
			{
				SetLogPath(elogprofile.primary);
			}
			else //default to current
			{
				m_LogMethods.SetLogPath();
			}
		}
		public static void SetLogPath(elogprofile logprofile)
		{
			((LogMethods)m_Logs[Convert.ToString(logprofile)]).SetLogPath();
		}
		public static void LogReset()
		{
			if (m_DefaultToPrimaryProfile)
			{
				LogReset(elogprofile.primary);
			}
			else //default to current
			{
				m_LogMethods.LogReset();
			}
		}
		public static void LogReset(elogprofile logprofile)
		{
			((LogMethods)m_Logs[Convert.ToString(logprofile)]).LogReset();
		}
		public static void LogHeader(string sText, elogheaderlevel logheaderlevel)
		{
			if (m_DefaultToPrimaryProfile)
			{
				LogHeader(elogprofile.primary, sText, logheaderlevel);
			}
			else
			{
				LogHeader(m_logprofile, sText, logheaderlevel);
			}
		}

		public static void LogHeader(elogprofile logprofile, string sText,elogheaderlevel logheaderlevel)
		{
			string sHeader = "";
			DateTime dt = DateTime.Now;
			switch(logheaderlevel)
			{
				case elogheaderlevel.Level_1:
					sHeader = "======(" + System.AppDomain.CurrentDomain.FriendlyName + ") ====  " + sText + "============ Date:" + dt.ToString("yyyyMMdd") + " Time:" + dt.ToString("hh:mm:ss") ;
					break;
				case elogheaderlevel.Level_2:
					sHeader = "------" + sText + " ============ Time:" + dt.ToString("hh:mm:ss") ;
					break;
				case elogheaderlevel.Level_3:
					sHeader = "---" + sText + " --- Time:" + dt.ToString("hh:mm:ss") ;
					break;
			}
			LogThis(sHeader, eloglevel.info, elogprefix.none);
		}
		public static LogMethods GetProfile(elogprofile logprofile)
		{
			return (LogMethods) m_Logs[Convert.ToString(logprofile)];
		}
		public void Init()
		{
			Init(elogprofile.primary);

		}
		public void Init(elogprofile logprofile)
		{
			
			if (m_Logs == null) 
			{ 
				m_Logs = new ListDictionary();
			}
			if (m_Logs.Contains(Convert.ToString(logprofile))) { return; }
			m_LogMethods = new LogMethods();
			m_Logs.Add(Convert.ToString(logprofile),m_LogMethods);
			Profile = logprofile;
			if (m_Logs.Count > 1 | logprofile != elogprofile.primary)
			{
				//Once there are more than just the primary log profile then the properties 
				//can no longer default to just the primary profile.
				m_DefaultToPrimaryProfile = false;
			}
		}
	}

	public class LogMethods //: System.IDisposable
	{
		private eloglevel m_loglevel = eloglevel.error;
		private elogwhere m_logwhere = elogwhere.eventlog;
		private elogperiod m_logperiod = elogperiod.none;
		private elognameformat m_lognameformat = elognameformat.name_date;
		private string m_ProcessName=System.AppDomain.CurrentDomain.FriendlyName;
		private string m_logfilename=System.AppDomain.CurrentDomain.FriendlyName;
		private elogquotaformat m_logquotaformat = elogquotaformat.no_restriction;
		private int m_logsizemax = 0;
		private elogprefix m_logprefix = elogprefix.dt_loglevel;
		private string m_logfilepath = "";
		private string m_logbasepath = string.Empty;
		private string m_logfiletype = ".log";
		public eloglevel LogLevel
		{
			get
			{
				return m_loglevel;
			}
			set
			{
				m_loglevel = (eloglevel)value;
			}
		}
		public elogwhere LogWhere
		{
			get
			{
				return m_logwhere;
			}
			set
			{
				m_logwhere = value;
			}
		}
		public elognameformat LogNameFormat
		{
			get
			{
				return m_lognameformat;
			}
			set
			{
				m_lognameformat = value;
			}
		}
		public elogprefix LogPrefix
		{
			get
			{
				return m_logprefix;
			}
			set
			{
				m_logprefix = value;
			}
		}
		public string LogName
		{
			get
			{
				return m_logfilename;
			}
			set
			{
				m_logfilename = value;
			}
		}
		public string LogBasePath
		{
			get
			{
				return m_logbasepath;
			}
			set
			{
				m_logbasepath = value;
			}
		}
		public string LogPath
		{
			get
			{
				return m_logfilepath;
			}
			set
			{
				m_logfilepath = value;
			}
		}
		public string ProcessName
		{
			get
			{
				return m_ProcessName;
			}
			set
			{
				m_ProcessName = value;
			}
		}
		public elogperiod LogPeriod
		{
			get
			{
				return m_logperiod;
			}
			set
			{
				m_logperiod = (elogperiod)value;
			}
		}
		public elogquotaformat LogQuotaFormat
		{
			get
			{
				return m_logquotaformat;
			}
			set
			{
				m_logquotaformat = (elogquotaformat)value;
			}
		}
		public int LogSizeMax
		{
			get
			{
				return m_logsizemax;
			}
			set
			{
				m_logsizemax = value;
			}
		}

		public void LogReset()
		{
			if (!(LogName == null))
			{
				File.Delete(LogName);
			}
		}
		 
		private void AppendToFile(string sPath, string sText)
		{
			StreamWriter SW;
			SW=File.AppendText(sPath);
			SW.WriteLine(sText);
			SW.Close();
		}
		private void LogEvent(string sText, eloglevel loglevel)
		{
			EventLogEntryType EventType;
			switch(loglevel)
			{
				case eloglevel.error:
					EventType = EventLogEntryType.Error;
					break;
				case eloglevel.warn:
					EventType = EventLogEntryType.Warning;
					break;
				case eloglevel.info:
					EventType = EventLogEntryType.Information;
					break;
				default:
					EventType = EventLogEntryType.Information;
					break;
			}
			//open and write to event log.
			System.Diagnostics.EventLog oEV = new System.Diagnostics.EventLog();
			oEV.Source = m_ProcessName;
			oEV.WriteEntry (sText, EventType);
			oEV.Close();
		}
		public void TruncateLogFile()
		{
			TruncateLogFile(LogPath);
		}
		public void TruncateLogFile(string sLogPath)
		{
			if (m_logquotaformat == elogquotaformat.no_restriction) { return; }
			
			long nfileSize = 0;
			long i=0;
			long j=0;
			if (!File.Exists(sLogPath)) { return;}
			switch (m_logquotaformat)
			{
				case elogquotaformat.kbytes:
					try
					{
						FileInfo info = new FileInfo(sLogPath);
						nfileSize = info.Length;
					}
					catch (Exception x)
					{
						System.Console.WriteLine("Error:" + x.Message);
						nfileSize = 0;
					}
					if (nfileSize < (m_logsizemax*1024))
					{ return; }
					break;
				case elogquotaformat.rows:
					using (StreamReader sr = new StreamReader(sLogPath)) 
					{
						String line;
						while ((line = sr.ReadLine()) != null) 
						{
							i++;
						}	
						if (i < m_logsizemax) {return;}
					}
					break;
			}

			File.Delete(sLogPath + ".new");
			StreamWriter SW;

			SW=File.AppendText(sLogPath + ".new");
			using (StreamReader sr = new StreamReader(sLogPath)) 
			{
				switch (m_logquotaformat)
				{
					case elogquotaformat.kbytes:
						char[] c = null;
						long bufsize = 1024; //should match streams natural buffer size.
						long kb = 1024;
						j = (long)((m_logsizemax*kb) * .9);
						while (sr.Peek() >= 0) 
						{
							c = new char[bufsize];
							sr.Read(c, 0, c.Length);
							i++;
							if ((i*bufsize) > j)
							{
								for (i = 0;i<c.Length ; i++)
								{
									if (c[i] == '\r' && c[i+1] == '\n')
									{
										//write out the remaining part of the last line.
										char[] c2 = new char[i+2];
										Array.Copy(c,0,c2,0,i+2);
										SW.Write(c2);
										break;
									}
										
								}
								
								break;
							}
							else
							{
								SW.Write(c);
							}
						}
						break;
					case elogquotaformat.rows:
						String line;
						j = (long)(m_logsizemax * .9); //reduce by 10% below max.
						while ((line = sr.ReadLine()) != null) 
						{
							SW.WriteLine(line);
							i++;
							if (i > j) { break;}
						}
						break;
				}
			}
			SW.Close();
			    
            File.Delete(sLogPath);
			File.Move(sLogPath + ".new",sLogPath);
		}
		private string GetWeek()
		{
			//I got this code right out of MSDN and only decided to change the 
			//week to start on Monday instead of Sunday for logging.
			//The first and last week of the year will likely be less than 7 days.

			// Gets the Calendar instance associated with a CultureInfo.
			CultureInfo myCI = new CultureInfo("en-US");
			Calendar myCal = myCI.Calendar;

			// Gets the DTFI properties required by GetWeekOfYear.
			CalendarWeekRule myCWR = myCI.DateTimeFormat.CalendarWeekRule;
			//DayOfWeek myFirstDOW = myCI.DateTimeFormat.FirstDayOfWeek;
			DayOfWeek myFirstDOW = DayOfWeek.Monday;
			// Displays the total number of weeks in the current year.
			DateTime LastDay = new System.DateTime( DateTime.Now.Year, 12, 31 );
			//Console.WriteLine( "There are {0} weeks in the current year ({1}).", myCal.GetWeekOfYear( LastDay, myCWR, myFirstDOW ), LastDay.Year );
			return myCal.GetWeekOfYear( DateTime.Now, myCWR, myFirstDOW ).ToString();

		}
		public void SetLogPath()
		{
			string sPeriod = "";
			string sLogName = m_logfilename;
			if (!(LogPeriod == elogperiod.none))
			{
				DateTime dt = DateTime.Now;
					
				switch (m_logperiod)
				{
					case elogperiod.day:
						sPeriod = dt.ToString("yyyyMMdd");
						break;
					case elogperiod.week:
						string week = GetWeek();
						sPeriod = dt.ToString("yyyyweek" + week);
						break;
					case elogperiod.month:
						sPeriod = dt.ToString("yyyyMM");
						break;
				}

				switch (m_lognameformat)
				{
					case elognameformat.date_name:
						sLogName = sPeriod + "_" + m_logfilename;
						break;
					case elognameformat.name_date:
						sLogName = m_logfilename + "_" + sPeriod;
						break;
				}
			}
			string sFilePath;
			if (m_logbasepath == string.Empty)
				sFilePath = System.Environment.CurrentDirectory.TrimEnd('\\') + "\\" + LogName + "_" + sPeriod + m_logfiletype;
			else
				sFilePath = m_logbasepath.TrimEnd('\\') + @"\" + LogName + "_" + sPeriod + m_logfiletype;

			m_logfilepath = sFilePath;
			//return sFilePath;
			return;
		}
		public void LogThis(string logtext,eloglevel loglevel)
		{
			LogThis(logtext, loglevel, m_logprefix);
		}
		public void LogThis(string logtext,eloglevel loglevel, elogprefix logprefix)
		{
			if (m_loglevel >= loglevel)
			{
				
				string sFilePath = LogPath;
				if (sFilePath == "" )
				{
					SetLogPath();
					sFilePath = LogPath;
				}
				TruncateLogFile(sFilePath);
				DateTime dt = DateTime.Now;
				switch (logprefix)
				{
					case elogprefix.dt:
						logtext = dt.ToString("yyyy.MM.dd") + "-" + dt.ToString("hh.mm.ss") + ": " + logtext;
						break;
					case elogprefix.loglevel:
						logtext = loglevel.ToString() + ": " + logtext;
						break;
					case elogprefix.dt_loglevel:
						logtext = dt.ToString("yyyy.MM.dd") + "-" + dt.ToString("hh.mm.ss") + ":" + loglevel + ": " + logtext;
						break;
				}
				
				//log it
				switch (m_logwhere)
				{
					case elogwhere.file:
						AppendToFile(sFilePath,logtext);
						break;
					case elogwhere.eventlog:
						LogEvent(logtext,loglevel);
						break;
					case elogwhere.eventlog_and_file:
						AppendToFile(sFilePath,logtext);
						LogEvent(logtext,loglevel);
						break;
				}

			}

		}

	}


}
