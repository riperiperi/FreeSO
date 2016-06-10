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
	
	/// <summary> Exception erorr codes for components of the JavaLayer API.
	/// </summary>
	
	internal struct JavaLayerErrors_Fields{
		public readonly static int BITSTREAM_ERROR = 0x100;
		public readonly static int DECODER_ERROR = 0x200;
	}
	internal interface JavaLayerErrors
		{
			//UPGRADE_NOTE: Members of interface 'JavaLayerErrors' were extracted into structure 'JavaLayerErrors_Fields'. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1045"'
			/// <summary> The first bitstream error code. See the {@link DecoderErrors DecoderErrors}
			/// interface for other bitstream error codes.
			/// </summary>
			/// <summary> The first decoder error code. See the {@link DecoderErrors DecoderErrors}
			/// interface for other decoder error codes.
			/// </summary>
		}
}