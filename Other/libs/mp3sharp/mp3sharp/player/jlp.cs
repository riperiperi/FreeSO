/*
* 06/04/01		Streaming support added. ebsp@iname.com
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
	using JavaLayerException = javazoom.jl.decoder.JavaLayerException;
	/// <summary> The <code>jlp</code> class implements a simple command-line
	/// player for MPEG audio files.
	/// *
	/// </summary>
	/// <author>  Mat McGowan (mdm@techie.com)
	/// 
	/// </author>
	public class jlp
	{
		/// <summary> Playing file from URL (Streaming).
		/// </summary>
		virtual protected internal System.IO.Stream URLInputStream
		{
			get
			{
				
				//UPGRADE_TODO: Class 'java.net.URL' was converted to a 'System.Uri' which does not throw an exception if a URL specifies an unknown protocol. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1132"'
				System.Uri url = new System.Uri(fFilename);
				System.IO.Stream fin = System.Net.WebRequest.Create(url).GetResponse().GetResponseStream();
				System.IO.BufferedStream bin = new System.IO.BufferedStream(fin);
				return bin;
			}
			
		}
		/// <summary> Playing file from FileInputStream.
		/// </summary>
		virtual protected internal System.IO.Stream InputStream
		{
			get
			{
				System.IO.FileStream fin = new System.IO.FileStream(fFilename, System.IO.FileMode.Open, System.IO.FileAccess.Read);
				System.IO.BufferedStream bin = new System.IO.BufferedStream(fin);
				return bin;
			}
			
		}
		virtual protected internal AudioDevice AudioDevice
		{
			get
			{
				return FactoryRegistry.systemRegistry().createAudioDevice();
			}
			
		}
		private System.String fFilename = null;
		private bool remote = false;
		
		[STAThread]
		public static void  Main(System.String[] args)
		{
			int retval = 0;
			try
			{
				jlp player = createInstance(args);
				if (player != null)
					player.play();
			}
			catch (System.Exception ex)
			{
				System.Console.Error.WriteLine(ex);
				SupportClass.WriteStackTrace(ex, System.Console.Error);
				retval = 1;
			}
			System.Environment.Exit(retval);
		}
		
		static public jlp createInstance(System.String[] args)
		{
			jlp player = new jlp();
			if (!player.parseArgs(args))
				player = null;
			return player;
		}
		
		private jlp()
		{
		}
		
		public jlp(System.String filename)
		{
			init(filename);
		}
		
		protected internal virtual void  init(System.String filename)
		{
			fFilename = filename;
		}
		
		protected internal virtual bool parseArgs(System.String[] args)
		{
			bool parsed = false;
			if (args.Length == 1)
			{
				init(args[0]);
				parsed = true;
				remote = false;
			}
			else if (args.Length == 2)
			{
				if (!(args[0].Equals("-url")))
				{
					showUsage();
				}
				else
				{
					init(args[1]);
					parsed = true;
					remote = true;
				}
			}
			else
			{
				showUsage();
			}
			return parsed;
		}
		
		public virtual void  showUsage()
		{
			System.Console.Out.WriteLine("Usage: jlp [-url] <filename>");
			System.Console.Out.WriteLine("");
			System.Console.Out.WriteLine(" e.g. : java javazoom.jl.player.jlp localfile.mp3");
			System.Console.Out.WriteLine("        java javazoom.jl.player.jlp -url http://www.server.com/remotefile.mp3");
			System.Console.Out.WriteLine("        java javazoom.jl.player.jlp -url http://www.shoutcastserver.com:8000");
		}
		
		public virtual void  play()
		{
			try
			{
				System.Console.Out.WriteLine("playing " + fFilename + "...");
				System.IO.Stream in_Renamed = null;
				if (remote == true)
					in_Renamed = URLInputStream;
				else
					in_Renamed = InputStream;
				AudioDevice dev = AudioDevice;
				Player player = new Player(in_Renamed, dev);
				player.play();
			}
			catch (System.IO.IOException ex)
			{
				throw new JavaLayerException("Problem playing file " + fFilename, ex);
			}
			catch (System.Exception ex)
			{
				throw new JavaLayerException("Problem playing file " + fFilename, ex);
			}
		}
		
		
		
	}
}