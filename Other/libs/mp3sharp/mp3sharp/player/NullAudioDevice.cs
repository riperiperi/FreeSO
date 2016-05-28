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
	
	/// <summary> The <code>NullAudioDevice</code> implements a silent, no-op
	/// audio device. This is useful for testing purposes.
	/// 
	/// @since 0.0.8
	/// </summary>
	/// <author>  Mat McGowan
	/// 
	/// </author>
	public class NullAudioDevice:AudioDeviceBase
	{
		override public int Position
		{
			get
			{
				return 0;
			}
			
		}
		
	}
}