//-----------------------------------------------------------------------------
// File: DXUtil.cs
//
// Desc: Shortcut macros and functions for using DX objects
//
// Copyright (c) Microsoft Corporation. All rights reserved
//-----------------------------------------------------------------------------
using System;
using System.IO;
using System.Runtime.InteropServices;




/// <summary>
/// Enumeration for various actions our timer can perform
/// </summary>
public enum DirectXTimer
{
	Reset, 
	Start, 
	Stop, 
	Advance,
	GetAbsoluteTime, 
	GetApplicationTime, 
	GetElapsedTime 
};




/// <summary>
/// Generic utility functions for our samples
/// </summary>
public class DXUtil
{
	#region Timer Internal Stuff
	[System.Security.SuppressUnmanagedCodeSecurity] // We won't use this maliciously
	[DllImport("kernel32")]
	private static extern bool QueryPerformanceFrequency(ref long PerformanceFrequency);
	[System.Security.SuppressUnmanagedCodeSecurity] // We won't use this maliciously
	[DllImport("kernel32")]
	private static extern bool QueryPerformanceCounter(ref long PerformanceCount);
	[System.Security.SuppressUnmanagedCodeSecurity] // We won't use this maliciously
	[DllImport("winmm.dll")]
	public static extern int timeGetTime();
	private static bool isTimerInitialized = false;
	private static bool m_bUsingQPF = false;
	private static bool m_bTimerStopped = true;
	private static long m_llQPFTicksPerSec = 0;
	private static long m_llStopTime = 0;
	private static long m_llLastElapsedTime = 0;
	private static long m_llBaseTime = 0;
	private static double m_fLastElapsedTime = 0.0;
	private static double m_fBaseTime = 0.0;
	private static double m_fStopTime = 0.0;
	#endregion

	// Constants for SDK Path registry keys
	private const string sdkPath = "Software\\Microsoft\\DirectX SDK";
	private const string sdkKey = "DX9S4SDK Samples Path";

	private DXUtil() { /* Private Constructor */ }




    /// <summary>
    /// Returns the DirectX SDK media path
    /// </summary>
	public static string SdkMediaPath
	{
		get
		{
			Microsoft.Win32.RegistryKey rKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(sdkPath);
			string sReg = string.Empty;
			if (rKey != null)
			{
				sReg = (string)rKey.GetValue(sdkKey);
				rKey.Close();
			}
			if (sReg != null)
				sReg += @"\Media\";
			else 
				return string.Empty;

			return sReg;
		}
	}




