using Support;
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
	/// <summary> <i>Work In Progress.</i>
	/// 
	/// An instance of <code>InputStreamSource</code> implements a
	/// <code>Source</code> that provides data from an <code>InputStream
	/// </code>. Seeking functionality is not supported. 
	/// 
	/// </summary>
	/// <author>  MDM
	/// 
	/// </author>
	internal class InputStreamSource : Source
	{
		virtual public bool Seekable
		{
			get
			{
				return false;
			}
			
		}
		//UPGRADE_NOTE: Final was removed from the declaration of 'in '. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1003"'
		private System.IO.Stream in_Renamed;
		
		public InputStreamSource(System.IO.Stream in_Renamed)
		{
			if (in_Renamed == null)
				throw new System.NullReferenceException("in");
			
			this.in_Renamed = in_Renamed;
		}
		
		public virtual int read(sbyte[] b, int offs, int len)
		{
			int read = SupportClass.ReadInput(in_Renamed, ref b, offs, len);
			return read;
		}
		
		public virtual bool willReadBlock()
		{
			return true;
			//boolean block = (in.available()==0);
			//return block;
		}
		
		
		public virtual long tell()
		{
			return - 1;
		}
		
		public virtual long seek(long to)
		{
			return - 1;
		}
		
		public virtual long length()
		{
			return - 1;
		}
	}
}