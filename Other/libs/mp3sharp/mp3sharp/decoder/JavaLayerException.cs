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
namespace Mp3Sharp
{
	using System;
	/// <summary> The Mp3SharpException is the base class for all API-level
	/// exceptions thrown by JavaLayer. To facilitate conversion and 
	/// common handling of exceptions from other domains, the class 
	/// can delegate some functionality to a contained Throwable instance. 
	/// <p> 
	/// 
	/// </summary>
	/// <author>  MDM
	/// 
	/// </author>
	public class Mp3SharpException:System.Exception
	{
		//UPGRADE_NOTE: Exception 'java.lang.Throwable' was converted to 'System.Exception' which has different behavior. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1100"'
		virtual public System.Exception Exception
		{
			get
			{
				return exception;
			}
			
		}
		
		//UPGRADE_NOTE: Exception 'java.lang.Throwable' was converted to 'System.Exception' which has different behavior. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1100"'
		private System.Exception exception;
		
		
		public Mp3SharpException()
		{
		}
		
		public Mp3SharpException(System.String msg):base(msg)
		{
		}
		
		//UPGRADE_NOTE: Exception 'java.lang.Throwable' was converted to 'System.Exception' which has different behavior. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1100"'
		public Mp3SharpException(System.String msg, System.Exception t):base(msg)
		{
			exception = t;
		}
		
		
		
		//UPGRADE_TODO: The equivalent of method 'java.lang.Throwable.printStackTrace' is not an override method. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1143"'
		public void  printStackTrace()
		{
			SupportClass.WriteStackTrace(this, System.Console.Error);
		}
		
		//UPGRADE_TODO: The equivalent of method 'java.lang.Throwable.printStackTrace' is not an override method. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1143"'
		public void  printStackTrace(System.IO.StreamWriter ps)
		{
			if (this.exception == null)
			{
				SupportClass.WriteStackTrace((System.Exception) this, ps);
			}
			else
			{
				SupportClass.WriteStackTrace(exception, Console.Error);
			}
		}
	}
}