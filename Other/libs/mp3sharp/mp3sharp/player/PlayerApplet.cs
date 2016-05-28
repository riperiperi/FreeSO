/*
* 29/01/00		Initial version. mdm@techie.com
/*-----------------------------------------------------------------------
*  This program is free software; you can redistribute it and/or modify
*  it under the terms of the GNU General Public License as published by
*  the Free Software Foundation; either version 2 of the License, or
*  (at your option) any later version.
*
*  This program is distributed in the hope that it will be useful,
*  but WITHOUT ANY WARRANTY; without even the implied warranty of
*  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
*  GNU General Public License for more details.
*
*  You should have received a copy of the GNU General Public License
*  along with this program; if not, write to the Free Software
*  Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
*----------------------------------------------------------------------
*/
namespace javazoom.jl.player
{
	using System;
	using System.Collections;
	using System.ComponentModel;
	using System.Drawing;
	using System.Data;
	using System.Windows.Forms;
	using javazoom.jl.decoder;
	/// <summary> A simple applet that plays an MPEG audio file. 
	/// The URL (relative to the document base)
	/// is passed as the "audioURL" parameter. 
	/// 
	/// </summary>
	/// <author> 	Mat McGowan
	/// @since	0.0.8
	/// 
	/// </author>
	public class PlayerApplet:System.Windows.Forms.UserControl, IThreadRunnable
	{
		public PlayerApplet()
		{
			init();
		}
		/// <summary> Retrieves the <code>AudioDevice</code> instance that will
		/// be used to sound the audio data. 
		/// 
		/// </summary>
		/// <returns>	an audio device instance that will be used to 
		/// sound the audio stream.
		/// 
		/// </returns>
		virtual protected internal AudioDevice AudioDevice
		{
			get
			{
				return FactoryRegistry.systemRegistry().createAudioDevice();
			}
			
		}
		/// <summary> Retrieves the InputStream that provides the MPEG audio
		/// stream data. 
		/// 
		/// </summary>
		/// <returns>	an InputStream from which the MPEG audio data
		/// is read, or null if an error occurs. 
		/// 
		/// </returns>
		virtual protected internal System.IO.Stream AudioStream
		{
			get
			{
				System.IO.Stream @in = null;
				
				try
				{
					System.Uri url = AudioURL;
					if (url != null)
						@in = System.Net.WebRequest.Create(url).GetResponse().GetResponseStream();
				}
				catch (System.IO.IOException ex)
				{
					System.Console.Error.WriteLine(ex);
				}
				return @in;
			}
			
		}
		virtual protected internal System.String AudioFileName
		{
			get
			{
				System.String urlString = fileName;
				if (urlString == null)
				{
					//UPGRADE_ISSUE: Method 'java.applet.Applet.getParameter' was not converted. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1000_javaappletAppletgetParameter_javalangString"'
					//UPGRADE_TODO: Applet parameter was not converted because it requires a string literal as parameter name. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1167"'
					urlString = getParameter(AUDIO_PARAMETER);
				}
				return urlString;
			}
			
		}
		virtual protected internal System.Uri AudioURL
		{
			get
			{
				System.String urlString = AudioFileName;
				System.Uri url = null;
				if (urlString != null)
				{
					try
					{
						//UPGRADE_TODO: Class 'java.net.URL' was converted to a 'System.Uri' which does not throw an exception if a URL specifies an unknown protocol. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1132"'
						//UPGRADE_ISSUE: Method 'java.applet.Applet.getDocumentBase' was not converted. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1000_javaappletAppletgetDocumentBase"'
						url = new System.Uri(getDocumentBase(), urlString);
					}
					catch (System.Exception ex)
					{
						System.Console.Error.WriteLine(ex);
					}
				}
				return url;
			}
			
		}
		/// <summary> Sets the URL of the audio stream to play.
		/// </summary>
		virtual public System.String FileName
		{
			get
			{
				return fileName;
			}
			
			set
			{
				fileName = value;
			}
			
		}
		private bool isActiveVar = true;
		public bool isActive()
		{
			return isActiveVar;
		}
		private void  javazoom.jl.player.PlayerApplet_StartEventHandler(System.Object sender, System.EventArgs e)
		{
			start();
		}
		private void  javazoom.jl.player.PlayerApplet_StopEventHandler(System.Object sender, System.EventArgs e)
		{
			stop();
		}
		public String getParameter(System.String paramName)
		{
			switch (paramName)
			{
				
				default: 
					return null;
				
			}
		}
		public const System.String AUDIO_PARAMETER = "audioURL";
		
