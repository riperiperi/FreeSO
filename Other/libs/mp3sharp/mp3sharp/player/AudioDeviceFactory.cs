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
	using javazoom.jl.decoder;
	/// <summary> An <code>AudioDeviceFactory</code> class is responsible for creating
	/// a specific <code>AudioDevice</code> implementation. A factory implementation
	/// can be as simple or complex as desired and may support just one implementation
	/// or may return several implementations depending upon the execution
	/// environment. 
	/// <p>
	/// When implementing a factory that provides an AudioDevice that uses
	/// class that may not be present, the factory should dynamically link to any
	/// specific implementation classes required to instantiate or test the audio
	/// implementation. This is so that the application as a whole
	/// can run without these classes being present. The audio
	/// device implementation, however, will usually statically link to the classes
	/// required. (See the JavaSound deivce and factory for an example
	/// of this.)
	/// 
	/// </summary>
	/// <seealso cref="">FactoryRegistry
	/// 
	/// @since	0.0.8
	/// </seealso>
	/// <author> 	Mat McGowan
	/// 
	/// </author>
	public abstract class AudioDeviceFactory
	{
		/// <summary> Creates a new <code>AudioDevice</code>.
		/// 
		/// </summary>
		/// <returns>	a new instance of a specific class of <code>AudioDevice</code>.
		/// @throws	JavaLayerException if an instance of AudioDevice could not
		/// be created. 
		/// 
		/// </returns>
		public abstract AudioDevice createAudioDevice();
		
		//UPGRADE_ISSUE: Class 'java.lang.ClassLoader' was not converted. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1000_javalangClassLoader"'
		/// <summary> Creates an instance of an AudioDevice implementation. 
		/// </summary>
		/// <param name="loader	The"><code>ClassLoader</code> to use to
		/// load the named class, or null to use the
		/// system class loader.
		/// </param>
		/// <param name="name		The">name of the class to load.
		/// </param>
		/// <returns>			A newly-created instance of the audio device class.
		/// 
		/// </returns>
		protected internal virtual AudioDevice instantiate(ClassLoader loader, System.String name)
		{
			AudioDevice dev = null;
			
			System.Type cls = null;
			if (loader == null)
			{
				//UPGRADE_TODO: Format of parameters of method 'java.lang.Class.forName' are different in the equivalent in .NET. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1092"'
				cls = System.Type.GetType(name);
			}
			else
			{
				//UPGRADE_ISSUE: Method 'java.lang.ClassLoader.loadClass' was not converted. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1000_javalangClassLoader"'
				cls = loader.loadClass(name);
			}
			
			System.Object o = SupportClass.CreateNewInstance(cls);
			dev = (AudioDevice) o;
			
			return dev;
		}
	}
}