/* 12/12/99 JavaLayer 0.0.7		mdm@techie.com
* Adapted from javalayer and MPEG_Args.
* Doc'ed and integerated with JL converter. Removed
* Win32 specifics from original Maplay code.
*
* MPEG_Args Based Class - E.B 14/02/99 , JavaLayer
*/
namespace javazoom.jl.converter
{
	using System;
	using javazoom.jl.decoder;
	/// <summary> The <code>jlc</code> class presents the JavaLayer
	/// Conversion functionality as a command-line program.
	/// *
	/// @since 0.0.7
	/// </summary>
	
	public class jlc
	{
		
		[STAThread]
		static public void  Main(System.String[] args)
		{
			System.String[] argv;
			long start = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
			int argc = args.Length + 1;
			argv = new System.String[argc];
			argv[0] = "jlc";
			for (int i = 0; i < args.Length; i++)
				argv[i + 1] = args[i];
			
			jlcArgs ma = new jlcArgs();
			if (!ma.processArgs(argv))
				System.Environment.Exit(1);
			
			Converter conv = new Converter();
			
			int detail = (ma.verbose_mode?ma.verbose_level:Converter.PrintWriterProgressListener.NO_DETAIL);
			
			System.IO.StreamWriter temp_writer;
			//UPGRADE_ISSUE: 'java.lang.System.out' was converted to 'System.Console.Out' which is not valid in this expression. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1109"'
			temp_writer = new System.IO.StreamWriter(System.Console.Out);
			temp_writer.AutoFlush = true;
			Converter.ProgressListener listener = new Converter.PrintWriterProgressListener(temp_writer, detail);
			
			try
			{
				conv.convert(ma.filename, ma.output_filename, listener);
			}
			catch (JavaLayerException ex)
			{
				System.Console.Error.WriteLine("Convertion failure: " + ex);
			}
			
			System.Environment.Exit(0);
		}
		
		
		/// <summary> Class to contain arguments for maplay.
		/// </summary>
		internal class jlcArgs
		{
			// channel constants moved into OutputChannels class.
			//public static final int	both = 0;
			//public static final int	left = 1;
			//public static final int	right = 2;
			//public static final int	downmix = 3;
			
			public int which_c;
			public int output_mode;
			public bool use_own_scalefactor;
			public float scalefactor;
			public System.String output_filename;
			public System.String filename;
			//public boolean 			stdout_mode;
			public bool verbose_mode;
			public int verbose_level = 3;
			
			public jlcArgs()
			{
				which_c = OutputChannels.BOTH_CHANNELS;
				use_own_scalefactor = false;
				scalefactor = (float) SupportClass.Identity(32768.0);
				//stdout_mode = false;
				verbose_mode = false;
			}
			
			/// <summary> Process user arguments.
			/// *
			/// Returns true if successful.
			/// </summary>
			public virtual bool processArgs(System.String[] argv)
			{
				filename = null;
				Crc16[] crc;
				crc = new Crc16[1];
				int i;
				int argc = argv.Length;
				
				//stdout_mode  = false;
				verbose_mode = false;
				output_mode = OutputChannels.BOTH_CHANNELS;
				output_filename = "";
				if (argc < 2 || argv[1].Equals("-h"))
					return Usage();
				
				i = 1;
				while (i < argc)
				{
					/* System.out.println("Option = "+argv[i]);*/
					if (argv[i][0] == '-')
					{
						if (argv[i].StartsWith("-v"))
						{
							verbose_mode = true;
							if (argv[i].Length > 2)
							{
								try
								{
									System.String level = argv[i].Substring(2);
									verbose_level = System.Int32.Parse(level);
								}
								catch (System.FormatException ex)
								{
									System.Console.Error.WriteLine("Invalid verbose level. Using default.");
								}
							}
							System.Console.Out.WriteLine("Verbose Activated (level " + verbose_level + ")");
						}
						/* else if (argv[i].equals("-s"))
						ma.stdout_mode = true; */
						else if (argv[i].Equals("-p"))
						{
							if (++i == argc)
							{
								System.Console.Out.WriteLine("Please specify an output filename after the -p option!");
								System.Environment.Exit(1);
							}
							//output_mode = O_WAVEFILE;
							output_filename = argv[i];
						}
						/*else if (argv[i].equals("-f"))
						{
						if (++i == argc)
						{
						System.out.println("Please specify a new scalefactor after the -f option!");
						System.exit(1);
						}
						ma.use_own_scalefactor = true;
						// ma.scalefactor = argv[i];
						}*/
						else
							return Usage();
					}
					else
					{
						filename = argv[i];
						System.Console.Out.WriteLine("FileName = " + argv[i]);
						if (filename == null)
							return Usage();
					}
					i++;
				}
				if (filename == null)
					return Usage();
				
				return true;
			}
			
			
			/// <summary> Usage of JavaLayer.
			/// </summary>
			public virtual bool Usage()
			{
				System.Console.Out.WriteLine("JavaLayer Converter V0.0.8 :");
				System.Console.Out.WriteLine("  -v[x]         verbose mode. ");
				System.Console.Out.WriteLine("                default = 2");
				/* System.out.println("  -s         write u-law samples at 8 kHz rate to stdout");
				System.out.println("  -l         decode only the left channel");
				System.out.println("  -r         decode only the right channel");
				System.out.println("  -d         downmix mode (layer III only)");
				System.out.println("  -s         write pcm samples to stdout");
				System.out.println("  -d         downmix mode (layer III only)");*/
				System.Console.Out.WriteLine("  -p name    output as a PCM wave file");
				System.Console.Out.WriteLine("");
				System.Console.Out.WriteLine("  More info on http://www.javazoom.net");
				/* System.out.println("  -f ushort  use this scalefactor instead of the default value 32768");*/
				return false;
			}
		}
		
	}
	
}