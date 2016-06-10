/*
* 12/12/99		Initial version.	mdm@techie.com
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
namespace javazoom.jl.decoder
{
	using System;
	/// <summary> The JavaLayerUtils class is not strictly part of the JavaLayer API.
	/// It serves to provide useful methods and system-wide hooks.
	/// 
	/// </summary>
	/// <author>  MDM
	/// 
	/// </author>
	
	internal class JavaLayerUtils
	{
		//UPGRADE_NOTE: Synchronized keyword was removed from method 'getHook'. Lock expression was added. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1027"'
		/// <summary> Sets the system-wide JavaLayer hook.
		/// </summary>
		//UPGRADE_NOTE: Synchronized keyword was removed from method 'setHook'. Lock expression was added. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1027"'
		static public JavaLayerHook Hook
		{
			get
			{
				lock (typeof(javazoom.jl.decoder.JavaLayerUtils))
				{
					return hook;
				}
			}
			
			set
			{
				lock (typeof(javazoom.jl.decoder.JavaLayerUtils))
				{
					hook = value;
				}
			}
			
		}
		private static JavaLayerHook hook = null;
		/*
		/// <summary> Deserializes the object contained in the given input stream.
		/// </summary>
		/// <param name="in	The">input stream to deserialize an object from.
		/// </param>
		/// <param name="cls	The">expected class of the deserialized object. 
		/// 
		/// </param>
		static public System.Object deserialize(System.IO.Stream in_Renamed, System.Type cls)
		{
			if (cls == null)
				throw new System.NullReferenceException("cls");
			
			System.Object obj = deserialize(in_Renamed, cls);
			if (!cls.IsInstanceOfType(obj))
			{
				throw new System.IO.IOException("type of deserialized instance not of required class.");
			}
			
			return obj;
		}
		
		/// <summary> Deserializes an object from the given <code>InputStream</code>.
		/// The deserialization is delegated to an <code>
		/// ObjectInputStream</code> instance. 
		/// 
		/// </summary>
		/// <param name="in	The"><code>InputStream</code> to deserialize an object
		/// from.
		/// 
		/// </param>
		/// <returns> The object deserialized from the stream. 
		/// </returns>
		/// <exception cref=""> IOException is thrown if there was a problem reading
		/// the underlying stream, or an object could not be deserialized
		/// from the stream.
		/// 
		/// </exception>
		/// <seealso cref="">java.io.ObjectInputStream
		/// 
		/// </seealso>
		static public System.Object deserialize(System.IO.Stream in_Renamed)
		{
			if (in_Renamed == null)
				throw new System.NullReferenceException("in");
			
			System.IO.BinaryReader objIn = new System.IO.BinaryReader(in_Renamed);
			
			System.Object obj;
			
			//UPGRADE_NOTE: Exception 'java.lang.ClassNotFoundException' was converted to 'System.Exception' which has different behavior. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1100"'
			try
			{
				//UPGRADE_WARNING: Method 'java.io.ObjectInputStream.readObject' was converted to 'SupportClass.Deserialize' which may throw an exception. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1101"'
				obj = SupportClass.Deserialize(objIn);
			}
			catch (System.Exception ex)
			{
				//UPGRADE_TODO: The equivalent in .NET for method 'java.lang.Throwable.toString' may return a different value. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1043"'
				throw new System.IO.IOException(ex.ToString());
			}
			
			return obj;
		}
		
		/// <summary> Deserializes an array from a given <code>InputStream</code>.
		/// 
		/// </summary>
		/// <param name="in		The"><code>InputStream</code> to 
		/// deserialize an object from.
		/// 
		/// </param>
		/// <param name="elemType	The">class denoting the type of the array
		/// elements.
		/// </param>
		/// <param name="length	The">expected length of the array, or -1 if
		/// any length is expected.
		/// 
		/// </param>
		static public System.Object deserializeArray(System.IO.Stream in_Renamed, System.Type elemType, int length)
		{
			if (elemType == null)
				throw new System.NullReferenceException("elemType");
			
			if (length < - 1)
				throw new System.ArgumentException("length");
			
			System.Object obj = deserialize(in_Renamed);
			
			System.Type cls = obj.GetType();
			
			
			if (!cls.IsArray)
				throw new System.IO.IOException("object is not an array");
			
			System.Type arrayElemType = cls.GetElementType();
			if (arrayElemType != elemType)
				throw new System.IO.IOException("unexpected array component type");
			
			if (length != - 1)
			{
				int arrayLength = ((System.Array) obj).Length;
				if (arrayLength != length)
					throw new System.IO.IOException("array length mismatch");
			}
			
			return obj;
		}
		
		static public System.Object deserializeArrayResource(System.String name, System.Type elemType, int length)
		{
			System.IO.Stream str = getResourceAsStream(name);
			if (str == null)
				throw new System.IO.IOException("unable to load resource '" + name + "'");
			
			System.Object obj = deserializeArray(str, elemType, length);
			
			return obj;
		}
		
		static public void  serialize(System.IO.Stream out_Renamed, System.Object obj)
		{
			if (out_Renamed == null)
				throw new System.NullReferenceException("out");
			
			if (obj == null)
				throw new System.NullReferenceException("obj");
			
			System.IO.BinaryWriter objOut = new System.IO.BinaryWriter(out_Renamed);
			SupportClass.Serialize(objOut, obj);
		}
		
		
		
		//UPGRADE_NOTE: Synchronized keyword was removed from method 'getResourceAsStream'. Lock expression was added. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1027"'
		/// <summary> Retrieves an InputStream for a named resource. 
		/// 
		/// </summary>
		/// <param name="name	The">name of the resource. This must be a simple
		/// name, and not a qualified package name.
		/// 
		/// </param>
		/// <returns>		The InputStream for the named resource, or null if
		/// the resource has not been found. If a hook has been 
		/// provided, its getResourceAsStream() method is called
		/// to retrieve the resource. 
		/// 
		/// </returns>
		static public System.IO.Stream getResourceAsStream(System.String name)
		{
			lock (typeof(javazoom.jl.decoder.JavaLayerUtils))
			{
				System.IO.Stream is_Renamed = null;
				
				if (hook != null)
				{
					is_Renamed = hook.getResourceAsStream(name);
				}
				else
				{
					System.Type cls = typeof(JavaLayerUtils);
					//UPGRADE_ISSUE: Method 'java.lang.Class.getResourceAsStream' was not converted. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1000_javalangClassgetResourceAsStream_javalangString"'
					is_Renamed = cls.getResourceAsStream(name);
				}
				
				return is_Renamed;
			}
		}*/
	}
}