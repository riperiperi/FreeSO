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
using javazoom.jl.decoder;
using javazoom;
using javazoom.jl.converter;
using javazoom.jl;
namespace Mp3Sharp
{
	using System;
	
	/// <summary> Instances of <code>BitstreamException</code> are thrown 
	/// when operations on a <code>Bitstream</code> fail. 
	/// <p>
	/// The exception provides details of the exception condition 
	/// in two ways:
	/// <ol><li>
	/// as an error-code describing the nature of the error
	/// </li><br></br><li>
	/// as the <code>Throwable</code> instance, if any, that was thrown
	/// indicating that an exceptional condition has occurred. 
	/// </li></ol></p>
	/// 
	/// @since 0.0.6
	/// </summary>
	/// <author>  MDM	12/12/99
	/// 
	/// </author>
	
	public class BitstreamException : Mp3SharpException, BitstreamErrors
	{
		private void  InitBlock()
		{
			errorcode = javazoom.jl.decoder.BitstreamErrors_Fields.UNKNOWN_ERROR;
		}
		virtual public int ErrorCode
		{
			get
			{
				return errorcode;
			}
			
		}
		//UPGRADE_NOTE: The initialization of  'errorcode' was moved to method 'InitBlock'. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1005"'
		private int errorcode;
		
		//UPGRADE_NOTE: Exception 'java.lang.Throwable' was converted to 'System.Exception' which has different behavior. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1100"'
		public BitstreamException(System.String msg, System.Exception t):base(msg, t)
		{
			InitBlock();
		}
		
		//UPGRADE_NOTE: Exception 'java.lang.Throwable' was converted to 'System.Exception' which has different behavior. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1100"'
		public BitstreamException(int errorcode, System.Exception t):this(getErrorString(errorcode), t)
		{
			InitBlock();
			this.errorcode = errorcode;
		}
		
		
		
		static public System.String getErrorString(int errorcode)
		{
			// REVIEW: use resource bundle to map error codes
			// to locale-sensitive strings.
			
			return "Bitstream errorcode " + System.Convert.ToString(errorcode, 16);
		}
	}
}