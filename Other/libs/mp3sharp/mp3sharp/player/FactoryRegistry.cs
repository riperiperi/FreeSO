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
	/// <summary> The <code>FactoryRegistry</code> class stores the factories
	/// for all the audio device implementations available in the system. 
	/// <p>
	/// Instances of this class are thread-safe. 
	/// 
	/// @since 0.0.8
	/// </summary>
	/// <author>  Mat McGowan
	/// 
	/// </author>
	
	public class FactoryRegistry:AudioDeviceFactory
	{
		public FactoryRegistry()
		{
			InitBlock();
		}
		private void  InitBlock()
		{
			factories = new System.Collections.Hashtable();
		}
		virtual protected internal AudioDeviceFactory[] FactoriesPriority
		{
			get
			{
				AudioDeviceFactory[] fa = null;
				lock (factories)
				{
					int size = factories.Count;
					if (size != 0)
					{
						fa = new AudioDeviceFactory[size];
						int idx = 0;
						System.Collections.IEnumerator e = factories.GetEnumerator();
						//UPGRADE_TODO: Method 'java.util.Enumeration.hasMoreElements' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1073"'
						while (e.MoveNext())
						{
							//UPGRADE_TODO: Method 'java.util.Enumeration.nextElement' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1073"'
							AudioDeviceFactory factory = (AudioDeviceFactory) e.Current;
							fa[idx++] = factory;
						}
					}
				}
				return fa;
			}
			
		}
		private static FactoryRegistry instance = null;
		
		//UPGRADE_NOTE: Synchronized keyword was removed from method 'systemRegistry'. Lock expression was added. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1027"'
		static public FactoryRegistry systemRegistry()
		{
			lock (typeof(javazoom.jl.player.FactoryRegistry))
			{
				if (instance == null)
				{
					instance = new FactoryRegistry();
					instance.registerDefaultFactories();
				}
				return instance;
			}
		}
		
		
		//UPGRADE_NOTE: The initialization of  'factories' was moved to method 'InitBlock'. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1005"'
		protected internal System.Collections.Hashtable factories;
		
		/// <summary> Registers an <code>AudioDeviceFactory</code> instance
		/// with this registry. 
		/// </summary>
		public virtual void  addFactory(AudioDeviceFactory factory)
		{
			SupportClass.PutElement(factories, factory.GetType(), factory);
		}
		
		public virtual void  removeFactoryType(System.Type cls)
		{
			SupportClass.HashtableRemove(factories, cls);
		}
		
		public virtual void  removeFactory(AudioDeviceFactory factory)
		{
			SupportClass.HashtableRemove(factories, factory.GetType());
		}
		
		public override AudioDevice createAudioDevice()
		{
			AudioDevice device = null;
			AudioDeviceFactory[] factories = FactoriesPriority;
			
			if (factories == null)
				throw new JavaLayerException(this + ": no factories registered");
			
			JavaLayerException lastEx = null;
			for (int i = 0; (device == null) && (i < factories.Length); i++)
			{
				try
				{
					device = factories[i].createAudioDevice();
				}
				catch (JavaLayerException ex)
				{
					lastEx = ex;
				}
			}
			
			if (device == null && lastEx != null)
			{
				throw new JavaLayerException("Cannot create AudioDevice", lastEx);
			}
			
			return device;
		}
		
		
		
		protected internal virtual void  registerDefaultFactories()
		{
			addFactory(new JavaSoundAudioDeviceFactory());
		}
	}
}