using System;
using System.Security.Principal;

namespace TSOClientMono
{
	static class Program
	{
		public static void Main(string[] args)
		{
			if (args.Length == 1)
			{
				using (Game1 game = new Game1(args[0]))
				{
					game.Run();
				}
			}
			else
			{
				Console.WriteLine("Usage: mono 'TSOClient Mono.exe' <path to The Sims Online>");
				Console.WriteLine("Example: mono 'TSOClient Mono.exe' /home/user/TSO/");
			}
		}
		
		private static bool IsAdministrator
		{
			get
			{
				PlatformID platform = Environment.OSVersion.Platform;
				
				if (platform == PlatformID.Win32NT)
				{
					WindowsIdentity wi = WindowsIdentity.GetCurrent();
					WindowsPrincipal wp = new WindowsPrincipal(wi);
					
					return wp.IsInRole(WindowsBuiltInRole.Administrator);
				}
				else if (platform == PlatformID.Unix || platform == PlatformID.MacOSX)
				{
					return Environment.UserName == "root";
				}
				
				return true;
			}
		}
	}
}