		/// <summary> The Player used to play the MPEG audio file. 
		/// </summary>
		private Player player = null;
		
		/// <summary> The thread that runs the player.
		/// </summary>
		private SupportClass.ThreadClass playerThread = null;
		
		private System.String fileName = null;
		
		
		
		
		
		
		
		
		/// <summary> Stops the audio player. If the player is already stopped
		/// this method is a no-op.  
		/// </summary>
		protected internal virtual void  stopPlayer()
		{
			if (player != null)
			{
				player.close();
				player = null;
				playerThread = null;
			}
		}
		
		/// <summary> Decompresses audio data from an InputStream and plays it
		/// back through an AudioDevice. The playback is run on a newly
		/// created thread. 
		/// 
		/// </summary>
		/// <param name="in	The">InputStream that provides the MPEG audio data.
		/// </param>
		/// <param name="dev	The">AudioDevice to use to sound the decompressed data. 
		/// 
		/// @throws JavaLayerException if there was a problem decoding
		/// or playing the audio data.
		/// 
		/// </param>
		protected internal virtual void  play(System.IO.Stream @in, AudioDevice dev)
		{
			stopPlayer();
			
			if (@in != null && dev != null)
			{
				player = new Player(@in, dev);
				playerThread = createPlayerThread();
				playerThread.Start();
			}
		}
		
		/// <summary> Creates a new thread used to run the audio player.
		/// </summary>
		/// <returns> A new Thread that, once started, runs the audio player.
		/// 
		/// </returns>
		protected internal virtual SupportClass.ThreadClass createPlayerThread()
		{
			SupportClass.ThreadClass temp_Thread;
			temp_Thread = new SupportClass.ThreadClass(new System.Threading.ThreadStart(this.Run));
			temp_Thread.Name = "Audio player thread";
			return temp_Thread;
		}
		
		//UPGRADE_TODO: The equivalent of method 'java.applet.Applet.init' is not an override method. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1143"'
		/// <summary> Initializes this applet.
		/// </summary>
		public void  init()
		{
			this.BackColor = Color.LightGray;
			this.Load += new System.EventHandler(this.javazoom.jl.player.PlayerApplet_StartEventHandler);
			this.Disposed += new System.EventHandler(this.javazoom.jl.player.PlayerApplet_StopEventHandler);
		}
		
		//UPGRADE_TODO: The equivalent of method 'java.applet.Applet.start' is not an override method. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1143"'
		/// <summary> Starts this applet. An input stream and audio device 
		/// are created and passed to the play() method.
		/// </summary>
		public void  start()
		{
			isActiveVar = true;
			System.String name = AudioFileName;
			try
			{
				System.IO.Stream @in = AudioStream;
				AudioDevice dev = AudioDevice;
				play(@in, dev);
			}
			catch (JavaLayerException ex)
			{
				lock (System.Console.Error)
				{
					System.Console.Error.WriteLine("Unable to play " + name);
					SupportClass.WriteStackTrace(ex, System.Console.Error);
				}
			}
		}
		
		//UPGRADE_TODO: The equivalent of method 'java.applet.Applet.stop' is not an override method. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1143"'
		/// <summary> Stops this applet. If audio is currently playing, it is
		/// stopped.
		/// </summary>
		public void  stop()
		{
			try
			{
				stopPlayer();
			}
			catch (JavaLayerException ex)
			{
				System.Console.Error.WriteLine(ex);
			}
			isActiveVar = false;
		}
		
		//UPGRADE_TODO: This function is not marked as virtual in the base class. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca5000"'
		public  void  Dispose()
		{
		}
		
		//UPGRADE_TODO: The equivalent of method 'java.lang.Runnable.run' is not an override method. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1143"'
		/// <summary> The run method for the audio player thread. Simply calls
		/// play() on the player to play the entire stream. 
		/// </summary>
		public void  Run()
		{
			if (player != null)
			{
				try
				{
					player.play();
				}
				catch (JavaLayerException ex)
				{
					System.Console.Error.WriteLine("Problem playing audio: " + ex);
				}
			}
		}
	}
}