    /// <summary>
    /// Performs timer opertations. Use the following commands:
    /// 
    ///          DirectXTimer.Reset - to reset the timer
    ///          DirectXTimer.Start - to start the timer
    ///          DirectXTimer.Stop - to stop (or pause) the timer
    ///          DirectXTimer.Advance - to advance the timer by 0.1 seconds
    ///          DirectXTimer.GetAbsoluteTime - to get the absolute system time
    ///          DirectXTimer.GetApplicationTime - to get the current time
    ///          DirectXTimer.GetElapsedTime - to get the time that elapsed between TIMER_GETELAPSEDTIME calls
    ///
    /// </summary>
	public static float Timer(DirectXTimer command)
	{
		if (!isTimerInitialized)
		{
			isTimerInitialized = true;

			// Use QueryPerformanceFrequency() to get frequency of timer.  If QPF is
			// not supported, we will timeGetTime() which returns milliseconds.
			long qwTicksPerSec = 0;
			m_bUsingQPF = QueryPerformanceFrequency(ref qwTicksPerSec);
			if (m_bUsingQPF)
				m_llQPFTicksPerSec = qwTicksPerSec;
		}
		if (m_bUsingQPF)
		{
			double time;
			double fElapsedTime;
			long qwTime = 0;
		    
			// Get either the current time or the stop time, depending
			// on whether we're stopped and what command was sent
			if (m_llStopTime != 0 && command != DirectXTimer.Start && command != DirectXTimer.GetAbsoluteTime)
				qwTime = m_llStopTime;
			else
				QueryPerformanceCounter(ref qwTime);

			// Return the elapsed time
			if (command == DirectXTimer.GetElapsedTime)
			{
				fElapsedTime = (double) (qwTime - m_llLastElapsedTime) / (double) m_llQPFTicksPerSec;
				m_llLastElapsedTime = qwTime;
				return (float)fElapsedTime;
			}
		
			// Return the current time
			if (command == DirectXTimer.GetApplicationTime)
			{
				double fAppTime = (double) (qwTime - m_llBaseTime) / (double) m_llQPFTicksPerSec;
				return (float)fAppTime;
			}
		
			// Reset the timer
			if (command == DirectXTimer.Reset)
			{
				m_llBaseTime        = qwTime;
				m_llLastElapsedTime = qwTime;
				m_llStopTime        = 0;
				m_bTimerStopped     = false;
				return 0.0f;
			}
		
			// Start the timer
			if (command == DirectXTimer.Start)
			{
				if (m_bTimerStopped)
					m_llBaseTime += qwTime - m_llStopTime;
				m_llStopTime = 0;
				m_llLastElapsedTime = qwTime;
				m_bTimerStopped = false;
				return 0.0f;
			}
		
			// Stop the timer
			if (command == DirectXTimer.Stop)
			{
				if (!m_bTimerStopped)
				{
					m_llStopTime = qwTime;
					m_llLastElapsedTime = qwTime;
					m_bTimerStopped = true;
				}
				return 0.0f;
			}
		
			// Advance the timer by 1/10th second
			if (command == DirectXTimer.Advance)
			{
				m_llStopTime += m_llQPFTicksPerSec/10;
				return 0.0f;
			}

			if (command == DirectXTimer.GetAbsoluteTime)
			{
				time = qwTime / (double) m_llQPFTicksPerSec;
				return (float)time;
			}

			return -1.0f; // Invalid command specified
		}
		else
		{
			// Get the time using timeGetTime()
			double time;
			double fElapsedTime;
		    
			// Get either the current time or the stop time, depending
			// on whether we're stopped and what command was sent
			if (m_fStopTime != 0.0 && command != DirectXTimer.Start && command != DirectXTimer.GetAbsoluteTime)
				time = m_fStopTime;
			else
				time = timeGetTime() * 0.001;
		
			// Return the elapsed time
			if (command == DirectXTimer.GetElapsedTime)
			{   
				fElapsedTime = (double) (time - m_fLastElapsedTime);
				m_fLastElapsedTime = time;
				return (float) fElapsedTime;
			}
		
			// Return the current time
			if (command == DirectXTimer.GetApplicationTime)
			{
				return (float) (time - m_fBaseTime);
			}
		
			// Reset the timer
			if (command == DirectXTimer.Reset)
			{
				m_fBaseTime         = time;
				m_fLastElapsedTime  = time;
				m_fStopTime         = 0;
				m_bTimerStopped     = false;
				return 0.0f;
			}
		
			// Start the timer
			if (command == DirectXTimer.Start)
			{
				if (m_bTimerStopped)
					m_fBaseTime += time - m_fStopTime;
				m_fStopTime = 0.0f;
				m_fLastElapsedTime  = time;
				m_bTimerStopped = false;
				return 0.0f;
			}
		
			// Stop the timer
			if (command == DirectXTimer.Stop)
			{
				if (!m_bTimerStopped)
				{
					m_fStopTime = time;
					m_fLastElapsedTime  = time;
					m_bTimerStopped = true;
				}
				return 0.0f;
			}
		
			// Advance the timer by 1/10th second
			if (command == DirectXTimer.Advance)
			{
				m_fStopTime += 0.1f;
				return 0.0f;
			}

			if (command == DirectXTimer.GetAbsoluteTime)
			{
				return (float) time;
			}

			return -1.0f; // Invalid command specified
		}
	}




    /// <summary>
    /// Returns a valid path to a DXSDK media file
    /// </summary>
    /// <param name="path">Initial path to search</param>
    /// <param name="filename">Filename we're searching for</param>
    /// <returns>Full path to the file</returns>
	public static string FindMediaFile(string path, string filename)
	{
		// First try to load the file in the full path
		if (path != null)
		{
			if (File.Exists(AppendDirectorySeparator(path) + filename))
				return AppendDirectorySeparator(path) + filename;
		}

		// if not try to find the filename in the current folder.
		if (File.Exists(filename))
			return AppendDirectorySeparator(Directory.GetCurrentDirectory()) + filename; 

		// last, check if the file exists in the media directory
		if (File.Exists(AppendDirectorySeparator(SdkMediaPath) + filename))
			return AppendDirectorySeparator(SdkMediaPath) + filename;

		throw new FileNotFoundException("Could not find this file.", filename);
	}




    /// <summary>
    /// Returns a valid string with a directory separator at the end.
    /// </summary>
	private static string AppendDirectorySeparator(string filename)
	{
		if (!filename.EndsWith(@"\"))
			return filename + @"\";

		return filename;
	}
}
