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
	using JavaLayerException = javazoom.jl.decoder.JavaLayerException;
	/// <summary> This class is responsible for creating instances of the
	/// JavaSoundAudioDevice. The audio device implementation is loaded
	/// and tested dynamically as not all systems will have support
	/// for JavaSound, or they may have the incorrect version. 
	/// </summary>
	
	public class JavaSoundAudioDeviceFactory:AudioDeviceFactory
	{
		private bool tested = false;
		
		private const System.String DEVICE_CLASS_NAME = "javazoom.jl.player.JavaSoundAudioDevice";
		
		//UPGRADE_NOTE: Synchronized keyword was removed from method 'createAudioDevice'. Lock expression was added. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1027"'
		public override AudioDevice createAudioDevice()
		{
			lock (this)
			{
				if (!tested)
				{
					testAudioDevice();
					tested = true;
				}
				
				try
				{
					return createAudioDeviceImpl();
				}
				catch (System.Exception ex)
				{
					throw new JavaLayerException("unable to create JavaSound device: " + ex);
				}
				catch (System.ApplicationException ex)
				{
					throw new JavaLayerException("unable to create JavaSound device: " + ex);
				}
			}
		}
		
		protected internal virtual JavaSoundAudioDevice createAudioDeviceImpl()
		{
			//UPGRADE_ISSUE: Class 'java.lang.ClassLoader' was not converted. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1000_javalangClassLoader"'
			//UPGRADE_ISSUE: Method 'java.lang.Class.getClassLoader' was not converted. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1000_javalangClassgetClassLoader"'
			ClassLoader loader = GetType().getClassLoader();
			try
			{
				JavaSoundAudioDevice dev = (JavaSoundAudioDevice) instantiate(loader, DEVICE_CLASS_NAME);
				return dev;
			}
			catch (System.Exception ex)
			{
				throw new JavaLayerException("Cannot create JavaSound device", ex);
			}
			catch (System.ApplicationException ex)
			{
				throw new JavaLayerException("Cannot create JavaSound device", ex);
			}
		}
		
		public virtual void  testAudioDevice()
		{
			JavaSoundAudioDevice dev = createAudioDeviceImpl();
			dev.test();
		}
	}
}